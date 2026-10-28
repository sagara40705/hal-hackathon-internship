using UnityEngine;

public class EffectBillboard : MonoBehaviour
{
    [SerializeField, Tooltip("表示に使うカメラ。空ならMainCameraを使う")]
    private Camera _targetCamera;

    private void OnEnable()
    {
        // 未設定ならMainCameraタグのカメラを取得する。
        if (_targetCamera == null)
            _targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_targetCamera == null)
            return;

        // 弾の移動・回転が終わってから、
        // 光の板をカメラと平行にする。
        transform.rotation = _targetCamera.transform.rotation;
    }
}
