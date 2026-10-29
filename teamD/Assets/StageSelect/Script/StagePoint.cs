using UnityEngine;

/// <summary>
/// ワールドマップ上のステージ位置
/// </summary>
[ExecuteAlways]
public class StagePoint : MonoBehaviour
{
    [SerializeField, Tooltip("ステージ情報")]
    private StageData _stageData;

    [SerializeField, HideInInspector]
    private int _gridPosition;

    public StageData StageData => _stageData;
    public int GridPosition => _gridPosition;

    private StageSelectManager _manager;


    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
        Refresh();
    }

    private void OnTransformParentChanged()
    {
        Refresh();
    }

    private void Update()
    {
        if (Application.IsPlaying(gameObject))
            return;

        if (transform.hasChanged)
        {
            SnapToGrid();
            transform.hasChanged = false;
        }

        UpdateObjectName();
    }

    // ステージ情報とグリッド設定を更新する
    public void Refresh()
    {
        _manager = GetComponentInParent<StageSelectManager>();

        UpdateObjectName();
        SnapToGrid();
    }

    // ステージ名をオブジェクト名に反映する
    private void UpdateObjectName()
    {
        if (_stageData == null)
            return;

        string name = "StagePoint : " + _stageData.StageName;
        if (gameObject.name != name)
            gameObject.name = name;
    }

    // ワールド座標からグリッド座標を計算し、再配置する
    private void SnapToGrid()
    {
        if (_manager == null)
            return;

        Vector3 localPosition = _manager.transform.InverseTransformPoint(transform.position);

        _gridPosition = Mathf.RoundToInt(
            localPosition.x / _manager.GridSize
        );

        localPosition.x = _gridPosition * _manager.GridSize;
        transform.position = _manager.transform.TransformPoint(localPosition);
    }
}
