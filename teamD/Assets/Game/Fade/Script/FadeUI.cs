using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// フェードイン・フェードアウトを行うUI
/// 作成者：石田怜
/// </summary>
public class FadeUI : MonoBehaviour
{
	[SerializeField, Tooltip("フェード状態")]
	private bool _isFade = false;

	[SerializeField, Tooltip("フェード割合")]
	private float _fadeRatio = 1.0f;

	[SerializeField, Tooltip("フェード速度")]
	private float _fadeSpeed = 1.0f;

	[SerializeField, Tooltip("フェード用イメージ")]
	private Image _image;

	private UnityAction _action;        // フェードアウト完了時のコールバック

    // フェードアウトを開始する
    public void StartFadeOut(UnityAction action = null)
	{
		_action = action;
		_isFade = true;
	}

	private void Update()
	{
        // フェード進行
		if (_isFade)
		{
			_fadeRatio += _fadeSpeed * Time.deltaTime;
		}
		else
		{
			_fadeRatio -= _fadeSpeed * Time.deltaTime;
		}
		_fadeRatio = Mathf.Clamp01(_fadeRatio);

        // アルファ値を更新
        Color color = _image.color;
		color.a = _fadeRatio;
		_image.color = color;

        // フェードアウト完了時のコールバックを呼び出す
        if (_isFade && _fadeRatio >= 1.0f)
		{
			_action?.Invoke();
		}
	}
}
