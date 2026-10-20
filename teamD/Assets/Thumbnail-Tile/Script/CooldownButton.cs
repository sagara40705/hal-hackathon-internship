using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CooldownButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField, Min(0f)] private float cooldownSeconds = 5f;
    [SerializeField] private UnityEvent onPressed = new UnityEvent();

    private bool isCoolingDown;

    // ButtonのOnClickから呼び出す
    public void Press()
    {
        if (isCoolingDown || button == null)
            return;

        isCoolingDown = true;
        button.interactable = false;

        //ボタンを押したときの処理を実行する
        onPressed.Invoke();

        StartCoroutine(CooldownRoutine());


    }

    // クールダウン終了後にボタンを再び有効にする
    private IEnumerator CooldownRoutine()
    {
        // ゲームが一時停止していても時間を数える
        yield return new WaitForSecondsRealtime(cooldownSeconds);

        isCoolingDown = false;
        button.interactable = true;

    }

}
