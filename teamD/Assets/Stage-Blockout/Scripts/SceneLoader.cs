using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    [SerializeField, Tooltip("次のシーン名")]
    private string _nextSceneName;

    // 次のシーンに遷移するメソッド
    public void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(_nextSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }
        else
        {
            Debug.LogWarning("次のシーン名が設定されていません。");
        }
    }
}
