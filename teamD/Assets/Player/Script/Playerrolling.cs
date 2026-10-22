using UnityEngine;
using System.Collections;
public class Playerrolling : MonoBehaviour
{
    [Header("転がる設定")]
    [SerializeField] private float rollDuration = 0.3f;

    [SerializeField] private float tileSize = 1f;

    private bool isRolling = false;

    public bool IsRolling => isRolling;

    // 指定方向に1マス転がる
    public void Roll(Vector3 direction)
    {
        if (isRolling)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        StartCoroutine(RollCoroutine(direction.normalized));
    }

    private IEnumerator RollCoroutine(Vector3 direction)
    {
        isRolling = true;

        // 転がる方向に対する回転軸
        Vector3 axis = Vector3.Cross(
            Vector3.up,
            direction
        );

        // 接地している辺を支点にする
        Vector3 pivot = transform.position
            + direction * (tileSize / 2f)
            - Vector3.up * (tileSize / 2f);

        float rotatedAngle = 0f;

        while (rotatedAngle < 90f)
        {
            float angle = Mathf.Min(
                90f - rotatedAngle,
                90f / Mathf.Max(rollDuration, 0.01f)
                    * Time.deltaTime
            );

            transform.RotateAround(
                pivot,
                axis,
                angle
            );

            rotatedAngle += angle;

            yield return null;
        }

        isRolling = false;
    }
}

