using UnityEngine;

public class EnergyGridTransitionButton : MonoBehaviour
{
    [SerializeField, Tooltip("遷移先のシーン名")]
    private string _sceneName;

    public void ChangeScene()
    {
        EnergyGridTransition transition =
            EnergyGridTransition.Instance;

        if (transition == null)
        {
            Debug.LogError(
                "PFB_EnergyGridTransitionをシーンに配置してください。",
                this
            );

            return;
        }

        transition.TransitionTo(_sceneName);
    }
}
