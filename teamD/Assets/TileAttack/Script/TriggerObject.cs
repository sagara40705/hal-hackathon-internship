using UnityEngine;
/// <summary>
/// Playerがタイルに触れたとき指定したObjectを飛ばす
/// 作成者：牛田大翔
/// </summary>
public class TriggerObject : MonoBehaviour
{
    [Header("発射するオブジェクト")]
    public GameObject attackPrefab;

    [Header("発射位置")]
    public Transform spawnPoint;

    [Header("発射速度")]
    public float speed = 10f;

    public void OnEnterPlayer(Player player)
    {

        // 敵を探す
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");

        if (enemy == null)
        {
            Debug.Log("Enemyタグの敵が見つかりません");
            return;
        }

        // 発射位置を決める
        Vector3 position = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        // 攻撃オブジェクトを生成
        GameObject obj = Instantiate(
            attackPrefab,
            position,
            Quaternion.identity
        );

        // 敵に向かって発射
        FlyingObject flying = obj.GetComponent<FlyingObject>();

        if (flying != null)
        {
            flying.Launch(enemy.transform, speed);
        }
        else
        {
            Debug.LogError("攻撃PrefabにFlyingObjectがありません");
            Destroy(obj);
        }

    }
}

