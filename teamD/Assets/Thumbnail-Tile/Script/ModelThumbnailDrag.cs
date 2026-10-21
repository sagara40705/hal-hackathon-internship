using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UIサムネイルのドラッグを受け取り、モデル配置処理へ伝えるクラス。
/// 作成者 : 酒井康佑
/// </summary>
public class ModelThumbnailDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField, Tooltip("このサムネイルをドラッグしたときに配置するPrefab")]
    private GameObject modelPrefab;

    [SerializeField, Tooltip("配置とプレビューを管理するGameObject")]
    private ModelPlacementController placementController;

    /// <summary>
    /// ドラッグ開始時に、対応するモデルのプレビューを作る。
    /// </summary>
    /// <param name="eventData">ドラッグ開始位置などのポインター情報</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 必要な参照がInspectorで設定されているか確認する。
        if (modelPrefab == null || placementController == null)
        {
            Debug.LogWarning("Prefab または ModelPlacementController が設定されていません。");
            return;
        }

        placementController.BeginPreview(modelPrefab);
        placementController.UpdatePreview(eventData.position);
    }

    /// <summary>
    /// ドラッグ中、ポインターの位置に合わせてプレビューを更新する。
    /// </summary>
    /// <param name="eventData">現在のポインター位置などの情報</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (placementController != null)
            placementController.UpdatePreview(eventData.position);
    }

    /// <summary>
    /// ドラッグ終了時に配置を試み、プレビューを片付ける。
    /// </summary>
    /// <param name="eventData">ドラッグ終了位置などのポインター情報</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (modelPrefab == null || placementController == null)
            return;

        bool placed = placementController.EndPreviewAndPlace(modelPrefab, eventData.position);

        // 配置に成功したらサムネイルを非表示にする
        if (placed)
        gameObject.SetActive(false); 
    }

    /// <summary>
    /// 生成したサムネイルに、配置するPrefabとコントローラーを設定する
    /// </summary>
    public void Initialize(GameObject prefab, ModelPlacementController controller)
    {
        modelPrefab = prefab;
        placementController = controller;
    }

}
