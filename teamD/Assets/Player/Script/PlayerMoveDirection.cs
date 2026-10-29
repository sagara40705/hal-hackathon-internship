using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveDirection : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Player player;
    [SerializeField] private GameObject objectPrefab;

    [Header("生成位置の基準")]
    [SerializeField] private Transform targetObject;
    [SerializeField] private float spawnDistance = 1.0f;
    [SerializeField] private float spawnHeight = 0.5f;

    [Header("生成時の回転")]
    [SerializeField] private Vector3 spawnRotation = Vector3.zero;

    private void Start()
    {
        SpawnObject();
    }

    public void SpawnObject()
    {
        if (player == null || objectPrefab == null ||
            targetObject == null)
        {
            Debug.LogWarning(
                "Player、Prefab、Target Objectを設定してください。");
            return;
        }

        // Playerの進行方向を取得
        Vector2Int direction = player.Direction;

        // グリッドの向きに合わせてワールド方向に変換
        Vector3 worldDirection =
            player.Grid.transform.right * direction.x +
            player.Grid.transform.forward * direction.y;

        // 選択したオブジェクトを基準に生成位置を計算
        Vector3 spawnPosition =
            targetObject.position
            + worldDirection.normalized * spawnDistance
            + player.Grid.transform.up * spawnHeight;

        // Inspectorで設定した回転を適用
        Quaternion rotation = Quaternion.Euler(spawnRotation);

        // オブジェクトを生成
        Instantiate(objectPrefab, spawnPosition, rotation);
    }
}
