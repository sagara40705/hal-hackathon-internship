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
    public void Retry()
	{
        Time.timeScale = 1f;
        if (_fadeUI != null)
		{
			_fadeUI.StartFadeOut(() =>
			{
				SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			});
		}
	}
}
