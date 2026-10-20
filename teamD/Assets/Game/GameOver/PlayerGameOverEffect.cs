using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// プレイヤーのゲームオーバー演出
/// 作成者：石田怜
/// </summary>
public class PlayerGameOverEffect : MonoBehaviour
{
	[SerializeField, Tooltip("プレイヤー")]
	private Player _player;

	[SerializeField, Tooltip("プレイヤーのモデル")]
	private Transform _model;

	[SerializeField, Tooltip("重力加速度")]
	private float _fallGravity = 15.0f;

	[SerializeField, Tooltip("落下時間")]
	private float _fallDuration = 1.0f;

	[SerializeField, Tooltip("回転速度")]
	private float _rotationSpeed = 180.0f;

	[Header("イベント")]
	[SerializeField, Tooltip("ゲームオーバー演出終了時の処理")]
	private UnityEvent _onEffectFinished;

	private Vector3 _initialModelScale;     // モデルの初期スケール
	private float _fallVelocity;            // 落下速度
	private float _fallTimer;               // 落下時間タイマー
	private bool _isPlaying;                // 演出中フラグ

	private void Awake()
	{
		if (_model != null)
			_initialModelScale = _model.localScale;
	}

	// ゲームオーバー演出開始
	public void Play()
	{
		if (_isPlaying)
			return;

		_isPlaying = true;
		_fallVelocity = 0.0f;
		_fallTimer = 0.0f;
	}

	private void Update()
	{
		if (!_isPlaying)
			return;

		if(_player == null)
			return;

		float deltaTime = Time.deltaTime;
		_fallTimer += deltaTime;
		_fallVelocity += _fallGravity * deltaTime;

		// 下方向へ落下
		_player.transform.position += Vector3.down * _fallVelocity * deltaTime;

		if (_model != null)
		{
			// モデルの回転
			_model.Rotate(
				Vector3.forward,
				_rotationSpeed * deltaTime,
				Space.Self
			);

			// モデルの縮小
			float t = Mathf.Clamp01(_fallTimer / _fallDuration);
			_model.localScale = Vector3.Lerp(_initialModelScale, Vector3.zero, t);
		}

		// 演出終了
		if (_fallTimer >= _fallDuration)
		{
			_isPlaying = false;
			_onEffectFinished?.Invoke();
		}
	}
}
