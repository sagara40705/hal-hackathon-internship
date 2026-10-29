using UnityEngine;

public class GridTransitionButton : MonoBehaviour
{
    [SerializeField, Tooltip("遷移先のシーン名")]
    private string _sceneName;

    // ButtonのOn Clickから呼ぶ。
    public void ChangeScene()
    {
        SquareGridTransition transition =
            SquareGridTransition.Instance;

        if (transition == null)
        {
            Debug.LogError(
                "シーンにPFB_SquareGridTransitionを配置してください。",
                this
            );

            return;
        }

        transition.TransitionTo(_sceneName);
    }
}
