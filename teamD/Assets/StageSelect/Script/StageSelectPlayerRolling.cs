using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
///オブジェクトの半径を計算して転がすスクリプト
/// 作成者：牛田大翔
/// </summary>
public class StageSelectPlayerRolling : MonoBehaviour
{
    [SerializeField, Tooltip("グリッドの1マスの大きさ")]
    private float _gridSize = 1.0f;

    [SerializeField, Tooltip("1マス移動する時間")]
    private float _rollDuration = 0.25f;

    [SerializeField, Tooltip("回転の補間カーブ")]
    private AnimationCurve _rollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField, Tooltip("初期グリッド座標")]
    private int _startGridPosition = 0;

    [SerializeField, Tooltip("目的地への移動完了時のイベント")]
    private UnityEvent _onMoveComplete = new UnityEvent();

    public bool IsRolling => _isRolling;
    public int GridPosition => _gridPosition;
    public int TargetGridPosition => _targetGridPosition;

    private Vector3 _originPosition;
    private int _gridPosition;
    private int _targetGridPosition;

    private Coroutine _moveCoroutine;
    private bool _isRolling;


    private void Awake()
    {
        _originPosition = transform.position - Vector3.right * (_startGridPosition * _gridSize);

        _gridPosition = _startGridPosition;
        _targetGridPosition = _gridPosition;
    }

    // 指定したグリッド座標まで移動する
    public void MoveTo(int targetGridPosition)
    {
        _targetGridPosition = targetGridPosition;

        // 移動処理が実行中なら目標だけ更新
        if (_isRolling)
            return;

        if (_gridPosition == _targetGridPosition)
            return;

        _moveCoroutine = StartCoroutine(MoveCoroutine());
    }

    // 目標グリッド座標まで移動する
    private IEnumerator MoveCoroutine()
    {
        _isRolling = true;

        while (_gridPosition != _targetGridPosition)
        {
            int direction = _targetGridPosition > _gridPosition ? 1 : -1;
            yield return RollCoroutine(direction);

            _gridPosition += direction;

            // 移動終了位置をグリッドに補正
            transform.position = _originPosition
                + Vector3.right * (_gridPosition * _gridSize);
        }

        _isRolling = false;
        _moveCoroutine = null;

        _onMoveComplete.Invoke();
    }

    // 1マス分転がる
    private IEnumerator RollCoroutine(int direction)
    {
        Vector3 moveDirection = Vector3.right * direction;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        // 移動方向側の接地辺を支点にする
        Vector3 pivot = startPosition
            + moveDirection * (_gridSize * 0.5f)
            + Vector3.down * (_gridSize * 0.5f);

        Vector3 rotationAxis = Vector3.Cross(
            Vector3.up,
            moveDirection
        );

        float timer = 0.0f;
        while (timer < _rollDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(
                timer / Mathf.Max(_rollDuration, 0.0001f)
            );

            float angle = 90.0f * Mathf.Clamp01(_rollCurve.Evaluate(t));
            Quaternion rotation = Quaternion.AngleAxis(angle, rotationAxis);

            // 接地辺を中心に位置と回転を更新
            transform.position = pivot
                + rotation * (startPosition - pivot);

            transform.rotation = rotation * startRotation;

            yield return null;
        }

        // 回転終了時の姿勢を確定
        transform.rotation = Quaternion.AngleAxis(
            90.0f,
            rotationAxis
        ) * startRotation;
    }
}
