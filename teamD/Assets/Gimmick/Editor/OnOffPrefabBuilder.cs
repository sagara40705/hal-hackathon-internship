using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>ON/OFF用の配置Prefabを、Unity標準プリミティブで生成する。</summary>
public static class OnOffPrefabBuilder
{
    private const string Root = "Assets/Gimmick";
    private const string GroupPath = Root + "/Prefab/TileGroup/";
    private const string MaterialPath = Root + "/Material/OnOff/";

    [MenuItem("Tools/Gimmick/ON OFF/Create Missing Prefabs")]
    public static void CreatePrefabs()
    {
        Directory.CreateDirectory(MaterialPath);
        AssetDatabase.Refresh();
        Material blue = GetMaterial("Off_Blue", new Color(0.1f, 0.45f, 1f));
        Material orange = GetMaterial("On_Orange", new Color(1f, 0.4f, 0.05f));
        Material dark = GetMaterial("Switch_Base", new Color(0.12f, 0.15f, 0.2f));
        CreateFloor("Group_OnOffFloor_Off", false, blue);
        CreateFloor("Group_OnOffFloor_On", true, orange);
        CreateSwitch(blue, dark);
        CreateController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ON/OFF PrefabをAssets/Gimmick/Prefabに作成しました。既存ファイルは上書きしません。");
    }

    private static Material GetMaterial(string name, Color color)
    {
        string path = MaterialPath + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.3f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject CreateGroup(string name, out GameObject cell)
    {
        GameObject root = new GameObject(name);
        root.AddComponent<TileGroup>(); // 既定値の固定フラグを使用する。
        cell = new GameObject("Cell");
        cell.transform.SetParent(root.transform, false);
        cell.AddComponent<TileCell>();
        return root;
    }

    private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size,
        Material material, bool collider = false)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static TextMesh Label(Transform parent, string text, float y)
    {
        GameObject label = new GameObject("StateLabel");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, y, 0f);
        label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TextMesh mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 64;
        mesh.characterSize = 0.065f;
        mesh.color = Color.white;
        return mesh;
    }

    private static void CreateFloor(string name, bool solidWhenOn, Material material)
    {
        string path = GroupPath + name + ".prefab";
        if (File.Exists(path))
            return;

        GameObject root = CreateGroup(name, out GameObject cell);
        try
        {
            OnOffFloor floor = root.AddComponent<OnOffFloor>();
            GameObject solid = new GameObject("Solid");
            solid.transform.SetParent(cell.transform, false);
            Cube("Floor", solid.transform, Vector3.zero, new Vector3(0.96f, 1f, 0.96f), material, true);
            Label(solid.transform, solidWhenOn ? "ON" : "OFF", 0.51f);

            GameObject outline = new GameObject("InactiveOutline");
            outline.transform.SetParent(cell.transform, false);
            Cube("North", outline.transform, new Vector3(0f, 0.49f, 0.44f), new Vector3(0.94f, 0.035f, 0.045f), material);
            Cube("South", outline.transform, new Vector3(0f, 0.49f, -0.44f), new Vector3(0.94f, 0.035f, 0.045f), material);
            Cube("East", outline.transform, new Vector3(0.44f, 0.49f, 0f), new Vector3(0.045f, 0.035f, 0.94f), material);
            Cube("West", outline.transform, new Vector3(-0.44f, 0.49f, 0f), new Vector3(0.045f, 0.035f, 0.94f), material);
            TextMesh ghostLabel = Label(outline.transform, solidWhenOn ? "ON" : "OFF", 0.505f);
            ghostLabel.color = material.GetColor("_BaseColor");

            SerializedObject data = new SerializedObject(floor);
            data.FindProperty("_solidWhenOn").boolValue = solidWhenOn;
            data.FindProperty("_solidVisual").objectReferenceValue = solid;
            data.FindProperty("_inactiveVisual").objectReferenceValue = outline;
            data.ApplyModifiedPropertiesWithoutUndo();
            // 編集時の見た目も初期OFF状態に合わせる。
            solid.SetActive(!solidWhenOn);
            outline.SetActive(solidWhenOn);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void CreateSwitch(Material blue, Material dark)
    {
        string path = GroupPath + "Group_OnOffSwitch.prefab";
        if (File.Exists(path))
            return;

        GameObject root = CreateGroup("Group_OnOffSwitch", out GameObject cell);
        try
        {
            Cube("Base", cell.transform, Vector3.zero, new Vector3(0.96f, 1f, 0.96f), dark, true);
            GameObject button = Cube("Button", cell.transform, new Vector3(0f, 0.57f, 0f), new Vector3(0.72f, 0.12f, 0.72f), blue);
            TextMesh label = Label(cell.transform, "OFF", 0.64f);
            OnOffSwitch tileSwitch = cell.AddComponent<OnOffSwitch>();
            SerializedObject data = new SerializedObject(tileSwitch);
            data.FindProperty("_buttonRenderer").objectReferenceValue = button.GetComponent<Renderer>();
            data.FindProperty("_button").objectReferenceValue = button.transform;
            data.FindProperty("_stateLabel").objectReferenceValue = label;
            data.ApplyModifiedPropertiesWithoutUndo();

            // 既存TileCellの動的Playerイベントへ、Prefab内参照を永続化する。
            UnityEvent<Player> entered = new UnityEvent<Player>();
            UnityEventTools.AddPersistentListener(entered, tileSwitch.OnEnterPlayer);
            // UnityEvent自体はSerializedPropertyへ代入できないため、対象フィールドへ設定する。
            typeof(TileCell).GetField("_onEnterPlayerEvent", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(cell.GetComponent<TileCell>(), entered);
            EditorUtility.SetDirty(cell.GetComponent<TileCell>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void CreateController()
    {
        string path = Root + "/Prefab/OnOffController.prefab";
        if (File.Exists(path))
            return;
        GameObject root = new GameObject("OnOffController");
        try
        {
            root.AddComponent<OnOffController>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [MenuItem("Tools/Gimmick/ON OFF/Create Sample Scene")]
    public static void CreateSampleScene()
    {
        const string path = Root + "/OnOffSample.unity";
        if (File.Exists(path))
            return;

        CreatePrefabs();
        Scene original = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            GameObject gridObject = new GameObject("TileGrid");
            TileGrid grid = gridObject.AddComponent<TileGrid>();
            SerializedObject gridData = new SerializedObject(grid);
            gridData.FindProperty("_gridSize").vector2IntValue = new Vector2Int(11, 5);
            gridData.ApplyModifiedPropertiesWithoutUndo();
            grid.SetAllPlacementAllowed(true);
            InstantiatePrefab(Root + "/Prefab/OnOffController.prefab", gridObject.transform, Vector3.zero);

            Material dark = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath + "Switch_Base.mat");
            TileCell endCell = null;
            for (int x = -4; x <= 4; x++)
            {
                string prefab = x == -3 || x == 2 ? "Group_OnOffSwitch" :
                    x >= -2 && x <= 0 ? "Group_OnOffFloor_On" :
                    x == 3 ? "Group_OnOffFloor_Off" : null;
                if (prefab != null)
                    InstantiatePrefab(GroupPath + prefab + ".prefab", gridObject.transform, new Vector3(x, 0f, 0f));
                else
                {
                    GameObject group = CreateGroup(x == 4 ? "Finish" : "SafeFloor", out GameObject cell);
                    group.transform.SetParent(gridObject.transform, false);
                    group.transform.localPosition = new Vector3(x, 0f, 0f);
                    Cube("Floor", cell.transform, Vector3.zero, new Vector3(0.96f, 1f, 0.96f), dark, true);
                    if (x == 4)
                    {
                        Label(cell.transform, "END", 0.51f);
                        endCell = cell.GetComponent<TileCell>();
                    }
                }
            }

            GameObject playerObject = InstantiatePrefab("Assets/Player/Prefab/Player.prefab", null, new Vector3(-4f, 0f, 0f));
            Player player = playerObject.GetComponent<Player>();
            SerializedObject playerData = new SerializedObject(player);
            playerData.FindProperty("_tileGrid").objectReferenceValue = grid;
            playerData.FindProperty("_moveSpeed").floatValue = 1.2f;
            playerData.FindProperty("_startDelay").floatValue = 2f;
            playerData.ApplyModifiedPropertiesWithoutUndo();
            UnityEvent<Player> finish = new UnityEvent<Player>();
            UnityEventTools.AddVoidPersistentListener(finish, player.Stop);
            typeof(TileCell).GetField("_onEnterPlayerEvent", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(endCell, finish);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.2f;
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0f, 7f, -5f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, 0f));
            cameraObject.AddComponent<AudioListener>();
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Label(gridObject.transform, "ON / OFF - walk over switches", 0.52f).transform.localPosition = new Vector3(0f, 0.52f, 1.6f);
            EditorSceneManager.SaveScene(scene, path);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject InstantiatePrefab(string path, Transform parent, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        return instance;
    }
}
