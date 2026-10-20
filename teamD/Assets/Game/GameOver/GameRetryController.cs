using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームのリトライ処理
/// </summary>
public class GameRetryController : MonoBehaviour
{
	[SerializeField, Tooltip("フェードUI")]
	private FadeUI _fadeUI;

	public void Retry()
	{
		if (_fadeUI != null)
		{
			_fadeUI.StartFadeOut(() =>
			{
				SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			});
		}
	}
}