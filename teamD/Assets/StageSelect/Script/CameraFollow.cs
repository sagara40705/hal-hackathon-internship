using UnityEngine;

/// <summary>
/// 特定のタグ(今回はプレイヤー)を探して動くスクリプト
/// 作成者：牛田大翔
/// </summary>
public class CameraFollow : MonoBehaviour
{
    private Transform player;

    // カメラとPlayerの初期位置の差
    private Vector3 offset;

    private void Start()
    {
        // Playerタグのオブジェクトを検索
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning(
                "Playerタグのオブジェクトが見つかりません。"
            );
            return;
        }

        player = playerObject.transform;

        // 現在のカメラ位置を基準にする
        offset = transform.position - player.position;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        // X軸とZ軸はPlayerを追従し、Y軸は固定
        Vector3 targetPosition = player.position + offset;
        targetPosition.y = transform.position.y;

        transform.position = targetPosition;
    }
}
