using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームのリトライ処理
/// 作成者：石田怜
/// </summary>
public class GameRetryController : MonoBehaviour
{
	[SerializeField, Tooltip("フェードUI")]
	private FadeUI _fadeUI;

    // リトライ処理
    // リトライは前後とも通常フェードを使う。
    public void Retry()
    {
        if (_fadeUI == null)
            return;

        _fadeUI.StartFadeOut(FadeType.Normal, FadeType.Normal, () =>
        {
            // 次のゲームが停止状態で始まらないようにする。
            Time.timeScale = 1f;

            SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex);
        });
    }
}
