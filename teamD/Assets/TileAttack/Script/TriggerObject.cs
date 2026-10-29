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

    [SerializeField, Tooltip("攻撃可能状態エフェクト")]
    private ParticleSystem _activeEffect;

    private bool _isAttackEnabled = true;       // 攻撃可能フラグ

    public void OnEnterPlayer(Player player)
    {
        if (!_isAttackEnabled)
            return;

        // 敵を探す
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");

        if (enemy == null)
        {
            Debug.Log("Enemyタグの敵が見つかりません");
            return;
        }

        // 大砲が指定されている場合は砲口から発射する。
        CannonFireAnimation cannon = spawnPoint != null
            ? spawnPoint.GetComponentInParent<CannonFireAnimation>()
            : null;
        Transform shotOrigin = cannon != null ? cannon.Muzzle : spawnPoint;
        Vector3 position = shotOrigin != null
            ? shotOrigin.position
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
            // 攻撃を発射したときの音。
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySEById(SoundId.PlayerAttack);
            if (cannon != null)
                cannon.PlayFire();
        }
        else
        {
            Debug.LogError("攻撃PrefabにFlyingObjectがありません");
            Destroy(obj);
        }

        // 攻撃可能フラグをリセット
        DisableAttack();

        TriggerObject[] triggers = FindObjectsByType<TriggerObject>(FindObjectsSortMode.None);
        foreach (TriggerObject trigger in triggers)
        {
            // 自分以外のTriggerObjectの攻撃可能フラグをリセット
            if (trigger != this)
            {
                trigger.EnableAttack();
            }
        }
    }

    public void DisableAttack()
    {
        _isAttackEnabled = false;
        if (_activeEffect != null)
            _activeEffect.Stop(true);
    }

    public void EnableAttack()
    {
        _isAttackEnabled = true;
        if (_activeEffect != null)
            _activeEffect.Play(true);
    }
}

