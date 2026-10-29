using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 敵のHP表示スクリプト
/// 作成者：牛田大翔
/// </summary>
public class EnemyHP : MonoBehaviour
{
    [Header("HP設定")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("HPバー")]
    public Slider hpSlider;

    [SerializeField, Tooltip("Enemyが倒されたときに呼ばれるイベント")]
    private UnityEvent _onDestroy;

    void Start()
    {
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
    }

    // ダメージを受ける
    public void TakeDamage(float damage)
    {
        float hpBeforeDamage = currentHP;
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateHPBar();
        // HPが実際に減った場合だけ再生する。
        if (currentHP < hpBeforeDamage)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySEById(SoundId.EnemyDamage);
        }
        // カメラシェイク
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake();
        }
        // ヒットストップ
        if (HitStop.Instance != null)
        {
            HitStop.Instance.Stop();
        }
        if (currentHP <= 0)
        {
            _onDestroy?.Invoke();
        }
    }

    // HPバーを更新
    void UpdateHPBar()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }
    }
}

