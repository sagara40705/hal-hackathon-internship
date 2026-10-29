using System;
using UnityEngine;

/// <summary>UIに依存しない、高速化の速度・許可状態・演出設定。</summary>
[DisallowMultipleComponent]
public sealed class HighSpeedController : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _speedMultiplier = 2.5f;
    [Header("高速化エフェクト")]
    [SerializeField, Range(0f, 1f)] private float _effectStrength = 1f;
    [SerializeField, Range(0f, 20f)] private float _chromaticPixels = 12f;
    [SerializeField, Range(0f, 32f)] private float _horizontalBlurPixels = 26f;
    [SerializeField, Range(0f, 0.3f)] private float _lineStrength = 0.3f;
    private bool _allowed = true;
    private bool _applied;
    private bool _speedSoundPlayed;
    private float _originalTimeScale;
    private float _appliedTimeScale;

    public static HighSpeedController Active { get; private set; }
    public bool CanSpeedUp => _allowed && isActiveAndEnabled;
    public bool IsRequested { get; private set; }
    public bool IsSpeedModified => CanSpeedUp && IsRequested && _applied && Time.timeScale > 0f
        && Mathf.Approximately(Time.timeScale, _appliedTimeScale);
    public Vector4 EffectParameters => new Vector4(_effectStrength, _chromaticPixels, _horizontalBlurPixels, _lineStrength);
    public event Action StateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActive() => Active = null;

    public bool BeginSpeedUp()
    {
        if (!CanSpeedUp) return false;
        if (Active != null && Active != this) Active.EndSpeedUp();
        Active = this;
        IsRequested = true;
        ApplySpeed();
        StateChanged?.Invoke();
        return true;
    }

    private void Update()
    {
        if (IsRequested) ApplySpeed();
    }

    private void ApplySpeed()
    {
        // 停止中は上書きせず、ヒットストップ解除後に再適用する。
        if (Time.timeScale <= 0f) return;
        if (!_applied)
        {
            _originalTimeScale = Time.timeScale;
            _appliedTimeScale = _originalTimeScale * Mathf.Max(1f, _speedMultiplier);
            _applied = true;
        }
        // ヒットストップからの復帰では再生し直さず、開始時に一度だけ鳴らす。
        if (!_speedSoundPlayed && _appliedTimeScale > _originalTimeScale)
        {
            _speedSoundPlayed = true;
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySpeedSE();
        }
        Time.timeScale = _appliedTimeScale;
    }

    public void EndSpeedUp()
    {
        // このControllerが開始した倍速音だけを解除時に停止する。
        if (Active == this && _speedSoundPlayed && SoundManager.Instance != null)
            SoundManager.Instance.StopSpeedSE();

        if (_applied && Mathf.Approximately(Time.timeScale, _appliedTimeScale))
            Time.timeScale = _originalTimeScale;
        _applied = false;
        _speedSoundPlayed = false;
        IsRequested = false;
        if (Active == this) Active = null;
        StateChanged?.Invoke();
    }

    public void SetSpeedUpAllowed(bool allowed)
    {
        _allowed = allowed;
        if (!allowed) EndSpeedUp();
        else StateChanged?.Invoke();
    }

    private void OnDisable() => EndSpeedUp();
    private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) EndSpeedUp(); }
    private void OnApplicationPause(bool paused) { if (paused) EndSpeedUp(); }
}
