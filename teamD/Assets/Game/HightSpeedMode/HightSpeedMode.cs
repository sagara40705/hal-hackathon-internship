using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 指定キーを押している間、ゲーム全体の時間を一定倍率で進める。
/// 作成者：sagara
/// </summary>
[DisallowMultipleComponent]
public class HightSpeedMode : MonoBehaviour
{
    [SerializeField, Tooltip("高速化するキー")]
    private Key _activationKey = Key.LeftShift;

    [SerializeField, Min(1f), Tooltip("キーを押している間の時間倍率")]
    private float _speedMultiplier = 2f;

    private float _originalTimeScale;
    private float _appliedTimeScale;
    private bool _isHolding;

    // ヒットストップなどで停止中の場合はfalse。
    public bool IsSpeedModified => _isHolding && Time.timeScale > 0f
        && Mathf.Approximately(Time.timeScale, _appliedTimeScale);

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool isHeld = Application.isFocused && keyboard != null
            && _activationKey != Key.None && keyboard[_activationKey].isPressed;

        if (!isHeld)
        {
            RestoreTimeScale();
            return;
        }

        // ポーズやヒットストップを解除しない。
        if (Time.timeScale <= 0f)
            return;

        if (!_isHolding)
        {
            _originalTimeScale = Time.timeScale;
            _appliedTimeScale = _originalTimeScale * Mathf.Max(1f, _speedMultiplier);
            _isHolding = true;
        }

        // ヒットストップから復帰した場合も、押下中は再適用する。
        Time.timeScale = _appliedTimeScale;
    }

    private void OnDisable()
    {
        RestoreTimeScale();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!_isHolding)
            return;

        // 別の処理による停止や時間変更は上書きしない。
        if (Mathf.Approximately(Time.timeScale, _appliedTimeScale))
            Time.timeScale = _originalTimeScale;

        _isHolding = false;
    }
}
