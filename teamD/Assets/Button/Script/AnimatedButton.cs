using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタンのアニメーションを制御する
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Animator))]
public class AnimatedButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField, Tooltip("クリックアニメーション終了時のイベント")]
    private UnityEvent _onClickComplete = new UnityEvent();

    public UnityEvent OnClickComplete => _onClickComplete;

    private static readonly int HoverID = Animator.StringToHash("Hover");
    private static readonly int ExitID = Animator.StringToHash("Exit");
    private static readonly int NormalStateID = Animator.StringToHash("Normal");

    private Button _button;
    private Animator _animator;

    private bool _isPointerOver;
    private bool _isExiting;


    private void Awake()
    {
        _button = GetComponent<Button>();
        _animator = GetComponent<Animator>();

        _button.onClick.AddListener(PlayExit);
    }

    private void OnEnable()
    {
        _isExiting = false;
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(PlayExit);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        UpdateHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        UpdateHover();
    }

    public void OnSelect(BaseEventData eventData)
    {
        UpdateHover();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        UpdateHover();
    }

    private void UpdateHover()
    {
        if (_isExiting)
            return;

        bool isSelected = EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject;

        bool isHover = _button.interactable && (_isPointerOver || isSelected);

        _animator.SetBool(HoverID, isHover);
    }

    [ContextMenu("Play Exit")]
    public void PlayExit()
    {
        if (_isExiting || !_button.interactable)
            return;

        _isExiting = true;
        _button.interactable = false;
        _animator.SetTrigger("Exit");
    }

    // ボタンを押したアニメーション終了時に呼ぶイベント
    public void OnExitAnimationComplete()
    {
        if (!_isExiting)
            return;

        _onClickComplete.Invoke();
    }

    public void ResetButton()
    {
        _isExiting = false;
        _button.interactable = true;

        _animator.ResetTrigger(ExitID);
        _animator.Play(NormalStateID, 0, 0.0f);
        _animator.Update(0.0f);

        UpdateHover();
    }
}
