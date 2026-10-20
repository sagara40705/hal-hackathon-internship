using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敵の攻撃範囲を表示する
/// </summary>
public class EnemyAttackIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("タイルグリッド")]
    private TileGrid _tileGrid;

    [SerializeField, Tooltip("攻撃予告のPrefab")]
    private GameObject _indicatorPrefab;

    [SerializeField, Tooltip("表示位置の高さ")]
    private float _heightOffset = 0.05f;

    private readonly List<GameObject> _indicators = new();


    // 攻撃範囲を表示
    public void ShowIndicators(List<Vector2Int> targetPositions)
    {
        ClearIndicators();

        if (_tileGrid == null || _indicatorPrefab == null || targetPositions == null)
            return;

        foreach (Vector2Int gridPosition in targetPositions)
        {
            if (!_tileGrid.IsInGrid(gridPosition))
                continue;

            Vector3 position = _tileGrid.GridToWorld(gridPosition);
            position += _tileGrid.transform.up * _heightOffset;

            GameObject indicator = Instantiate(_indicatorPrefab, position, _tileGrid.transform.rotation, transform);
            _indicators.Add(indicator);
        }
    }

    // 攻撃範囲の表示を解除
    public void ClearIndicators()
    {
        foreach (GameObject indicator in _indicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }

        _indicators.Clear();
    }

    private void OnDisable()
    {
        ClearIndicators();
    }
}
