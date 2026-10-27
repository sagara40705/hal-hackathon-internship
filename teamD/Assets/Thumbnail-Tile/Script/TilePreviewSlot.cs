using UnityEngine;

/// <summary>
/// プレビュー表示用のタイル撮影スロット
/// 作成者：石田怜
/// </summary>
public class TilePreviewSlot : MonoBehaviour
{
    [SerializeField, Tooltip("撮影用のカメラ")]
    private Camera _previewCamera;

    private readonly string _previewLayerName = "Preview";    // プレビュー表示用のレイヤー名

    private GameObject _previewObject;      // プレビュー表示用のオブジェクト

    // プレビューオブジェクトを設定する
    // 現在は元のオブジェクトをそのまま複製しているため、Updateなどが呼ばれることに注意
    public void SetPreviewObject(GameObject obj, Vector3 offset)
    {
        if (obj == null)
            return;

        // 古いプレビューオブジェクトを削除して新しいオブジェクトを生成
        Destroy(_previewObject);

        _previewObject = Instantiate(obj, transform);
        _previewObject.transform.localPosition = -offset;
        _previewObject.transform.localRotation = Quaternion.identity;
        SetLayerHierarchy(_previewObject, LayerMask.NameToLayer(_previewLayerName));
    }

    public void ClearPreviewObject()
    {
        Destroy(_previewObject);
    }

    // オブジェクトのレイヤーを設定する（子オブジェクトも含む）
    private void SetLayerHierarchy(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerHierarchy(child.gameObject, layer);
        }
    }
}
