using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 消費した枠ごとに待ち時間を数え、その枠だけランダム補充する。
/// 作成者 : 酒井康佑
/// </summary>
public class HoldTileSpawner : MonoBehaviour
{
    [SerializeField] private TileGridController _tileGridController;
    [SerializeField] private TileGroupTableData _tileGroupTable;
    [SerializeField] private List<HoldTileElement> _holdTileElements;
    [SerializeField] private bool _spawnOnStart = true;

    [Header("個別補充")]
    [SerializeField, Min(0f)] private float _cooldownSeconds = 5f;
    [SerializeField] private ButtonClickEffect _completionEffect;
    [SerializeField, Tooltip("既存の待ち時間・エフェクト設定の引き継ぎ元")]
    private CooldownButton _refillCooldown;

    private sealed class SlotTimer
    {
        public HoldTileElement slot;
        public float remaining;
        public float duration;
    }

    private readonly List<SlotTimer> _timers = new();

    private void Start()
    {
        if (_spawnOnStart)
            RefillInternal(false);
    }

    // 一括補充の既存呼び出しにも対応する。
    public void Refill() => RefillInternal(true);

    private List<TileGroup> GetCandidates()
    {
        List<TileGroup> result = new();
        if (_tileGroupTable != null && _tileGroupTable.TileGroupTable != null)
        {
            foreach (TileGroup tile in _tileGroupTable.TileGroupTable)
                if (tile != null) result.Add(tile);
        }
        return result;
    }

    private void RefillInternal(bool playSound)
    {
        List<TileGroup> candidates = GetCandidates();
        if (candidates.Count == 0 || _holdTileElements == null)
        {
            Debug.LogWarning("タイル一覧または手持ち枠が未設定です。", this);
            return;
        }

        _timers.Clear();
        // 最初の一巡は重複を避け、候補数が少なくても全枠を埋める。
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int index = Random.Range(0, i + 1);
            (candidates[i], candidates[index]) = (candidates[index], candidates[i]);
        }

        int next = 0;
        foreach (HoldTileElement slot in _holdTileElements)
        {
            if (slot == null) continue;
            SetTile(slot, candidates[next++ % candidates.Count]);
            if (playSound) PlayCompletion(slot);
        }
    }

    private void SetTile(HoldTileElement slot, TileGroup tile)
    {
        // どの枠が使われたかを通知に保持する。
        slot.Initialize(_tileGridController, tile, () => OnTileConsumed(slot));
    }

    private void OnTileConsumed(HoldTileElement slot)
    {
        if (slot == null || slot.HasTile || _timers.Exists(timer => timer.slot == slot))
            return;

        float sharedDuration = _refillCooldown != null
            ? _refillCooldown.CooldownSeconds : _cooldownSeconds;
        float duration = Mathf.Max(0f,
            slot.CooldownSeconds >= 0f ? slot.CooldownSeconds : sharedDuration);

        _timers.Add(new SlotTimer { slot = slot, remaining = duration, duration = duration });
        slot.ShowCooldown(0f);
    }

    private void Update()
    {
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            SlotTimer timer = _timers[i];
            if (timer.slot == null || timer.slot.HasTile)
            {
                if (timer.slot != null) timer.slot.HideCooldown();
                _timers.RemoveAt(i);
                continue;
            }

            // 以前と同様、倍速に影響されない実時間で計測する。
            // 枠またはSpawnerが非表示の間は残り時間を維持する。
            if (!timer.slot.isActiveAndEnabled) continue;
            timer.remaining = Mathf.Max(0f, timer.remaining - Time.unscaledDeltaTime);
            timer.slot.ShowCooldown(timer.duration <= 0f
                ? 1f : 1f - timer.remaining / timer.duration);
            if (timer.remaining > 0f) continue;

            _timers.RemoveAt(i);
            List<TileGroup> candidates = GetCandidates();
            if (candidates.Count == 0)
            {
                timer.slot.HideCooldown();
                Debug.LogWarning("補充できるタイルが一覧にありません。", this);
                continue;
            }

            SetTile(timer.slot, candidates[Random.Range(0, candidates.Count)]);
            PlayCompletion(timer.slot);
        }
    }

    private void PlayCompletion(HoldTileElement slot)
    {
        ButtonClickEffect effect = _completionEffect != null
            ? _completionEffect
            : _refillCooldown != null ? _refillCooldown.CompletionEffect : null;
        if (effect != null)
            effect.PlayAt(slot.SlotRect);
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySEById(SoundId.TileRefill);
    }
}