using JetBrains.Annotations;
using UnityEngine;


/// <summary>
/// ゲームを終了する
/// 作成者 : 酒井康佑
/// </summary>
public class GameQuit : MonoBehaviour
{
   public void QuitGame()
    {
#if UNITY_EDITOR
        // Unity Editor上では、ゲームの再生を停止する
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ビルドしたゲームでは、アプリケーションを終了する
        Application.Quit();
#endif

    }
}
