using UnityEngine;

// 減速タイルの速度設定。判定と速度の切り替えはSlowTileSpeedControllerが担当する。
public class ChangeSpeed : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("このタイル上の移動速度")]
    private float _moveSpeed = 0.5f;

    public float MoveSpeed => _moveSpeed;
}
