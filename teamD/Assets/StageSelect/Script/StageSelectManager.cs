using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 設定した位置にあるオブジェクトの場所でUIのボタンを押すとそのステージにいくスクリプト
/// 作成者：牛田大翔
/// </summary>
[ExecuteAlways]
public class StageSelectManager : MonoBehaviour
{
    [SerializeField, Tooltip("プレイヤー")]
    private StageSelectPlayerRolling _player;

    [SerializeField, Tooltip("選択可能な距離")]
    private float _selectableDistance = 2.0f;

    [SerializeField, Tooltip("グリッドサイズ")]
    private float _gridSize = 1.0f;

    [SerializeField, Tooltip("ステージ位置")]
    private StagePoint[] _stagePoints;

    [SerializeField, Tooltip("次のステージへ移動するボタン")]
    private GameObject _nextStageButton;

    [SerializeField, Tooltip("前のステージへ移動するボタン")]
    private GameObject _previousStageButton;

    [SerializeField, Tooltip("現在のステージが変更されたときのイベント")]
    private UnityEvent<StageData> _onStageChanged;

    public StagePoint CurrentStage => _currentStage;
    public float GridSize => _gridSize;

    private StagePoint _currentStage;
    private StagePoint _targetStage;


    private void Awake()
    {
        RefreshStagePoints();
    }

    private void Start()
    {
        if (!Application.IsPlaying(gameObject))
            return;

        UpdateCurrentStage();
        UpdateStageButtons();

        _targetStage = _currentStage;
    }

    private void Update()
    {
        if (!Application.IsPlaying(gameObject))
            return;

        UpdateCurrentStage();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshStagePoints();
    }

    // 子オブジェクトからステージ位置を取得する
    [ContextMenu("Refresh Stage Points")]
    public void RefreshStagePoints()
    {
        _stagePoints = GetComponentsInChildren<StagePoint>(true);

        foreach (StagePoint stage in _stagePoints)
        {
            stage.Refresh();
        }
    }

    // 次のステージへ移動する
    public void MoveNextStage()
    {
        MoveStage(1);
    }

    // 前のステージへ移動する
    public void MovePreviousStage()
    {
        MoveStage(-1);
    }

    // 指定方向のステージへ移動する
    private void MoveStage(int direction)
    {
        if (!Application.IsPlaying(gameObject))
            return;

        if (_player == null || _stagePoints == null)
            return;

        // 最後に指定した移動先を基準にする
        StagePoint baseStage = _targetStage != null
            ? _targetStage
            : _currentStage;

        if (baseStage == null)
            return;

        StagePoint nextStage = FindNextStage(
            baseStage.GridPosition,
            direction
        );

        if (nextStage == null)
            return;

        _targetStage = nextStage;
        _player.MoveTo(nextStage.GridPosition);

        UpdateStageButtons();
    }

    // 指定方向にある最も近いステージを取得する
    private StagePoint FindNextStage(int gridPosition, int direction)
    {
        StagePoint nearestStage = null;
        int nearestDistance = int.MaxValue;

        foreach (StagePoint stage in _stagePoints)
        {
            if (stage == null || !stage.gameObject.activeInHierarchy)
                continue;

            int difference = stage.GridPosition - gridPosition;

            // 指定方向にあるステージだけを対象にする
            if (difference * direction <= 0)
                continue;

            int distance = Mathf.Abs(difference);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestStage = stage;
            }
        }

        return nearestStage;
    }

    // 箱に最も近いステージを取得する
    private StagePoint FindNearestStage()
    {
        if (_player == null || _stagePoints == null)
            return null;

        StagePoint nearestStage = null;
        float nearestDistance = float.MaxValue;

        float playerX = _player.transform.position.x;

        foreach (StagePoint stage in _stagePoints)
        {
            if (stage == null || !stage.gameObject.activeInHierarchy)
                continue;

            float distance = Mathf.Abs(playerX - stage.transform.position.x);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestStage = stage;
            }
        }

        // 選択可能な距離を超えている場合はnullを返す
        if (nearestDistance > _selectableDistance)
        {
            nearestStage = null;
        }

        return nearestStage;
    }

    // 現在のステージを更新する
    private void UpdateCurrentStage()
    {
        StagePoint nearestStage = FindNearestStage();

        // ステージに変更がなければ更新しない
        if (_currentStage == nearestStage)
            return;

        _currentStage = nearestStage;

        _onStageChanged?.Invoke(
            _currentStage != null ? _currentStage.StageData : null
        );
    }

    // ステージ位置に応じてボタンの表示を更新する
    private void UpdateStageButtons()
    {
        StagePoint baseStage = _targetStage != null
            ? _targetStage
            : _currentStage;

        bool canMoveNext = false;
        bool canMovePrevious = false;

        if (baseStage != null && _stagePoints != null)
        {
            canMoveNext = FindNextStage(baseStage.GridPosition, 1) != null;
            canMovePrevious = FindNextStage(baseStage.GridPosition, -1) != null;
        }

        if (_nextStageButton != null)
            _nextStageButton.SetActive(canMoveNext);

        if (_previousStageButton != null)
            _previousStageButton.SetActive(canMovePrevious);
    }

    private void OnValidate()
    {
        _gridSize = Mathf.Max(0.0001f, _gridSize);
        _selectableDistance = Mathf.Max(0.0f, _selectableDistance);
    }
}
