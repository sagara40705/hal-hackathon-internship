using UnityEngine;
/// <summary>
///オブジェクトの半径を計算して転がすスクリプト
/// 作成者：牛田大翔
/// </summary>
public class StageSelectPlayerRolling : MonoBehaviour
{
    [Header("サイコロの半径（半分の大きさ）")]
    public float rollRadius = 0.5f;

    private Vector3 previousPosition;

    void Start()
    {
        previousPosition = transform.position;
    }

    void LateUpdate()
    {
        // 前回からの移動量を取得
        Vector3 movement = transform.position - previousPosition;

        // 上下方向の移動を除外
        movement.y = 0f;

        float distance = movement.magnitude;

        if (distance > 0.0001f && rollRadius > 0f)
        {
            // 移動方向に対して垂直な回転軸を求める
            Vector3 direction = movement.normalized;
            Vector3 rotationAxis =
                Vector3.Cross(Vector3.up, direction).normalized;

            // 移動距離に応じて回転する
            float angle = distance / rollRadius * Mathf.Rad2Deg;

            transform.Rotate(
                rotationAxis,
                angle,
                Space.World
            );
        }

        // 現在位置を保存
        previousPosition = transform.position;
    }
}
