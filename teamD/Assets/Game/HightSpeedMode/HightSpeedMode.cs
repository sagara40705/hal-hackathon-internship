using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>画像ボタンの押下状態を高速化Controllerへ通知する。</summary>
[DisallowMultipleComponent]
public class HightSpeedMode : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private HighSpeedController _controller;
    [SerializeField] private UnityEngine.UI.Image _image;
    private int? _pointerId;
    private bool CanSpeedUp => _controller != null && _controller.CanSpeedUp;

    private void Awake()
    {
        // 透過部分はクリックを通す。SpriteはRead/Write有効で取り込む。
        if (_image != null) _image.alphaHitTestMinimumThreshold = 0.1f;
    }

    private void OnEnable()
    {
        if (_controller != null) _controller.StateChanged += OnStateChanged;
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        if (_controller == null || !_controller.IsRequested) _pointerId = null;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (_image == null) return;
        _image.color = !CanSpeedUp ? new Color(0.7f, 0.7f, 0.7f, 0.4f)
            : _pointerId.HasValue ? new Color(0.7f, 0.75f, 0.75f, 1f) : Color.white;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || _controller == null || _pointerId.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
        if (!_controller.BeginSpeedUp()) return;
        _pointerId = eventData.pointerId;
        UpdateAppearance();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_pointerId == eventData.pointerId) Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pointerId == eventData.pointerId) Release();
    }

    private void Release()
    {
        if (_pointerId.HasValue && _controller != null) _controller.EndSpeedUp();
        _pointerId = null;
        UpdateAppearance();
    }

    private void OnDisable()
    {
        Release();
        if (_controller != null) _controller.StateChanged -= OnStateChanged;
    }
}
