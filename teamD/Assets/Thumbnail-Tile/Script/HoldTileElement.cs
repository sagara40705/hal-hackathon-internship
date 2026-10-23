using UnityEngine;
using UnityEngine.EventSystems;
using System;

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

    // タイルを使ったことを管理側に通知する
    private Action _onConsumed;

    // この枠に使用可能なタイルが残っているか
    public bool HasTile => _tilePrefab != null;

    /// <summary>
    /// タイルと、消費したときの通知先を設定する。
    /// </summary>
    public void Initialize(TileGridController tileGridController, TileGroup tilePrefab, Action onConsumed = null)
    {
        _tileGridController = tileGridController;
        _tilePrefab = tilePrefab;
        _onConsumed = onConsumed;

        // 表示先が設定されていなければ終了する。
        if (_tilePreviewSlot == null)
            return;

        // 表示するタイルがあれば、プレビューを生成する。
        if (tilePrefab != null)
        {
            _tilePreviewSlot.SetPreviewObject(tilePrefab.gameObject, tilePrefab.PreviewOffset);
        }
        else
        {
            // タイルがない枠は空にする。
            _tilePreviewSlot.ClearPreviewObject();
        }
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
    /// 配置に成功したら枠を空にし、管理側に通知する
    /// </summary>
    /// <param name="placed">配置に成功したか</param>
    private void OnEndDragFromController(bool placed)
    {
        if (!placed || _tilePrefab == null)
            return;

        _tilePrefab = null;
        _tilePreviewSlot.ClearPreviewObject();


        // 空にした後で、全枠の残数を確認してもらう
        _onConsumed?.Invoke();
    }
}
