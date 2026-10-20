using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

	private UnityAction _action;

	public void StartFadeOut(UnityAction action = null)
	{
		_action = action;
		_isFade = true;
	}

	private void Update()
	{
		if (_isFade)
		{
			_fadeRatio += _fadeSpeed * Time.deltaTime;
		}
		else
		{
			_fadeRatio -= _fadeSpeed * Time.deltaTime;
		}
		_fadeRatio = Mathf.Clamp01(_fadeRatio);

		Color color = _image.color;
		color.a = _fadeRatio;
		_image.color = color;

		if (_isFade && _fadeRatio >= 1.0f)
		{
			_action?.Invoke();
		}
	}
}
