using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [SerializeField, Tooltip("遷移先シーン")]
    private SceneAsset _targetScene;

    [SerializeField, Tooltip("フェードUI")]
    private FadeUI _fadeUI;

    public void ChangeScene()
    {
        if (_targetScene == null)
            return;

        string sceneName = _targetScene.name;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning(sceneName + "が見つかりません。ビルドシーンに含まれているか確認してください。");
            return;
        }

        if (_fadeUI != null)
        {
            _fadeUI.StartFadeOut(() =>
            {
                SceneManager.LoadScene(sceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
