using UnityEngine;

// Playerの移動前に速度を決める。シーンのTileGridに1つ配置し、参照を設定する。
// 更新：10/20　Sagara
[DefaultExecutionOrder(-100)]
public class SlowTileSpeedController : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private TileGrid _tileGrid;

    private TileCell _currentCell;
    private ChangeSpeed _currentSlow;
    private float? _originalSpeed;

    private void Update()
    {
        // プレイヤーが無効化されている場合は速度を元に戻す
        if (_player == null || !_player.isActiveAndEnabled || _tileGrid == null)
        {
            RestoreSpeed();
            return;
        }

        // プレイヤーの現在位置にあるタイルセルを取得する
        TileCell cell = _tileGrid.GetCell(_tileGrid.WorldToGrid(_player.transform.position));
        if (cell != _currentCell)
        {
            _currentCell = cell;
            _currentSlow = cell != null ? cell.GetComponent<ChangeSpeed>() : null;
        }

        // 減速セルが存在しない場合は速度を元に戻す
        if (cell == null || !cell.isActiveAndEnabled ||
            _currentSlow == null || !_currentSlow.isActiveAndEnabled)
        {
            RestoreSpeed();
            return;
        }

        // 隣接する減速セル間では、最初に保存した通常速度を維持する。
        if (!_originalSpeed.HasValue)
            _originalSpeed = _player.MoveSpeed;

        // 減速セルの速度を適用する
        float speed = _currentSlow.MoveSpeed;
        if (_player.MoveSpeed != speed)
            _player.SetMoveSpeed(speed);
    }

    private void OnDisable()
    {
        RestoreSpeed();
        _currentCell = null;
        _currentSlow = null;
    }

    // 元の速度に戻す
    private void RestoreSpeed()
    {
        if (!_originalSpeed.HasValue)
            return;

        if (_player != null)
            _player.SetMoveSpeed(_originalSpeed.Value);
        _originalSpeed = null;
    }
}
