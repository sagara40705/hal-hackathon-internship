using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class VerifyPopupPresentation
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    public static string Prepare()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop first.");
        SessionState.SetString("PopupGameStyle.start", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool("PopupGameStyle.background", PlayerSettings.runInBackground);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Game/Popup/PopupTest.unity");
        Application.runInBackground = true;
        return "PopupTest prepared.";
    }
    public static async Task<object> Verify()
    {
        if (SceneManager.GetActiveScene().path != "Assets/Game/Popup/PopupTest.unity") throw new Exception("Wrong scene.");
        var popup = UnityEngine.Object.FindFirstObjectByType<StageStartPopup>();
        if (popup == null || !StageStartPopup.IsShowing || Time.timeScale != 0) throw new Exception("Popup not paused.");
        var card = (RectTransform)popup.transform.Find("PopupOverlay/Card");
        var art = card.Find("PopupImage").GetComponent<UnityEngine.UI.Image>();
        var catalog = AssetDatabase.LoadAssetAtPath<PopupCatalog>("Assets/Game/Popup/PopupCatalog.asset");
        var button = card.GetComponentInChildren<UnityEngine.UI.Button>();
        var buttonRect = (RectTransform)button.transform;
        var rootCanvas = popup.GetComponentInParent<Canvas>().rootCanvas;
        var assembly = typeof(Editor).Assembly;
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        var selection = viewType.GetProperty("selectedSizeIndex", Flags);
        int previous = (int)selection.GetValue(view);
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var sizes = sizesType.GetProperty("instance", Flags | BindingFlags.FlattenHierarchy).GetValue(null);
        var group = sizesType.GetMethod("GetGroup", Flags).Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        var indices = new List<int>();
        var dimensions = new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(1024,768), new Vector2Int(2560,1080) };
        var reports = new List<object>();
        var physicalMouse = Mouse.current;
        var physicalKeyboard = Keyboard.current;
        Mouse testMouse = null;
        Keyboard testKeyboard = null;
        var originalInputSettings = InputSystem.settings;
        InputSettings testInputSettings = null;
        try
        {
            // Background automation must receive events even when the desktop has focus.
            // Clone the settings so no project asset is changed by this verification.
            testInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testInputSettings;
            // Keep native window mouse events from overwriting queued test coordinates.
            // These devices exist only inside Unity's Input System, never at OS level.
            if (physicalMouse != null) InputSystem.DisableDevice(physicalMouse);
            if (physicalKeyboard != null) InputSystem.DisableDevice(physicalKeyboard);
            testMouse = InputSystem.AddDevice<Mouse>("PopupVerificationMouse"); testMouse.MakeCurrent();
            testKeyboard = InputSystem.AddDevice<Keyboard>("PopupVerificationKeyboard"); testKeyboard.MakeCurrent();
            foreach (var dimension in dimensions)
            {
                int index = (int)group.GetType().GetMethod("GetBuiltinCount", Flags).Invoke(group, null)
                    + (int)group.GetType().GetMethod("GetCustomCount", Flags).Invoke(group, null);
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var mode = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, Flags, null,
                    new object[] { Enum.Parse(mode, "FixedResolution"), dimension.x, dimension.y, "Popup Quality Check" }, null);
                group.GetType().GetMethod("AddCustomSize", Flags).Invoke(group, new[] { size });
                indices.Add(index); selection.SetValue(view, index); view.Focus(); view.Repaint();
                await Task.Delay(600);
                Canvas.ForceUpdateCanvases();
                if (Screen.width != dimension.x || Screen.height != dimension.y) throw new Exception("Resolution did not change: " + Screen.width + "x" + Screen.height);
                var ids = new[] { "first_stage", "boss_stage", "jump" };
                foreach (string id in ids)
                {
                    if (!catalog.TryGetImage(id, out var sprite) || sprite.texture.width != 1920 || sprite.texture.height != 1080) throw new Exception("Missing image: " + id);
                    art.sprite = sprite; await Task.Delay(140);
                    var bounds = ScreenBounds(card);
                    var hitbox = ScreenBounds(buttonRect);
                    if (bounds.xMin < 0 || bounds.yMin < 0 || bounds.xMax > Screen.width || bounds.yMax > Screen.height) throw new Exception("Card clipped.");
                    if (!bounds.Contains(hitbox.min) || !bounds.Contains(hitbox.max)) throw new Exception("Button outside panel.");
                    if (Mathf.Abs(bounds.width / bounds.height - 16f/9f) > .001) throw new Exception("Image stretched.");
                    if (card.GetComponent<CanvasGroup>().alpha < .999f || Time.timeScale != 0) throw new Exception("Presentation/pause failure.");
                    var captured = new TaskCompletionSource<Texture2D>();
                    popup.StartCoroutine(CaptureFrame(captured));
                    if (await Task.WhenAny(captured.Task, Task.Delay(5000)) != captured.Task) throw new Exception("Game View capture timed out.");
                    var screenshot = await captured.Task;
                    if (screenshot == null) throw new Exception("Game View capture returned null at end of frame.");
                    File.WriteAllBytes(Path.GetFullPath("../output/popup/GameStyle-" + id + "-" + dimension.x + "x" + dimension.y + ".png"), screenshot.EncodeToPNG());
                    UnityEngine.Object.Destroy(screenshot);
                    reports.Add(new { id, resolution = dimension.ToString(), panel = bounds.ToString(), button = hitbox.ToString() });
                }
            }
            // Exercise each catalog ID through StageStartPopup.Start, not just by replacing the Image.
            popup.Close();
            UnityEngine.Object.Destroy(popup.gameObject);
            var interactions = new List<object>();
            foreach (string id in new[] { "first_stage", "boss_stage", "jump" })
            {
                var current = Spawn(rootCanvas, id);
                await Task.Delay(350);
                catalog.TryGetImage(id, out var expected);
                var currentArt = current.transform.Find("PopupOverlay/Card/PopupImage").GetComponent<UnityEngine.UI.Image>();
                if (!StageStartPopup.IsShowing || Time.timeScale != 0 || currentArt.sprite != expected) throw new Exception("ID not displayed: " + id);
                var currentButton = current.GetComponentInChildren<UnityEngine.UI.Button>();
                var currentRect = (RectTransform)currentButton.transform;
                var label = currentButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                label.ForceMeshUpdate();
                if (label.text != "はじめる" || label.isTextOverflowing) throw new Exception("Start button text incorrect.");
                int frame = Time.frameCount;
                float time = Time.time;
                var player = UnityEngine.Object.FindFirstObjectByType<Player>();
                Vector3 position = player.transform.position;
                var mouse = Mouse.current;
                // Click the non-button area: popup must stay open and gameplay must remain blocked.
                var away = new Vector2(Screen.width * .28f, Screen.height * .5f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = away }.WithButton(MouseButton.Left));
                await Task.Delay(80);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = away });
                await Task.Delay(80);
                var controller = UnityEngine.Object.FindFirstObjectByType<TileGridController>();
                var selectedGroup = typeof(TileGridController).GetField("_selectedGroup", Flags);
                if (!StageStartPopup.IsShowing || !StageStartPopup.BlocksGameplayInput || selectedGroup.GetValue(controller) != null) throw new Exception("Gameplay input escaped popup.");
                if (Time.frameCount <= frame || Time.time != time || Vector3.Distance(position, player.transform.position) > .0001f) throw new Exception("Pause/player-loop check failed.");
                bool closeFrameBlocked = false;
                currentButton.onClick.AddListener(() => closeFrameBlocked = StageStartPopup.BlocksGameplayInput && !StageStartPopup.IsShowing);
                var point = ScreenBounds(currentRect).center;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                await Task.Delay(200);
                if (currentRect.localScale.x < 1.01f) throw new Exception("Hover did not animate while paused: " + id);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                await Task.Delay(140);
                if (currentRect.localScale.x > .99f || Time.timeScale != 0) throw new Exception("Pressed feedback/pause failed.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); await Task.Delay(100);
                if (StageStartPopup.IsShowing || Time.timeScale != 1 || !closeFrameBlocked || selectedGroup.GetValue(controller) != null || Time.time <= time) throw new Exception("Mouse close/resume/input guard failed: " + id);
                interactions.Add(new { id, catalogSelection = true, pausedMovement = true, outsideClickBlocked = true, closeFrameBlocked, mouseResume = true });
                UnityEngine.Object.Destroy(current.gameObject);
            }
            var second = Spawn(rootCanvas, "jump");
            await Task.Delay(350);
            if (!StageStartPopup.IsShowing || Time.timeScale != 0) throw new Exception("Keyboard popup did not pause.");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Enter)); await Task.Delay(140);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); await Task.Delay(100);
            if (StageStartPopup.IsShowing || Time.timeScale != 1) throw new Exception("Keyboard submit/resume failed.");
            UnityEngine.Object.Destroy(second.gameObject);
            return new { success = true, previews = reports, interactions, keyboard = "Enter submit and resume passed" };
        }
        finally
        {
            if (testMouse != null) InputSystem.RemoveDevice(testMouse);
            if (testKeyboard != null) InputSystem.RemoveDevice(testKeyboard);
            if (physicalMouse != null) { InputSystem.EnableDevice(physicalMouse); physicalMouse.MakeCurrent(); }
            if (physicalKeyboard != null) { InputSystem.EnableDevice(physicalKeyboard); physicalKeyboard.MakeCurrent(); }
            InputSystem.settings = originalInputSettings;
            if (testInputSettings != null) UnityEngine.Object.Destroy(testInputSettings);
            selection.SetValue(view, previous);
            foreach (int index in indices.AsEnumerable().Reverse()) group.GetType().GetMethod("RemoveCustomSize", Flags).Invoke(group, new object[] { index });
            view.Repaint();
        }
    }
    private static StageStartPopup Spawn(Canvas canvas, string id)
    {
        var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Popup/Prefab/StageStartPopup.prefab"), canvas.transform);
        var popup = go.GetComponent<StageStartPopup>();
        typeof(StageStartPopup).GetField("_popupId", Flags).SetValue(popup, id);
        return popup;
    }
    private static Rect ScreenBounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        var b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return new Rect(a, b - a);
    }
    private static System.Collections.IEnumerator CaptureFrame(TaskCompletionSource<Texture2D> completion)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        completion.SetResult(ScreenCapture.CaptureScreenshotAsTexture());
    }
    public static string Restore()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop first.");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("PopupGameStyle.start", ""));
        PlayerSettings.runInBackground = SessionState.GetBool("PopupGameStyle.background", false);
        AssetDatabase.SaveAssets();
        return "Original start scene and background preference restored; active scene " + SceneManager.GetActiveScene().path + ", dirty=" + SceneManager.GetActiveScene().isDirty;
    }
}
