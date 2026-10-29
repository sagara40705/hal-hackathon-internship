using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手持ちタイルをすべて使い切ると、
/// クールタイム終了後にランダムで補充する。
/// 作成者 : 酒井康佑
/// </summary>
public class HoldTileSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("タイルグリッドコントローラー")]
    private TileGridController _tileGridController;

    [Header("生成に使うデータ")]
    [SerializeField, Tooltip("生成するタイルグループのテーブル")]
    private TileGroupTableData _tileGroupTable;

    [SerializeField, Tooltip("手持ちタイルのUI要素")]
    private List<HoldTileElement> _holdTileElements;

    [Header("生成設定")]
    [SerializeField, Tooltip("ゲーム開始時にも手持ちタイルを生成する")]
    private bool _spawnOnStart = true;

    [SerializeField, Tooltip("自動補充までのクールタイム表示")]
    private CooldownButton _refillCooldown;

    private void Start()
    {
        // 最初の手持ちは待ち時間なしで補充する。
        if (_spawnOnStart)
            Refill();
    }

    /// <summary>
    /// SOの一覧からランダムに選び、各枠に補充する。
    /// </summary>
    public void Refill()
    {
        if (_tileGroupTable == null ||
            _holdTileElements == null ||
            _holdTileElements.Count == 0)
        {
            Debug.LogWarning(
                "タイルテーブルまたは手持ち枠が設定されていません。",
                this
            );
            return;
        }

        List<TileGroup> candidates = new();

        foreach (TileGroup tileGroup in _tileGroupTable.TileGroupTable)
        {
            if (tileGroup != null)
                candidates.Add(tileGroup);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("SOに有効なPrefabがありません。", this);
            return;
        }

        // 候補をシャッフルする。
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (candidates[i], candidates[randomIndex]) =
                (candidates[randomIndex], candidates[i]);
        }

        int candidateIndex = 0;

        foreach (HoldTileElement element in _holdTileElements)
        {
            // 未設定の枠だけ飛ばす。
            if (element == null)
                continue;

            // 候補が足りない場合は、余った枠を空にする。
            TileGroup tileGroup = candidateIndex < candidates.Count
                ? candidates[candidateIndex++]
                : null;

            // 補充するたびに、消費時の通知先も設定する。
            element.Initialize(
                _tileGridController,
                tileGroup,
                OnTileConsumed
            );
        }

        // 初回生成を含め、補充できたときに一度だけ再生する。
        if (_holdTileElements.Exists(element => element != null && element.HasTile))
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySEById(SoundId.TileRefill);
        }
    }

    /// <summary>
    /// 全枠が空になったら、補充までのクールタイムを開始する。
    /// </summary>
    private void OnTileConsumed()
    {
        if (_holdTileElements == null || _holdTileElements.Count == 0)
            return;

        foreach (HoldTileElement element in _holdTileElements)
        {
            // タイルが1つでも残っていれば、まだ開始しない。
            if (element != null && element.HasTile)
                return;
        }

        if (_refillCooldown == null)
        {
            Debug.LogWarning(
                "Refill Cooldownが設定されていません。",
                this
            );
            return;
        }

        // ここでは補充せず、時間が満了したときにRefillを呼んでもらう。
        _refillCooldown.StartCooldown(Refill);
    }
}
