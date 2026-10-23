using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
///  作成者 : 酒井康佑
/// </summary>
public class CooldownButton : MonoBehaviour
{
    [SerializeField, Tooltip("操作する補充ボタン")]
    private Button _button;

    [SerializeField, Min(0f), Tooltip("再び押せるまでの秒数")]
    private float _cooldownSeconds = 5f;

    [SerializeField, Tooltip("ボタンを押したときに実行する処理")]
    private UnityEvent _onPressed = new UnityEvent();

    [SerializeField, Tooltip("ボタン自身についてるImage")]
    private Image _buttonImage;

    private bool _isCoolingDown;


    private void Awake()
    {
        // Imageが未設定なら、指定されたボタンから取得する
        if(_buttonImage == null && _button != null)
            _buttonImage = _button.GetComponent<Image>();

        // 起動時は満タンにする
        if (_buttonImage != null)
            _buttonImage.fillAmount = 1f;
    }

    // ButtonのOnClickから呼び出す
    public void Press()
    {
        if (_isCoolingDown || _button == null)
            return;

        _isCoolingDown = true;
        _button.interactable = false;

        // 押した瞬間にボタン画像を空にする
        if (_buttonImage != null)
            _buttonImage.fillAmount = 0f;

        //ボタンを押したときの処理を実行する
        _onPressed.Invoke();

        StartCoroutine(CooldownRoutine());


    }

    /// <summary>
    /// 時間の経過に合わせて、ボタン画像を0から1へ戻す
    /// </summary>    
    private IEnumerator CooldownRoutine()
    {
        float duration = Mathf.Max(0f, _cooldownSeconds);
        float startTime = Time.realtimeSinceStartup;

        if(duration > 0f)
        {
            while(true)
            {
                // ゲームの一時停止中も経過時間を数える
                float elapsed = Time.realtimeSinceStartup - startTime;

                if (elapsed >= duration)
                    break;

                // 経過時間に応じて、カラカラ満タンへ戻す
                if(_buttonImage != null)
                {
                    _buttonImage.fillAmount = Mathf.Clamp01(elapsed / duration);
                }

                yield return null;
              
            }
        }

        // 終了時は確実に満タンにする
        if (_buttonImage != null)
            _buttonImage.fillAmount = 1f;

        _isCoolingDown = false;

        if (_button != null)
            _button.interactable = true;

    }

}
