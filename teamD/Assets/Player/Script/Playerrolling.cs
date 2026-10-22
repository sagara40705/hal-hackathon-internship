using UnityEngine;
using System.Collections;
/// <summary>
/// プレイヤーが転がるスクリプト
/// 作成者：牛田大翔
/// </summary>
public class Playerrolling : MonoBehaviour
{
    [Header("転がるオブジェクト")]
    [SerializeField] private Transform rollingObject;

    [Header("転がる設定")]
    [SerializeField] private float tileSize = 1f;

    [Header("立方体の一辺の長さ")]
    [SerializeField] private float cubeSize = 1f;

    private Vector3 previousPosition;
    private Vector3 initialLocalPosition;

    // 現在の1マス内での回転角度
    private float currentAngle = 0f;

    private void Start()
    {
        previousPosition = transform.position;

        if (rollingObject != null)
        {
            initialLocalPosition = rollingObject.localPosition;
        }
    }

    private void LateUpdate()
    {
        if (rollingObject == null || tileSize <= 0f)
            return;

        // 前フレームからの移動量
        Vector3 movement =
            transform.position - previousPosition;

        movement.y = 0f;

        float distance = movement.magnitude;

        if (distance > 0.0001f)
        {
            // 移動距離に応じた回転角度
            float deltaAngle =
                distance / tileSize * 90f;

            // 回転軸
            Vector3 axis = Vector3.Cross(
                Vector3.up,
                movement.normalized
            );

            // 回転
            rollingObject.Rotate(
                axis,
                deltaAngle,
                Space.World
            );

            // 現在の回転角度
            currentAngle =
                (currentAngle + deltaAngle) % 90f;

            // 回転途中の持ち上げ量
            float rad = currentAngle * Mathf.Deg2Rad;

            float heightOffset =
                cubeSize * 0.5f *
                (Mathf.Abs(Mathf.Cos(rad)) +
                 Mathf.Abs(Mathf.Sin(rad)) - 1f);

            // めり込み防止
            Vector3 localPos = initialLocalPosition;
            localPos.y += heightOffset;

            rollingObject.localPosition = localPos;
        }

        previousPosition = transform.position;
    }
}

