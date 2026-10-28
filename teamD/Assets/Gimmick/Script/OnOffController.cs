using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>TileGridごとのON/OFF状態。シーンを読み直すと初期状態に戻る。</summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class OnOffController : MonoBehaviour
{
    [SerializeField, Tooltip("対象のグリッド。未指定なら親から取得")]
    private TileGrid _tileGrid;

    [SerializeField, Tooltip("OFFは青床、ONはオレンジ床が有効")]
    private bool _startOn;

    [SerializeField] private UnityEvent<bool> _onStateChanged = new();

    private readonly List<OnOffFloor> _floors = new();
    private readonly List<OnOffSwitch> _switches = new();
    private bool _initialized;
    private bool _isOn;

    public TileGrid Grid => _tileGrid != null ? _tileGrid : GetComponentInParent<TileGrid>();
    public bool IsOn => _initialized ? _isOn : _startOn;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (_initialized)
            return;

        _tileGrid = Grid;
        _isOn = _startOn;
        _initialized = true;
    }

    // 明示参照がなくても、同じTileGrid内のコントローラーを共有する。
    // 床・スイッチのStartから呼び、TileGrid.Awakeの初期登録を先に完了させる。
    public static OnOffController Resolve(Component source)
    {
        TileGrid grid = source.GetComponentInParent<TileGrid>();
        if (grid == null)
        {
            Debug.LogWarning("ON/OFF PrefabはTileGridの子に配置してください。", source);
            return null;
        }

        OnOffController controller = null;
        foreach (OnOffController candidate in grid.GetComponentsInChildren<OnOffController>())
        {
            if (!candidate.isActiveAndEnabled || candidate.Grid != grid)
                continue;

            if (controller != null)
            {
                Debug.LogError("同じTileGridに有効なOnOffControllerを複数配置できません。", source);
                return null;
            }
            controller = candidate;
        }

        if (controller == null && grid.TryGetComponent(out OnOffController disabledController))
        {
            Debug.LogWarning("TileGrid上のOnOffControllerを有効にしてください。", disabledController);
            return null;
        }
        if (controller == null)
            controller = grid.gameObject.AddComponent<OnOffController>();

        controller.Initialize();
        return controller;
    }

    public void Toggle() => SetState(!IsOn);

    public void SetState(bool isOn)
    {
        Initialize();
        if (!isActiveAndEnabled || _isOn == isOn)
            return;

        _isOn = isOn;
        // 登録・表示をすべて更新してから外部へ通知する。
        foreach (OnOffFloor floor in _floors.ToArray())
        {
            if (floor != null)
                floor.ApplyState(_isOn);
        }
        foreach (OnOffSwitch tileSwitch in _switches.ToArray())
        {
            if (tileSwitch != null)
                tileSwitch.RefreshAppearance(_isOn);
        }
        _onStateChanged?.Invoke(_isOn);
    }

    internal void Register(OnOffFloor floor)
    {
        Initialize();
        if (!_floors.Contains(floor))
            _floors.Add(floor);
        floor.ApplyState(_isOn);
    }

    internal void Register(OnOffSwitch tileSwitch)
    {
        Initialize();
        if (!_switches.Contains(tileSwitch))
            _switches.Add(tileSwitch);
        tileSwitch.RefreshAppearance(_isOn);
    }

    internal void Unregister(OnOffFloor floor) => _floors.Remove(floor);
    internal void Unregister(OnOffSwitch tileSwitch) => _switches.Remove(tileSwitch);
}
