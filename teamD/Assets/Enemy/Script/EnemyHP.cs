using UnityEngine;
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
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateHPBar();
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
            Destroy(gameObject);
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

