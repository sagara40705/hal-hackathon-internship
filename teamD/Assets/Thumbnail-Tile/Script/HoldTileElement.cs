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

    [Header("個別クールタイム")]
    [SerializeField, Tooltip("-1ならSpawnerの共通待ち時間を使用")]
    private float _cooldownSeconds = -1f;
    [SerializeField] private Color _cooldownColor = new Color(1f, 0.85f, 0.2f, 1f);
    private ButtonEffectGraphic _cooldownRing;
    public float CooldownSeconds => _cooldownSeconds;
    public RectTransform SlotRect => transform as RectTransform;

    // プレビューとは別のUIなので、枠が空でも表示できる。
    public void ShowCooldown(float progress)
    {
        if (_cooldownRing == null)
        {
            GameObject obj = new GameObject("CooldownRing",
                typeof(RectTransform), typeof(CanvasRenderer));
            obj.layer = gameObject.layer;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);
            _cooldownRing = obj.AddComponent<ButtonEffectGraphic>();
            _cooldownRing.Configure(true, _cooldownColor, 5f);
        }
        _cooldownRing.gameObject.SetActive(true);
        _cooldownRing.SetFillAmount(progress);
    }

    public void HideCooldown()
    {
        if (_cooldownRing != null)
            _cooldownRing.gameObject.SetActive(false);
    }

    // この枠に使用可能なタイルが残っているか
    public bool HasTile => _tilePrefab != null;

    /// <summary>
    /// タイルと、消費したときの通知先を設定する。
    /// </summary>
    public void Initialize(TileGridController tileGridController, TileGroup tilePrefab, Action onConsumed = null)
    {
        HideCooldown();
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
            if (_tilePreviewSlot != null) _tilePreviewSlot.ClearPreviewObject();
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
        if (_tileGridController == null ||
            !_tileGridController.BeginDragTile(tileGroup, OnEndDragFromController))
            Destroy(tileGroup.gameObject);
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
        if (_tilePreviewSlot != null) _tilePreviewSlot.ClearPreviewObject();

        // 消費した枠だけのクールタイムを開始してもらう
        _onConsumed?.Invoke();
    }
}
