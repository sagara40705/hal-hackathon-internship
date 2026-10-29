using UnityEngine;

/// <summary>
/// 複数のオブジェクトを同期して指定方向に揺らす
/// </summary>
public class FloatingObject : MonoBehaviour
{
    [SerializeField, Tooltip("揺らすオブジェクト")]
    private GameObject[] _gameObjects;

    [SerializeField, Tooltip("揺れる方向（ローカル座標）")]
    private Vector3 _moveAxis = Vector3.up;

    [SerializeField, Tooltip("揺れる幅")]
    private float _amplitude = 0.05f;

    [SerializeField, Tooltip("揺れる速度")]
    private float _speed = 2.0f;

    [SerializeField, Tooltip("揺れの開始位相")]
    private float _phase = 0.0f;

    private Vector3[] _initialLocalPositions;


    private void OnEnable()
    {
        // 各オブジェクトの初期位置を保存
        _initialLocalPositions = new Vector3[_gameObjects.Length];

        for (int i = 0; i < _gameObjects.Length; i++)
        {
            if (_gameObjects[i] == null)
                continue;

            _initialLocalPositions[i] = _gameObjects[i].transform.localPosition;
        }
    }

    private void LateUpdate()
    {
        // 指定方向への揺れ量を計算
        float offset = Mathf.Sin(Time.time * _speed + _phase) * _amplitude;
        Vector3 floatOffset = _moveAxis.normalized * offset;

        // すべてのオブジェクトを同期して移動
        for (int i = 0; i < _gameObjects.Length; i++)
        {
            if (_gameObjects[i] == null)
                continue;

            _gameObjects[i].transform.localPosition =
                _initialLocalPositions[i] + floatOffset;
        }
    }

    private void OnDisable()
    {
        if (_initialLocalPositions == null)
            return;

        // 各オブジェクトを初期位置に戻す
        for (int i = 0; i < _gameObjects.Length && i < _initialLocalPositions.Length; i++)
        {
            if (_gameObjects[i] == null)
                continue;

            _gameObjects[i].transform.localPosition = _initialLocalPositions[i];
        }
    }
}
