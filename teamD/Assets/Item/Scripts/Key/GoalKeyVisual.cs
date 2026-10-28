using UnityEngine;

/// <summary>鍵条件に応じたゴールの発光・粒子表示。取得処理やUIには関与しない。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GoalCell))]
public sealed class GoalKeyVisual : MonoBehaviour
{
    [SerializeField] private PlayerInventory _inventory;
    [SerializeField] private GoalCell _goal;
    [SerializeField] private Renderer _goalFrame;
    [SerializeField] private GameObject _goalGlow;
    [SerializeField] private ParticleSystem _goalSparks;
    [SerializeField] private ParticleSystem[] _goalCompleteEffects;

    private MaterialPropertyBlock _properties;
    private bool _hasState;
    private bool _complete;

    private void OnEnable()
    {
        if (_inventory != null && _goal != null)
            Refresh(_inventory.KeyCount);
    }

    // PlayerInventoryの所持数通知をUIとは独立して受け取る。
    public void OnCountChanged(ItemType type, int count)
    {
        if (type == ItemType.Key && isActiveAndEnabled && _goal != null)
            Refresh(count);
    }

    private void Refresh(int count)
    {
        bool complete = _goal.HasRequiredKeyCount(count);
        // 必要数を超えて取得しても、達成時の一度きりの演出を再始動しない。
        if (_hasState && _complete == complete)
            return;
        _hasState = true;
        _complete = complete;

        if (_goalFrame != null)
        {
            _properties ??= new MaterialPropertyBlock();
            _goalFrame.GetPropertyBlock(_properties);
            _properties.SetFloat("_Intensity", complete ? 4.5f : 2.4f);
            _properties.SetColor("_Tint", new Color(1f, 0.71f, 0.16f, 1f));
            _goalFrame.SetPropertyBlock(_properties);
        }
        if (_goalGlow != null)
            _goalGlow.SetActive(true);
        if (_goalSparks != null && !_goalSparks.isPlaying)
            _goalSparks.Play(true);
        if (_goalCompleteEffects == null)
            return;
        foreach (ParticleSystem effect in _goalCompleteEffects)
        {
            if (effect == null)
                continue;
            if (complete)
                effect.Play(true);
            else
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
