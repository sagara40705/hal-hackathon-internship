using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 敵の攻撃範囲を表示する
/// 作成者：石田怜
/// </summary>
public class EnemyAttackIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("タイルグリッド")]
    private TileGrid _tileGrid;

    [SerializeField, Tooltip("攻撃予告のPrefab")]
    private GameObject _indicatorPrefab;

    [SerializeField, Tooltip("表示位置の高さ")]
    private float _heightOffset = 0.05f;

    private List<GameObject> _indicators;       // 攻撃範囲の表示オブジェクトのリスト

    // 攻撃範囲を表示
    public void ShowIndicators(List<Vector2Int> targetPositions)
    {
        ClearIndicators();

        if (_tileGrid == null || _indicatorPrefab == null || targetPositions == null)
            return;

        // 攻撃範囲の表示オブジェクトを生成
        foreach (Vector2Int gridPosition in targetPositions)
        {
            if (!_tileGrid.IsInGrid(gridPosition))
                continue;

            // グリッド座標をワールド座標に変換して表示位置を計算
            Vector3 position = _tileGrid.GridToWorld(gridPosition);
            position += _tileGrid.transform.up * _heightOffset;
            position.y += _tileGrid.TileHeight * 0.5f;

            GameObject indicator = Instantiate(_indicatorPrefab, position, _tileGrid.transform.rotation, transform);
            _indicators.Add(indicator);
        }
    }

    public void ShowIndicators(List<TileGroup> targetTiles)
    {
        ClearIndicators();

        if (_tileGrid == null || _indicatorPrefab == null || targetTiles == null)
            return;

        // 攻撃範囲の表示オブジェクトを生成
        foreach (TileGroup tileGroup in targetTiles)
        {
            if (tileGroup == null)
                continue;

            GameObject indicator = Instantiate(_indicatorPrefab, tileGroup.transform);
            _indicators.Add(indicator);

            indicator.transform.localPosition = tileGroup.PreviewOffset + Vector3.up * 0.5f;
        }
    }

    // 攻撃範囲の表示を解除
    public void ClearIndicators()
    {
        // 表示オブジェクトを削除
        foreach (GameObject indicator in _indicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        _indicators.Clear();
    }

    private void Awake()
    {
        _indicators = new List<GameObject>();
    }
}
