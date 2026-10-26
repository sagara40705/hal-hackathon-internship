using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// マウスホール移動スクリプト横のみ
/// 作成者：牛田大翔
/// </summary>
public class MouseWheelMove : MonoBehaviour
{
    [Header("ホイール1回の移動距離")]
    [SerializeField] private float moveDistance = 3f;

    [Header("移動速度")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("移動範囲")]
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;

    private float targetX;

    private void Start()
    {
        targetX = transform.position.x;
    }

    private void Update()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            if (scroll != 0f)
            {
                // ホイール上：右
                // ホイール下：左
                targetX += Mathf.Sign(scroll) * moveDistance;

                targetX = Mathf.Clamp(targetX, minX, maxX);
            }
        }

        Vector3 pos = transform.position;

        // 目標位置まで滑らかに移動
        pos.x = Mathf.MoveTowards(
            pos.x,
            targetX,
            moveSpeed * Time.deltaTime
        );

        transform.position = pos;
    }
}
