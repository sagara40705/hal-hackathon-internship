using UnityEngine;
/// <summary>
///オブジェクトの半径を計算して転がすスクリプト
/// 作成者：牛田大翔
/// </summary>
public class StageSelectPlayerRolling : MonoBehaviour
{
    [Header("サイコロの半径")]
    public float rollRadius = 0.5f;

    [Header("床の高さ（床の表面のY座標）")]
    public float groundY = 0f;

    [Header("めり込み補正")]
    public float correctionOffset = 0.01f;

    private Vector3 previousPosition;
    private BoxCollider boxCollider;

    private void Start()
    {
        previousPosition = transform.position;
        boxCollider = GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            Debug.LogError("Box Colliderが見つかりません。");
        }
    }

    private void LateUpdate()
    {
        // ほかのスクリプトによる移動量を取得
        Vector3 movement = transform.position - previousPosition;
        movement.y = 0f;

        float distance = movement.magnitude;

        if (distance > 0.0001f && rollRadius > 0f)
        {
            Vector3 direction = movement.normalized;

            // 移動方向に対して垂直な回転軸
            Vector3 rotationAxis =
                Vector3.Cross(Vector3.up, direction).normalized;

            float angle = distance / rollRadius * Mathf.Rad2Deg;

            transform.Rotate(rotationAxis, angle, Space.World);
        }

        // 回転後のBox Colliderの最下部を計算
        if (boxCollider != null)
        {
            Vector3 center = boxCollider.center;
            Vector3 halfSize = boxCollider.size * 0.5f;

            float lowestY = float.MaxValue;

            // Box Colliderの8つの頂点を調べる
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localCorner = center + new Vector3(
                            halfSize.x * x,
                            halfSize.y * y,
                            halfSize.z * z
                        );

                        Vector3 worldCorner =
                            boxCollider.transform.TransformPoint(localCorner);

                        if (worldCorner.y < lowestY)
                        {
                            lowestY = worldCorner.y;
                        }
                    }
                }
            }

            // 最下部が床より低ければ上方向へ補正
            if (lowestY < groundY + correctionOffset)
            {
                Vector3 position = transform.position;
                position.y += groundY + correctionOffset - lowestY;
                transform.position = position;
            }
        }

        // 補正後の位置を保存
        previousPosition = transform.position;
    }
}
