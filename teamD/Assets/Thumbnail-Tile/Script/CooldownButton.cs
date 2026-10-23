using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 自動補充までのクールタイムを、画面の充填量で表示する
///  作成者 : 酒井康佑
/// </summary>
public class CooldownButton : MonoBehaviour
{
    [SerializeField, Tooltip("以前の補充ボタン")]
    private Button _button;

    [SerializeField, Min(0f), Tooltip("自動補充までの秒数")]
    private float _cooldownSeconds = 5f;

    [SerializeField, Tooltip("クールタイムを表示するImage")]
    private Image _buttonImage;

    [SerializeField, Tooltip("クールタイム完了時に再生するエフェクト")]
    private ButtonClickEffect _completionEffect;

    private bool _isCoolingDown;


    private void Awake()
    {
        // Imageが未設定なら、指定されたボタンから取得する
        if(_buttonImage == null && _button != null)
            _buttonImage = _button.GetComponent<Image>();

        // 起動時は空にする
        if (_buttonImage != null)
            _buttonImage.fillAmount = 0f;

        if(_button != null)
            _button.interactable = false;

    }

    /// <summary>
    /// クールタイムを開始する
    /// 終了時にonCompletedを実行する
    /// </summary>   
    public void StartCooldown(Action onCompleted)
    {
        if (_isCoolingDown)
            return;

        if (!isActiveAndEnabled)
            return;

        _isCoolingDown = true;

        // 使い切った瞬間にゲージを空にする
        if (_buttonImage != null)
            _buttonImage.fillAmount = 0f;

        StartCoroutine(CooldownRoutine(onCompleted));
    }


    /// <summary>
    /// 時間の経過に合わせて、ボタン画像を0から1へ戻す
    /// </summary>    
    private IEnumerator CooldownRoutine(Action onCompleted)
    {
        float duration = Mathf.Max(0f, _cooldownSeconds);
        float startTime = Time.realtimeSinceStartup;


        while (duration > 0f)
        {
            // ゲームの一時停止中も経過時間を数える
            float elapsed = Time.realtimeSinceStartup - startTime;

            if (elapsed >= duration)
                break;

            // 経過時間に応じて、カラカラ満タンへ戻す
            if (_buttonImage != null)
            {
                _buttonImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            }

            yield return null;

        }


        // 終了時は確実に満タンにする。
        if (_buttonImage != null)
            _buttonImage.fillAmount = 0f;

        _isCoolingDown = false;

        // サムネイルを補充する。
        onCompleted?.Invoke();

        // 満タンになったことを知らせるエフェクトを再生する。
        if (_completionEffect != null)
            _completionEffect.Play();
    }
}
