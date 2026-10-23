using UnityEngine;

/// <summary>
/// タイルのドラッグ状態に合わせて手のカーソルを切り替える。
/// シーン内の1つのオブジェクトに付けて使用する。
/// </summary>
public class GameCursorController : MonoBehaviour
{
    [SerializeField, Tooltip("ドラッグ状態を取得するタイルグリッドコントローラー")]
    private TileGridController _tileGridController;

    [SerializeField, Tooltip("通常時の開いた手の画像。Texture TypeをCursorに設定する")]
    private Texture2D _openHandCursor;

    [SerializeField, Tooltip("ドラッグ中の掴んだ手の画像。Texture TypeをCursorに設定する")]
    private Texture2D _grabHandCursor;

    [SerializeField, Tooltip("クリック位置。画像左上が(0, 0)、右下が(1, 1)")]
    private Vector2 _hotspot = new Vector2(0.5f, 0.6f);

    private bool _wasDragging;

    private bool IsDragging => _tileGridController != null &&
        _tileGridController.isActiveAndEnabled && _tileGridController.IsDragging;

    private void OnEnable()
    {
        // 同じオブジェクトに付けた場合は参照を自動取得する。
        if (_tileGridController == null)
            _tileGridController = GetComponent<TileGridController>();

        ApplyCursor(IsDragging);
    }

    private void LateUpdate()
    {
        // タイル側のUpdate後に確認し、状態が変わったときだけ画像を更新する。
        bool dragging = IsDragging;
        if (dragging != _wasDragging)
            ApplyCursor(dragging);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && isActiveAndEnabled)
            ApplyCursor(IsDragging);
    }

    private void OnDisable()
    {
        // シーン終了・無効化時にカーソル設定を戻す。
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void ApplyCursor(bool dragging)
    {
        Texture2D texture = dragging ? _grabHandCursor : _openHandCursor;
        Vector2 hotspot = Vector2.zero;

        if (texture != null)
        {
            hotspot = new Vector2(
                Mathf.Clamp01(_hotspot.x) * (texture.width - 1),
                Mathf.Clamp01(_hotspot.y) * (texture.height - 1));
        }

        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        _wasDragging = dragging;
    }
}
