using UnityEngine;
using System.Collections;

public class ChangeSpeed : MonoBehaviour
{
    [SerializeField, Min(0f),Tooltip("移動速度")]
    private float _moveSpeed = 1f;

    [SerializeField, Min(0f), Tooltip("効果時間（秒）")]
    private float _duration = 3f;

    public void OnEnterPlayer(Player player)
    {
        StartCoroutine(ChangeSpeedTemporarily(player));
    }

    private IEnumerator ChangeSpeedTemporarily(Player player)
    {
        // 変更前の速度を保存
        float originalSpeed = player.MoveSpeed;

        // 速度を変更
        player.SetMoveSpeed(_moveSpeed);

        // 指定秒数待つ
        yield return new WaitForSeconds(_duration);

        // プレイヤーが残っていれば元に戻す
        if (player != null)
        {
            player.SetMoveSpeed(originalSpeed);
        }
    }
}
