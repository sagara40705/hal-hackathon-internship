using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 指定キーを押している間、押し始めた時点の移動速度を一定倍率にする。
/// 作成者：sagara
/// </summary>
[RequireComponent(typeof(Player))]
[DisallowMultipleComponent]
public class PlayerSpeedMultiplier : MonoBehaviour
{
    [SerializeField, Tooltip("速度を変更するキー")]
    private Key _activationKey = Key.LeftShift;

    [SerializeField, Min(0f), Tooltip("キーを押している間の速度倍率")]
    private float _speedMultiplier = 2f;

    private Player _player;
    private float _originalSpeed;

    // 高速化が有効かどうか
    public bool IsSpeedModified { get; private set; }

    private void Start()
    {
        _player = GetComponent<Player>();
        _originalSpeed = _player.MoveSpeed;
        IsSpeedModified = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool isHeld = keyboard != null && keyboard[_activationKey].isPressed;

        // キーが押されている場合、速度を変更する
        if (isHeld && !IsSpeedModified)
        {
            IsSpeedModified = true;
            _player?.SetMoveSpeed(_originalSpeed * Mathf.Max(0f, _speedMultiplier));
        }
        // キーが離された場合、速度を元に戻す
        else if (!isHeld && IsSpeedModified)
        {
            RestoreSpeed();
        }
    }

    private void OnDisable()
    {
        RestoreSpeed();
    }

    // 速度を元に戻す
    private void RestoreSpeed()
    {
        IsSpeedModified = false;
        _player?.SetMoveSpeed(_originalSpeed);
    }
}
