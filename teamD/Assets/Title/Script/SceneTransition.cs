using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ボタンまたはEnterで、フェードアウト後にシーンを切り替える。
/// 作成者 : 酒井康佑
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [SerializeField, Tooltip("遷移先のシーン名")]
    private string _sceneName;

    [SerializeField, Tooltip("シーン内のFadeUI")]
    private FadeUI _fadeUI;

    [SerializeField, Tooltip("Enterキーでも遷移する")]
    private bool _useEnterKey = true;

    private bool _isTransitioning;
    private bool _hasRequestedLoad;

    private void Update()
    {
        if (!_useEnterKey || _isTransitioning)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // 通常のEnter、またはテンキーのEnterで遷移する。
        if (keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            LoadScene();
        }
    }

    // ボタンのOn Clickから呼び出す。
    public void LoadScene()
    {
        // フェード中の連打による二重実行を防ぐ。
        if (_isTransitioning)
            return;

        if (string.IsNullOrWhiteSpace(_sceneName))
        {
            Debug.LogWarning("遷移先のシーン名が未設定です。", this);
            return;
        }

        if (_fadeUI == null || !_fadeUI.isActiveAndEnabled)
        {
            Debug.LogWarning(
                "シーン内の有効なFadeUIを設定してください。",
                this
            );
            return;
        }

        _isTransitioning = true;

        // FadeUIはTime.deltaTimeを使うため、一時停止を解除する。
        Time.timeScale = 1f;

        // フェードが完了したらシーンを読み込む。
        _fadeUI.StartFadeOut(ChangeScene);
    }

    private void ChangeScene()
    {
        // 現在のFadeUIは完了後も通知するため、一度だけ処理する。
        if (_hasRequestedLoad)
            return;

        _hasRequestedLoad = true;
        SceneManager.LoadScene(_sceneName);
    }
}
