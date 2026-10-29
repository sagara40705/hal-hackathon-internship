using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor;

/// <summary>
/// ステージ情報のUI
/// </summary>
public class StageInfoUI : MonoBehaviour
{
	[SerializeField, Tooltip("ステージ名を表示するText")]
	private TextMeshProUGUI _stageNameText;

	[SerializeField, Tooltip("ステージのサムネイルを表示するImage")]
	private Image _stageThumbnailImage;

	[SerializeField, Tooltip("ステージ開始ボタン")]
	private Button _startButton;

	[SerializeField, Tooltip("フェードUI")]
	private FadeUI _fadeUI;

	private SceneAsset _currentInfoStageScene;

	// ステージ情報を設定する
	public void SetStageInfo(StageData stageData)
	{
		if (stageData == null)
		{
            if (_stageNameText != null)
                _stageNameText.gameObject.SetActive(false);

            if (_stageThumbnailImage != null)
                _stageThumbnailImage.gameObject.SetActive(false);

            if (_startButton != null)
                _startButton.gameObject.SetActive(false);

        }
		else
		{
            if (_stageNameText != null)
            {
                _stageNameText.gameObject.SetActive(true);
                _stageNameText.text = stageData.StageName;
            }

            if (_stageThumbnailImage != null)
            {
                _stageThumbnailImage.gameObject.SetActive(true);
                _stageThumbnailImage.sprite = stageData.StageThumbnail;
            }

            if (_startButton)
            {
                _startButton.gameObject.SetActive(true);
            }

			_currentInfoStageScene = stageData.Scene;
		}
	}

	public void LoadStageScene()
	{
        if (_currentInfoStageScene == null)
        {
            Debug.LogWarning("ステージ情報が設定されていません。");
            return;
        }

        if (_fadeUI == null)
        {
            SceneManager.LoadScene(_currentInfoStageScene.name);
            return;
        }
        else
        {
            _fadeUI.StartFadeOut(() =>
            {
                SceneManager.LoadScene(_currentInfoStageScene.name);
            });
        }
	}
}
