using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 敵の破裂消滅エフェクトを制御する
/// </summary>
public class EnemyBurstEffect : MonoBehaviour
{
    [SerializeField, Tooltip("エフェクトを適用するRenderer")]
    private Renderer[] _renderers;

    [SerializeField, Tooltip("撃破エフェクト用Material")]
    private Material _burstMaterial;

    [SerializeField, Tooltip("破裂アニメーションの時間")]
    private float _duration = 0.45f;

    [SerializeField, Tooltip("エフェクト開始までの待ち時間")]
    private float _startDelay = 0.5f;

    [SerializeField, Tooltip("エフェクト終了からイベント実行までの待ち時間")]
    private float _eventDelay = 0.5f;

    [SerializeField, Tooltip("破裂アニメーションのカーブ")]
    private AnimationCurve _burstCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField, Tooltip("再生終了時にオブジェクトを破棄する")]
    private bool _destroyOnComplete = true;

    [SerializeField, Tooltip("エフェクト終了時のイベント")]
    private UnityEvent _onCompleteEvent = new UnityEvent();

    public UnityEvent OnCompleteEvent => _onCompleteEvent;

    private static readonly int BurstID = Shader.PropertyToID("_Burst");

    private MaterialPropertyBlock _propertyBlock;
    private Material[][] _originalMaterials;

    private Coroutine _coroutine;
    private bool _isPlaying;


    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();

        // 元のMaterialを保存
        _originalMaterials = new Material[_renderers.Length][];

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
                continue;

            _originalMaterials[i] = _renderers[i].sharedMaterials;
        }

        SetBurst(0.0f);
    }

    /// <summary>
    /// 破裂エフェクトを再生する
    /// </summary>
    [ContextMenu("Play")]
    public void Play()
    {
        if (_isPlaying || _burstMaterial == null)
            return;

        _isPlaying = true;

        // 撃破用Materialに切り替える
        ApplyBurstMaterial();

        _coroutine = StartCoroutine(PlayCoroutine());
    }

    /// <summary>
    /// 撃破用Materialを適用する
    /// </summary>
    private void ApplyBurstMaterial()
    {
        foreach (Renderer renderer in _renderers)
        {
            if (renderer == null)
                continue;

            // 全Materialスロットを撃破用Materialに置き換える
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
                materials[i] = _burstMaterial;

            renderer.sharedMaterials = materials;
        }
    }

    /// <summary>
    /// 元のMaterialに戻す
    /// </summary>
    private void RestoreMaterials()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null || _originalMaterials[i] == null)
                continue;

            _renderers[i].sharedMaterials = _originalMaterials[i];
        }
    }

    /// <summary>
    /// 破裂エフェクトを再生する
    /// </summary>
    private IEnumerator PlayCoroutine()
    {
        // エフェクト開始まで待機
        if (_startDelay > 0.0f)
            yield return new WaitForSeconds(_startDelay);

        float timer = 0.0f;

        SetBurst(0.0f);

        while (timer < _duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(_duration, 0.0001f));
            float burst = _burstCurve.Evaluate(t);

            SetBurst(burst);

            yield return null;
        }

        SetBurst(1.0f);

        // イベント実行まで待機
        if (_eventDelay > 0.0f)
            yield return new WaitForSeconds(_eventDelay);

        _coroutine = null;
        _isPlaying = false;

        // 再生終了イベントを実行
        _onCompleteEvent.Invoke();

        if (_destroyOnComplete)
            Destroy(gameObject);
    }

    /// <summary>
    /// シェーダーの破裂進行度を設定する
    /// </summary>
    private void SetBurst(float burst)
    {
        foreach (Renderer renderer in _renderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(BurstID, burst);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    /// <summary>
    /// 破裂状態を初期化する
    /// </summary>
    public void ResetEffect()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        _isPlaying = false;

        // 元のMaterialに戻す
        RestoreMaterials();

        SetBurst(0.0f);
    }
}
