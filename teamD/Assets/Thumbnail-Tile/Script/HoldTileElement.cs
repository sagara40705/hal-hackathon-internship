using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 手持ちタイルのUI要素
/// 作成者 : 酒井康佑
/// </summary>
public class HoldTileElement : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField, Tooltip("タイルのプレビュースロット")]
    private TilePreviewSlot _tilePreviewSlot;

    private TileGridController _tileGridController;     // タイルグリッドのコントローラー
    private TileGroup _tilePrefab;      // 生成するタイルのPrefab

    /// <summary>
    /// 初期化処理
    /// </summary>
    /// <param name="tileGridController">タイルグリッドのコントローラー</param>
    /// <param name="tilePrefab">生成するタイルのPrefab</param>
    public void Initialize(TileGridController tileGridController, TileGroup tilePrefab)
    {
        _tileGridController = tileGridController;
        _tilePrefab = tilePrefab;
        _tilePreviewSlot?.SetPreviewObject(tilePrefab.gameObject, tilePrefab.PreviewOffset);
    }

    /// <summary>
    /// ドラッグ操作の開始処理
    /// </summary>
    /// <param name="eventData">ドラッグ開始位置などのポインター情報</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 必要な参照が設定されているか確認する。
        if (_tilePrefab == null)
        {
            Debug.LogWarning("Prefabが設定されていません。");
            return;
        }

        // タイルを生成し、ドラッグ操作を開始する。
        TileGroup tileGroup = Instantiate(_tilePrefab, Vector3.zero, Quaternion.identity);
        _tileGridController.BeginDragTile(tileGroup, OnEndDragFromController);
    }

    /// <summary>
    /// ドラッグ中の処理
    /// </summary>
    /// <param name="eventData">現在のポインター位置などの情報</param>
    public void OnDrag(PointerEventData eventData)
    {
    }

    /// <summary>
    /// ドラッグ終了時の処理
    /// </summary>
    /// <param name="eventData">ドラッグ終了位置などのポインター情報</param>
    public void OnEndDrag(PointerEventData eventData)
    {
    }

    /// <summary>
    /// TileGridControllerからのドラッグ終了通知を受け取る
    /// </summary>
    /// <param name="placed">配置に成功したか</param>
    private void OnEndDragFromController(bool placed)
    {
        if (placed)
        {
            _tilePrefab = null;
            _tilePreviewSlot.ClearPreviewObject();
        }
    }
}
