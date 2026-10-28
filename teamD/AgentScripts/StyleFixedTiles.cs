using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class StyleFixedTiles
{
    public static string Inspect()
    {
        var b = new StringBuilder();
        foreach (var path in new[] { "Assets/Tile/Material/MAT_FixedTile.mat", "Assets/Tile/Material/MAT_FixedTile 1.mat" }) {
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            b.AppendLine(path + " " + m.color);
        }
        var root=PrefabUtility.LoadPrefabContents("Assets/NormalStage/stage2-4/Stage2-4_Layout.prefab");
        try {
            foreach (var m in root.GetComponentsInChildren<TileCell>(true).Where(c=>c.GetComponentInParent<TileGroup>().IsStatic).Select(c=>c.GetComponent<MeshRenderer>()).Where(r=>r!=null).SelectMany(r=>r.sharedMaterials).Distinct())
                b.AppendLine("Fixed uses: " + AssetDatabase.GetAssetPath(m));
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) b.AppendLine(c.name+" "+c.clearFlags+" "+c.backgroundColor);
        return b.ToString();
    }
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Editor is playing");
        for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene");
        Color tile, background;
        ColorUtility.TryParseHtmlString("#8295AD",out tile);
        ColorUtility.TryParseHtmlString("#202C40",out background);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Tile/Material/MAT_FixedTile.mat");
        material.SetColor("_BaseColor",tile);
        if(material.HasProperty("_Color")) material.SetColor("_Color",tile);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        var setup=EditorSceneManager.GetSceneManagerSetup();
        int verified=0;
        try {
            for(int w=1;w<=3;w++) for(int s=1;s<=4;s++) {
                var scene=EditorSceneManager.OpenScene($"Assets/NormalStage/stage{w}-{s}/stage{w}-{s}.unity",OpenSceneMode.Single);
                var cam=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single(c=>c.CompareTag("MainCamera"));
                cam.clearFlags=CameraClearFlags.SolidColor;
                cam.backgroundColor=background;
                PrefabUtility.RecordPrefabInstancePropertyModifications(cam);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            for(int w=1;w<=3;w++) for(int s=1;s<=4;s++) {
                var scene=EditorSceneManager.OpenScene($"Assets/NormalStage/stage{w}-{s}/stage{w}-{s}.unity",OpenSceneMode.Single);
                var cam=Camera.main;
                if(cam.backgroundColor!=background || cam.clearFlags!=CameraClearFlags.SolidColor) throw new Exception("Background mismatch");
                verified++;
                if(w==1 && s==1) {
                    var rt=new RenderTexture(1280,720,24);
                    var prior=RenderTexture.active;
                    var target=cam.targetTexture;
                    var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    try {
                        cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt;
                        tex.ReadPixels(new Rect(0,0,1280,720),0,0); tex.Apply();
                        Directory.CreateDirectory("Logs/StageRebuild");
                        File.WriteAllBytes("Logs/StageRebuild/fixed-tile-colors.png",tex.EncodeToPNG());
                    } finally {cam.targetTexture=target; RenderTexture.active=prior; UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);}
                }
            }
        } finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return verified+" scenes verified. Fixed tile #8295AD, background #202C40.";
    }
}
