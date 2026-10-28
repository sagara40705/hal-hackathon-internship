using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class StyleMovableTiles
{
    static string ScenePath(int w, int s) => $"Assets/NormalStage/stage{w}-{s}/stage{w}-{s}.unity";
    public static string Inspect()
    {
        var output = new StringBuilder();
        var root = PrefabUtility.LoadPrefabContents("Assets/NormalStage/stage1-1/Stage1-1_Layout.prefab");
        try {
            foreach (var g in root.GetComponentsInChildren<TileGroup>(true).Where(g => !g.IsStatic)) {
                output.AppendLine(g.name);
                foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                    output.AppendLine($"  {r.name} {r.GetType().Name} cell={r.GetComponent<TileCell>() != null} mats={string.Join(",", r.sharedMaterials.Select(m => m == null ? "null" : m.name))}");
            }
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        return output.ToString();
    }
    static int Apply(GameObject root, Material material, bool verify)
    {
        int count = 0;
        foreach (var cell in root.GetComponentsInChildren<TileCell>(true)) {
            var group = cell.GetComponentInParent<TileGroup>();
            if (group == null || group.IsStatic) continue;
            var renderer = cell.GetComponent<MeshRenderer>();
            if (renderer == null) throw new Exception("Tile mesh missing: " + cell.name);
            if (verify) {
                if (renderer.sharedMaterials.Any(m => m != material)) throw new Exception("Wrong material: " + cell.name);
            } else if (renderer.sharedMaterials.Any(m => m != material)) {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => material).ToArray();
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            foreach (var arrow in cell.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null && r.sprite.name == "TEX_Arrow")) {
                if (verify) {
                    if (arrow.color != Color.black) throw new Exception("Wrong arrow color: " + cell.name);
                } else if (arrow.color != Color.black) {
                    arrow.color = Color.black;
                    EditorUtility.SetDirty(arrow);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(arrow);
                }
            }
            count++;
        }
        return count;
    }
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Editor is playing");
        for (int i=0; i<SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: " + SceneManager.GetSceneAt(i).path);
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Tile/Material/MAT_BaseTile.mat");
        if (material == null) throw new Exception("Material missing");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var log = new StringBuilder();
        try {
            for (int w=1; w<=3; w++) for (int s=1; s<=4; s++) {
                var path = $"Assets/NormalStage/stage{w}-{s}/Stage{w}-{s}_Layout.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try {
                    Apply(root, material, false);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                } finally { PrefabUtility.UnloadPrefabContents(root); }
                var scene = EditorSceneManager.OpenScene(ScenePath(w,s), OpenSceneMode.Single);
                foreach (var go in scene.GetRootGameObjects()) Apply(go, material, false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            for (int w=1; w<=3; w++) for (int s=1; s<=4; s++) {
                var scene = EditorSceneManager.OpenScene(ScenePath(w,s), OpenSceneMode.Single);
                int count = scene.GetRootGameObjects().Sum(go => Apply(go, material, true));
                var root = PrefabUtility.LoadPrefabContents($"Assets/NormalStage/stage{w}-{s}/Stage{w}-{s}_Layout.prefab");
                try { Apply(root, material, true); } finally { PrefabUtility.UnloadPrefabContents(root); }
                log.AppendLine($"stage{w}-{s}: {count} movable cells verified, prefab and scene.");
            }
        } finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return log.ToString();
    }
}
