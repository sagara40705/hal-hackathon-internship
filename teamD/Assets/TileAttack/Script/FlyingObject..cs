using UnityEngine;
using UnityEngine.WSA;
using static UnityEngine.GraphicsBuffer;
/// <summary>
/// TriggerObjectで指定した物が、敵に向かって飛んでいきダメージを与える
/// 作成者：牛田大翔
/// </summary>
public class FlyingObject: MonoBehaviour
{
    public float flightDuration = 1.5f;
    public float arcHeight = 3f;
    public int damage = 10;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float elapsedTime;
    private bool launched;

    public void Launch(Transform enemy, float launchSpeed)
    {
        if (enemy == null)
            return;

        startPosition = transform.position;
        targetPosition = enemy.position;

        // speedを使って飛行時間を決める
        flightDuration = Vector3.Distance(
            startPosition, targetPosition
        ) / Mathf.Max(launchSpeed, 0.1f);

        flightDuration = Mathf.Max(flightDuration, 0.1f);
        elapsedTime = 0f;
        launched = true;

        Destroy(gameObject, flightDuration + 2f);
    }

    void Update()
    {
        if (!launched)
            return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(elapsedTime / flightDuration);

        // 始点から終点へ移動し、途中で上昇する
        Vector3 position = Vector3.Lerp(
            startPosition, targetPosition, t
        );

        position.y += arcHeight * 4f * t * (1f - t);

        Vector3 direction = position - transform.position;

        transform.position = position;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);

        // 飛行終了時に命中処理
        if (t >= 1f)
        {
            EnemyHP enemyHP = FindEnemyHPAtTarget();

            if (enemyHP != null)
                enemyHP.TakeDamage(damage);

            Destroy(gameObject);
        }

       

       
    }

    private EnemyHP FindEnemyHPAtTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            targetPosition, 1f
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHP hp = hit.GetComponentInParent<EnemyHP>();

                if (hp != null)
                    return hp;
            }
        }

        return null;
    }
}
