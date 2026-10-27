using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>編集中のシーンを切り替えず、プレビューシーンで撮影する。</summary>
public static class StageThumbnailCapture
{
    public static string ValidateSettings(StageThumbnailCaptureSettings settings)
    {
        if (settings == null)
            return "撮影設定がありません。";

        if (settings.Scenes == null || settings.Scenes.Length == 0)
            return "撮影するシーンを1つ以上登録してください。";

        string folder = AssetDatabase.GetAssetPath(settings.OutputFolder);
        if (!folder.StartsWith("Assets/", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(folder))
            return "保存先にはAssets内のフォルダを指定してください。";

        int maxSize = Mathf.Min(4096, SystemInfo.maxTextureSize);
        if (settings.Width < 16 || settings.Height < 16 || settings.Width > maxSize || settings.Height > maxSize)
            return $"画像サイズは16〜{maxSize}の範囲で指定してください。";

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sceneAsset in settings.Scenes)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            if (sceneAsset == null || !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                return "シーン一覧に未設定の項目があります。";

            if (!names.Add(Path.GetFileNameWithoutExtension(path)))
                return "同じ名前のシーンが複数登録されています。PNGの上書きを防ぐため、シーン名を分けてください。";

            Scene openScene = SceneManager.GetSceneByPath(path);
            if (openScene.IsValid() && openScene.isDirty)
                return $"{sceneAsset.name}に未保存の変更があります。シーンを保存してから撮影してください。";
        }

        return null;
    }

    public static int CaptureAll(StageThumbnailCaptureSettings settings)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("再生を停止してから撮影してください。");

        string error = ValidateSettings(settings);
        if (error != null)
            throw new InvalidOperationException(error);

        string folder = AssetDatabase.GetAssetPath(settings.OutputFolder);
        int captured = 0;
        try
        {
            for (int i = 0; i < settings.Scenes.Length; i++)
            {
                string scenePath = AssetDatabase.GetAssetPath(settings.Scenes[i]);
                string outputPath = $"{folder}/{Path.GetFileNameWithoutExtension(scenePath)}.png";
                if (EditorUtility.DisplayCancelableProgressBar("ステージ写真を撮影中", scenePath,
                        (float)i / settings.Scenes.Length))
                    break;

                try
                {
                    CaptureScene(scenePath, settings, outputPath);
                    ImportSprite(outputPath);
                    captured++;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"ステージ写真の撮影に失敗しました: {scenePath}\n{exception}", settings);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"ステージ写真: {captured}/{settings.Scenes.Length}件を {folder} に保存しました。", settings);
        return captured;
    }

    private static void CaptureScene(string scenePath, StageThumbnailCaptureSettings settings, string outputPath)
    {
        Scene preview = EditorSceneManager.OpenPreviewScene(scenePath);
        try
        {
            Camera camera = FindCamera(preview, settings.CameraName);
            if (camera == null)
            {
                string description = string.IsNullOrWhiteSpace(settings.CameraName)
                    ? "MainCameraタグの有効なCamera"
                    : $"「{settings.CameraName}」という名前のCamera";
                throw new InvalidOperationException($"{description}が見つかりません。撮影用Cameraをシーンに配置してください。");
            }

            byte[] png = RenderPng(camera, preview, settings.Width, settings.Height);
            // .metaは削除しない。GUIDとSpriteの参照を維持したまま内容だけ更新する。
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputPath));
            File.WriteAllBytes(absolutePath, png);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static Camera FindCamera(Scene scene, string cameraName)
    {
        Camera result = null;
        bool useMainCamera = string.IsNullOrWhiteSpace(cameraName);
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                bool matches = useMainCamera
                    ? camera.isActiveAndEnabled && camera.CompareTag("MainCamera")
                    : camera.gameObject.activeInHierarchy && camera.name == cameraName;
                if (!matches)
                    continue;

                if (result != null)
                    throw new InvalidOperationException("撮影対象のCameraが複数あります。Cameraの名前を指定し、対象を1つにしてください。");
                result = camera;
            }
        }
        return result;
    }

    private static byte[] RenderPng(Camera camera, Scene scene, int width, int height)
    {
        if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData) &&
            cameraData.renderType == CameraRenderType.Overlay)
            throw new InvalidOperationException("Overlay Cameraは単独で撮影できません。Base Cameraを指定してください。");

        var previousActive = RenderTexture.active;
        var previousTarget = camera.targetTexture;
        var previousScene = camera.scene;
        var previousRect = camera.rect;
        float previousAspect = camera.aspect;
        var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        Texture2D image = null;
        try
        {
            // Camera StackのOverlay Cameraにも、編集中の別シーンを映させない。
            foreach (var root in scene.GetRootGameObjects())
            foreach (var sceneCamera in root.GetComponentsInChildren<Camera>(true))
                sceneCamera.scene = scene;
            camera.targetTexture = renderTexture;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.aspect = (float)width / height;
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                camera.Render();
            }
            else
            {
                // URPでもポストプロセスとCamera Stackを含む通常の描画経路を使う。
                var request = new RenderPipeline.StandardRequest { destination = renderTexture };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new NotSupportedException("このCameraまたはRender Pipelineは撮影に対応していません。URPではBase Cameraを指定してください。");
                RenderPipeline.SubmitRenderRequest(camera, request);
            }

            RenderTexture.active = renderTexture;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            return image.EncodeToPNG();
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.scene = previousScene;
            camera.rect = previousRect;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            if (image != null)
                Object.DestroyImmediate(image);
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    private static void ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"PNGをインポートできませんでした: {path}");

        // 再撮影では既存のインポート設定も維持する。
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
    }
}
