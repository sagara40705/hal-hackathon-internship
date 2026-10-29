// Unity Pipeline: unity command run_script --file Tools/Popup/BuildPopupArtwork.cs --entry BuildPopupArtwork.Build
// UI素材はimage_gen、ゲーム画面はUnityの実撮影。文字・配置はこのファイルで再現する。
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public static class BuildPopupArtwork
{
    private const string Root = "Assets/Game/Popup/";
    private static TMP_FontAsset font;
    private static Sprite panel, button, mouse;
    private static Transform canvasRoot;
    private static readonly Color Ink = new Color32(39, 44, 49, 255);
    private static readonly Color Muted = new Color32(91, 95, 98, 255);
    private static readonly Color Blue = new Color32(30, 111, 168, 255);
    private static readonly Color Teal = new Color32(30, 104, 89, 255);
    private static readonly Color Mint = new Color32(120, 241, 185, 255);
    private static readonly Color Gold = new Color32(246, 212, 109, 255);

    public static Task<string> Build() => BuildSelected(-1);

    public static async Task<string> BuildSelected(int selected)
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        panel = Import("TutorialPanel", new Vector4(130, 110, 130, 100));
        button = Import("StartButton", new Vector4(180, 120, 180, 80));
        mouse = Import("MouseLeft", Vector4.zero);
        foreach (var capture in new[] { "BasicOverview", "BossAttack", "JumpLesson" }) ImportCapture(capture);
        font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/LINESeedJP-Bold.ttf"),
            72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        try
        {
            if (selected < 0 || selected == 0) await Compose(0, "FirstStage.png");
            if (selected < 0 || selected == 1) await Compose(1, "BossStage.png");
            if (selected < 0 || selected == 2) await Compose(2, "Jump.png");
        }
        finally
        {
            foreach (var atlas in font.atlasTextures) if (atlas != null) UnityEngine.Object.DestroyImmediate(atlas);
            UnityEngine.Object.DestroyImmediate(font.material);
            UnityEngine.Object.DestroyImmediate(font);
        }
        ApplyPrefabs();
        AssetDatabase.SaveAssets();
        return "Three game-specific lesson layouts rebuilt. Generated mint panel, start button and left-mouse artwork applied. Single-image catalog IDs preserved.";
    }

    private static Sprite Import(string name, Vector4 border)
    {
        string path = Root + "Texture/Generated/GameStyle/" + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 300;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels = texture.GetPixels32();
        if (pixels[(texture.height / 2) * texture.width + texture.width / 2].a < 240)
            throw new Exception(name + " has unwanted central transparency.");
        int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
        // 薄い遠方の影を含めず、素材本体の不透明な境界をSprite矩形に採用。
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a < 230) continue;
            minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
        }
        minX = Mathf.Max(0, minX - 4); minY = Mathf.Max(0, minY - 4);
        maxX = Mathf.Min(texture.width - 1, maxX + 4); maxY = Mathf.Min(texture.height - 1, maxY + 4);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var old = provider.GetSpriteRects().FirstOrDefault(s => s.name == name);
        var guid = old == null ? GUID.Generate() : old.spriteID;
        provider.SetSpriteRects(new[] { new SpriteRect { name = name, spriteID = guid,
            rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
            pivot = new Vector2(0.5f, 0.5f), alignment = SpriteAlignment.Center, border = border } });
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(name, guid) });
        provider.Apply();
        importer.isReadable = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(s => s.name == name);
    }

    private static void ImportCapture(string name)
    {
        string path = Root + "Texture/Captures/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        // 1920x1080を2048x1024へ変換すると見た目と案内線の座標がずれる。
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture.width != 1920 || texture.height != 1080) throw new Exception("Capture dimensions changed: " + name);
    }

    private static async Task Compose(int kind, string output)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var cameraObject = new GameObject("ArtworkCamera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.GetComponent<Camera>();
        camera.scene = scene;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true; camera.orthographicSize = 540;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.cullingMask = 1 << 5; camera.allowHDR = false;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        var rt = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = rt;
        var previous = RenderTexture.active;
        Texture2D result = null;
        try
        {
            var root = new GameObject("ArtworkCanvas", typeof(RectTransform), typeof(Canvas));
            root.layer = 5; SceneManager.MoveGameObjectToScene(root, scene);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            canvasRoot = root.transform;
            Surface("Panel", panel, 12, 10, 1896, 1060, Color.white);
            if (kind == 0) BasicLesson();
            else if (kind == 1) BossLesson();
            else JumpLesson();
            root.SetActive(false);
            await Task.Delay(80);
            root.SetActive(true);
            await Task.Delay(100);
            foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>())
            {
                label.ForceMeshUpdate();
                if (label.isTextOverflowing) throw new Exception(output + " text overflow: " + label.name);
            }
            Canvas.ForceUpdateCanvases(); await Task.Delay(100);
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                {
                    graphic.SetAllDirty();
                    graphic.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
                    graphic.canvasRenderer.cull = false;
                }
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt });
                await Task.Delay(80);
            }
            RenderTexture.active = rt;
            result = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); result.Apply();
            if (result.GetPixel(960, 1000).a < .99f) throw new Exception("Missing opaque panel: " + output);
            File.WriteAllBytes(Root + "Texture/" + output, result.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            if (result != null) UnityEngine.Object.DestroyImmediate(result);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        string path = Root + "Texture/" + output;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = 2048; importer.mipmapEnabled = false; importer.isReadable = false;
        importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void BasicLesson()
    {
        Title("タイルを動かして、道をつなごう");
        var point = Photo("BasicOverview.png", new Rect(360, 90, 900f * 1042 / 638, 900), 94, 232, 1042, 638);
        LabelTo("青いキューブ\n自動で進みます", 125, 571, 252, 96, point(615, 752), new Vector2(252, 667), Blue, 29);
        LabelTo("カギ", 350, 411, 145, 58, point(812, 490), new Vector2(420, 469), Ink, 31);
        LabelTo("ゴール", 824, 295, 165, 58, point(1208, 240), new Vector2(824, 324), Ink, 31);
        LabelTo("矢印を踏むと\nその向きに進みます", 748, 744, 350, 98, point(1150, 680), new Vector2(748, 792), Ink, 30);

        Text("InputHeading", "タイルの動かしかた", 44, 1220, 235, 600, 66, Teal);
        Surface("MouseLeft", mouse, 1280, 323, 150, 245, Color.white);
        Text("LeftButton", "左ボタン", 38, 1500, 368, 290, 60, Ink);
        Arrow(new Vector2(1490, 400), new Vector2(1330, 403), Teal, 5, 17);
        Arrow(new Vector2(1290, 608), new Vector2(1760, 608), Teal, 7, 22);
        Arrow(new Vector2(1760, 608), new Vector2(1290, 608), Teal, 7, 22);
        Text("HoldAndMove", "押したまま動かして", 45, 1215, 655, 600, 66, Ink);
        Text("Release", "離すと、その場所に置けます", 34, 1215, 738, 610, 62, Muted);
        Text("Goal", "必要な数のカギを集めて、ゴールへ", 34, 98, 897, 1160, 58, Ink);
        Caution("道が途切れると落下します。");
    }

    private static void BossLesson()
    {
        Title("攻撃タイルでボスを倒そう");
        Text("Guide", "道をつないで、青いキューブを攻撃タイルへ導こう", 32, 100, 177, 1680, 50, Muted);
        var point = Photo("BossAttack.png", new Rect(410, 40, 1510, 1510f * 620 / 1096), 94, 254, 1096, 620);
        LabelTo("ボスの体力", 854, 360, 280, 62, point(1460, 99), new Vector2(994, 360), Ink, 33);
        LabelTo("踏むと攻撃！", 143, 565, 278, 66, point(790, 650), new Vector2(330, 631), Ink, 34);
        LabelTo("攻撃タイル", 826, 774, 265, 64, point(1397, 690), new Vector2(940, 774), Ink, 33);

        Text("RepeatHeading", "攻撃タイルを\n交互に使おう", 52, 1250, 262, 565, 148, Teal);
        Text("Reactivation", "別の攻撃タイルを踏むと、\n使ったタイルがまた使えます。", 33, 1240, 435, 590, 102, Ink);
        // Actual attack-tile crops, with a cycle arrow rather than a solved route through the level.
        Box("CycleBackdrop", 1230, 592, 590, 209, new Color32(12, 6, 34, 255));
        Photo("BossAttack.png", new Rect(675, 548, 270, 240), 1252, 595, 215, 191, false);
        Photo("BossAttack.png", new Rect(1260, 565, 270, 240), 1582, 595, 215, 191, false);
        Arrow(new Vector2(1467, 650), new Vector2(1580, 650), Mint, 6, 18);
        Arrow(new Vector2(1580, 735), new Vector2(1467, 735), Mint, 6, 18);
        Text("CycleCaption", "使うたびに切り替わる", 34, 1250, 823, 560, 55, Teal, TextAlignmentOptions.Center);
        Text("Trigger", "キューブが赤いタイルを踏むと攻撃！", 33, 98, 898, 1190, 57, Ink);
        Text("VictoryGoal", "ボスの体力をゼロにしよう", 36, 100, 974, 1200, 60, Teal);
    }

    private static void JumpLesson()
    {
        Title("ジャンプ台で飛び越えよう");
        Text("Guide", "トゲやすき間の手前に、ジャンプ台を置こう", 32, 100, 177, 1660, 50, Muted);
        var point = Photo("JumpLesson.png", new Rect(350, 40, 1450, 1450f * 638 / 1096), 94, 244, 1096, 638);
        // Positions projected by the gameplay camera in CaptureJumpLesson: cells (6,5), (7,5), (8,5).
        var landing = point(1069.10f, 457.41f);
        Diamond(landing, 82.5f, 58.3f, Mint);
        LabelTo("ジャンプ台", 135, 690, 252, 64, point(850.91f, 574), new Vector2(387, 722), Ink, 33);
        LabelTo("着地する場所", 858, 560, 287, 66, landing, new Vector2(858, 593), Teal, 33);

        Text("JumpRule", "1マス飛び越える", 49, 1240, 273, 590, 74, Teal);
        Text("Automatic", "台を踏むと、自動でジャンプ。", 32, 1235, 376, 600, 52, Ink);

        // Side view: the numbers describe distance only. The three cells are labelled by role.
        Cell("台", 1245, 610, Gold);
        Cell("トゲ", 1445, 610, new Color32(234, 140, 127, 255));
        Cell("着地", 1645, 610, Mint);
        Arc(new Vector2(1320, 604), new Vector2(1520, 335), new Vector2(1720, 604), Teal, 7);
        Text("SkippedCell", "このマスを越える", 27, 1383, 724, 280, 48, Muted, TextAlignmentOptions.Center);
        Line(new Vector2(1320, 815), new Vector2(1720, 815), 4, Teal);
        Line(new Vector2(1320, 802), new Vector2(1320, 828), 4, Teal);
        Line(new Vector2(1720, 802), new Vector2(1720, 828), 4, Teal);
        Text("Distance", "進む向きの2マス先に着地", 34, 1225, 853, 610, 61, Teal, TextAlignmentOptions.Center);
        Caution("着地先にもタイルが必要です。");
    }

    private static void Title(string title)
    {
        Box("TitleShadow", 98, 164, 220, 8, Teal);
        Box("TitleGold", 94, 158, 220, 8, Gold);
        Text("Title", title, 64, 94, 65, 1740, 100, Ink);
    }

    private static void Caution(string message)
    {
        Box("CautionAccent", 99, 969, 7, 48, Teal);
        Text("Caution", message, 33, 125, 969, 1170, 65, Ink);
    }

    private static Func<float, float, Vector2> Photo(string source, Rect crop, float x, float y, float w, float h, bool frame = true)
    {
        if (Mathf.Abs(crop.width / crop.height - w / h) > .003f) throw new Exception("Screenshot aspect mismatch: " + source);
        var shot = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Texture/Captures/" + source);
        if (shot == null) throw new Exception("Missing capture: " + source);
        if (frame)
        {
            Box("PhotoShadow", x - 5, y + 5, w + 10, h + 7, new Color32(182, 207, 195, 255));
            Box("PhotoBorder", x - 5, y - 5, w + 10, h + 10, Ink);
        }
        var raw = Rect("Gameplay", x, y, w, h).gameObject.AddComponent<UnityEngine.UI.RawImage>();
        raw.texture = shot; raw.raycastTarget = false;
        raw.uvRect = new Rect(crop.x / shot.width, 1 - (crop.y + crop.height) / shot.height, crop.width / shot.width, crop.height / shot.height);
        return (sx, sy) => new Vector2(x + (sx - crop.x) * w / crop.width, y + (sy - crop.y) * h / crop.height);
    }

    private static void LabelTo(string label, float x, float y, float w, float h, Vector2 target, Vector2 edge, Color color, float size)
    {
        Line(edge, target, 9, new Color32(35, 48, 44, 255));
        Line(edge, target, 4, Color.white);
        Box("LabelShadow", x + 3, y + 5, w, h, new Color32(36, 57, 49, 210));
        Box("Label", x, y, w, h, new Color32(247, 253, 249, 255));
        Text("LabelText", label, size, x + 9, y - 2, w - 18, h, color, TextAlignmentOptions.Center);
    }

    private static void Cell(string label, float x, float y, Color color)
    {
        Box("CellSide", x, y + 17, 150, 85, Teal);
        Box("CellTop", x, y, 150, 85, color);
        Text("CellLabel", label, 35, x, y - 3, 150, 85, Ink, TextAlignmentOptions.Center);
    }

    private static void Arrow(Vector2 a, Vector2 b, Color color, float thickness, float head)
    {
        Line(a, b, thickness, color);
        var d = (b - a).normalized;
        var side = new Vector2(-d.y, d.x);
        Line(b, b - d * head + side * head * .65f, thickness, color);
        Line(b, b - d * head - side * head * .65f, thickness, color);
    }

    private static void Arc(Vector2 a, Vector2 control, Vector2 b, Color color, float thickness)
    {
        var previous = a;
        for (int i = 1; i <= 32; i++)
        {
            float t = i / 32f;
            var p = (1-t)*(1-t)*a + 2*(1-t)*t*control + t*t*b;
            if (i == 32) Arrow(previous, p, color, thickness, 21);
            else Line(previous, p, thickness, color);
            previous = p;
        }
    }

    private static void Diamond(Vector2 center, float halfWidth, float halfHeight, Color color)
    {
        var points = new[] { center + new Vector2(-halfWidth, 0), center + new Vector2(0, -halfHeight), center + new Vector2(halfWidth, 0), center + new Vector2(0, halfHeight) };
        for (int i = 0; i < 4; i++)
        {
            Line(points[i], points[(i+1)%4], 11, Ink);
            Line(points[i], points[(i+1)%4], 6, color);
        }
    }

    private static void ApplyPrefabs()
    {
        string path = Root + "Prefab/PopupCloseButton.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(460, 112);
            var image = root.GetComponent<UnityEngine.UI.Image>();
            image.sprite = button; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = Color.white;
            var control = root.GetComponent<UnityEngine.UI.Button>();
            var colors = control.colors;
            colors.normalColor = new Color(.96f, .96f, .96f);
            colors.highlightedColor = Color.white; colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.74f, .88f, .81f); colors.fadeDuration = .09f;
            control.colors = colors;
            if (!root.GetComponent<PopupButtonFeedback>()) root.AddComponent<PopupButtonFeedback>();
            // シーン開始なので「はじめる」。閉じる処理と参照はそのまま維持する。
            var icon = (RectTransform)root.transform.Find("CloseIcon");
            icon.GetComponent<UnityEngine.UI.Image>().enabled = false;
            icon.GetComponent<UnityEngine.UI.Image>().sprite = null;
            icon.gameObject.SetActive(false);
            var label = root.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            label.rectTransform.anchoredPosition = new Vector2(0, 8);
            label.rectTransform.sizeDelta = new Vector2(400, 86);
            label.fontSize = 50; label.color = Ink;
            label.text = "はじめる"; label.textWrappingMode = TextWrappingModes.NoWrap;
            string materialPath = Root + "Texture/Generated/GameStyle/StartButtonText.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(label.fontSharedMaterial);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetColor("_OutlineColor", Color.white);
            material.SetFloat("_OutlineWidth", .17f);
            label.fontSharedMaterial = material;
            EditorUtility.SetDirty(material);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        path = Root + "Prefab/StageStartPopup.prefab";
        root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var overlay = root.transform.Find("PopupOverlay");
            overlay.GetComponent<UnityEngine.UI.Image>().color = new Color(.035f, .045f, .06f, .9f);
            var card = (RectTransform)overlay.Find("Card");
            card.GetComponent<UnityEngine.UI.Image>().enabled = false;
            card.GetComponent<UnityEngine.UI.Image>().sprite = null;
            if (!card.GetComponent<PopupCardPresentation>()) card.gameObject.AddComponent<PopupCardPresentation>();
            card.anchorMin = card.anchorMax = new Vector2(.5f, .5f);
            card.anchoredPosition = Vector2.zero; card.sizeDelta = new Vector2(1920, 1080);
            var image = card.Find("PopupImage").GetComponent<UnityEngine.UI.Image>();
            image.rectTransform.anchorMin = Vector2.zero; image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.preserveAspect = true;
            var close = (RectTransform)card.GetComponentInChildren<UnityEngine.UI.Button>(true).transform;
            close.anchorMin = close.anchorMax = new Vector2(.5f, .5f);
            close.anchoredPosition = new Vector2(625, -448); close.sizeDelta = new Vector2(460, 112);
            close.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(close);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static RectTransform Rect(string name, float x, float y, float w, float h)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5; rect.SetParent(canvasRoot, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        return rect;
    }
    private static void Surface(string name, Sprite sprite, float x, float y, float w, float h, Color color)
    {
        var image = Rect(name, x, y, w, h).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.type = sprite == mouse ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
        image.color = color; image.raycastTarget = false;
    }
    private static void Box(string name, float x, float y, float w, float h, Color color) => Surface(name, null, x, y, w, h, color);
    private static void Text(string name, string value, float size, float x, float y, float w, float h, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        var label = Rect(name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = value; label.fontSize = size; label.color = color; label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
    }
    private static void Line(Vector2 a, Vector2 b, float thickness, Color color)
    {
        var rect = Rect("Leader", a.x, a.y, Vector2.Distance(a, b), thickness);
        rect.pivot = new Vector2(0, .5f);
        rect.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
    }
}
