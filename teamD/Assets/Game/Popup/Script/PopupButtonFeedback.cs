using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>一時停止中にも動く、共通ボタンのホバー・押下表現。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class PopupButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private bool _hovered;
    private bool _pressed;
    private UnityEngine.UI.Button _button;

    private void Awake() => _button = GetComponent<UnityEngine.UI.Button>();
    public void OnPointerEnter(PointerEventData eventData) => _hovered = true;
    public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) _pressed = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) _pressed = false;
    }
    private void Update()
    {
        float target = _button.IsInteractable() ? (_pressed ? 0.97f : _hovered ? 1.025f : 1) : 1;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target,
            1 - Mathf.Exp(-24 * Time.unscaledDeltaTime));
    }
    private void OnDisable()
    {
        _hovered = _pressed = false;
        transform.localScale = Vector3.one;
    }
}
