using System;
using UnityEngine;


/// <summary>
/// 作成者 : 酒井康佑
/// </summary>
public class EnemyProjectileEffect : MonoBehaviour
{
    [Header("移動")]
    [SerializeField, Min(0.01f), Tooltip("発射から着弾までの秒数")]
    private float _flightDuration = 1.5f;

    [SerializeField, Min(0f), Tooltip("軌道の山の高さ")]
    private float _arcHeight = 2f;

    [SerializeField, Tooltip("対象の原点から見た着弾一のずれ")]
    private Vector3 _targetLocalOffset = new Vector3(0f, 0.5f,0f);

    [Header("飛行中の見た目")]
    [SerializeField, Tooltip("弾の見た目をまとめた親")]
    private GameObject _visualRoot;

    [Header("着弾エフェクト")]
    [SerializeField]
    private GameObject _explosionPrefab;

    [SerializeField, Min(0.01f)]
    private float _explosionScale = 1f;

    [SerializeField, Min(0.1f), Tooltip("爆発を削除するまでの時間")]
    private float _explosionLifetime = 3f;

    [Header("単体確認用")]
    [SerializeField,Tooltip("確認時に追尾する、シーン内のブロック")]
    private Transform _previewTarget;

    // 追尾する対象
    private Transform _target;

    // 発射した位置
    private Vector3 _startPosition;

    // 発射してからの経過時間
    private float _elapsedTime;

    // 飛行中かどうか
    private bool _isFlying;
     
    // 着弾時に攻撃側へ通知するための処理
    private Action _onArrived;

    /// <summary>
    /// 対象を指定して発射する
    /// onArrivedには、着弾したときの処理を渡せる
    /// </summary>
    public void Launch(Transform target, Action onArrived = null)
    {
        if(target == null)
        {
            Destroy(gameObject);
            return;
        }

        _target = target;
        _onArrived = onArrived;
        _startPosition = transform.position;
        _elapsedTime = 0f;
        _isFlying = true;

        if (_visualRoot != null)
            _visualRoot.SetActive(true);

    }

    public void Update()
    {
        if (!_isFlying)
            return;

        // 対象が先に破壊された場合、球も消す
        if(_target == null)
        {
            _isFlying = false;
            _onArrived = null;
            Destroy(gameObject);
            return;
        }

        _elapsedTime += Time.deltaTime;

        float duration = MathF.Max(0.01f, _flightDuration);
        float t = Mathf.Clamp01(_elapsedTime / duration);

        // 対象の現在位置を毎フレーム取得する
        // ブロックを移動・回転しても着弾位置が追従する
        Vector3 targetPosition =
            _target.TransformPoint(_targetLocalOffset);

        // 発射地点から、対象の現在位置へ向かう
        Vector3 nextPosition =
            Vector3.Lerp(_startPosition, targetPosition, t);

        // 飛行途中が最も高くなる山なりの軌道を加える
        nextPosition += Vector3.up * (_arcHeight * 4f * t * (1f - t));

        Vector3 direction = nextPosition - transform.position;
        transform.position = nextPosition;

        // 弾を進行方向へ向ける
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);

        if (t >= 1f)
            Arrive();
    }

    // 着弾したときの処理
    private void Arrive()
    {
        // 同じ弾から着弾通知が複数回でないようにする
        _isFlying = false;

        // 飛行中の見た目を消す
        if (_visualRoot != null)
            _visualRoot.SetActive(false);

        // 着弾位置に爆発を生成する
        if(_explosionPrefab != null)
        {
            GameObject explosion =
                Instantiate(_explosionPrefab, transform.position, Quaternion.identity );

            explosion.transform.localScale *= _explosionScale;

            // 爆発がジーンに残り続けないようにする
            Destroy(explosion, _explosionLifetime);
        }

        // カメラ振動
        CameraShake.Instance.Shake();

        Action callback = _onArrived;
        _onArrived = null;

        // 弾を削除する
        Destroy(gameObject);

        // 攻撃側へ着弾を通知する
        callback?.Invoke();
    }

    // 再生中、確認用の弾を複製して発射する。
    [ContextMenu("確認用：発射")]
    private void PreviewLaunch()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("再生中に実行してください。", this);
            return;
        }

        if (_previewTarget == null)
        {
            Debug.LogWarning("Preview Targetを設定してください。", this);
            return;
        }

        // 自分を複製し、コピーした弾だけを飛ばす。
        EnemyProjectileEffect projectile = Instantiate(
            this,
            transform.position,
            transform.rotation
        );

        projectile.name = gameObject.name + "_Preview";

        // コピー側の見た目を表示する。
        if (projectile._visualRoot != null)
            projectile._visualRoot.SetActive(true);

        // コピーした弾を対象へ発射する。
        projectile.Launch(_previewTarget);

        // 発射元は残し、見た目だけ隠す。
        if (_visualRoot != null)
            _visualRoot.SetActive(false);
    }


}
