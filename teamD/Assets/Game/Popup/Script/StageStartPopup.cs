using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Canvas配下へ配置し、カタログのIDを指定する。シーン開始時に1枚だけ表示する。</summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class StageStartPopup : MonoBehaviour
{
    [SerializeField, Tooltip("IDと表示画像を登録したカタログ")]
    private PopupCatalog _catalog;
    [SerializeField, Tooltip("このシーンで表示するポップアップID")]
    private string _popupId = "first_stage";
    [SerializeField, HideInInspector] private GameObject _view;
    [SerializeField, HideInInspector] private UnityEngine.UI.Image _popupImage;
    [SerializeField, HideInInspector] private UnityEngine.UI.Button _closeButton;

    private static StageStartPopup _active;
    private static int _closedFrame = -1;
    private float _previousTimeScale;
    private bool _previousCursorVisible;
    private CursorLockMode _previousCursorLock;
    private GameObject _previousSelection;

    public static bool IsShowing => _active != null;
    // 閉じたフレームも遮断し、同じクリックで背後のタイルを操作させない。
    public static bool BlocksGameplayInput => IsShowing || _closedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        _active = null;
        _closedFrame = -1;
    }

    private void Awake()
    {
        if (_view != null) _view.SetActive(false);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (_view == null || _popupImage == null || _closeButton == null ||
            _catalog == null || !_catalog.TryGetImage(_popupId, out Sprite image))
        {
            Debug.LogWarning($"StageStartPopup: ID '{_popupId}'、カタログ、Prefabの参照を確認してください。", this);
            return;
        }
        if (GetComponentInParent<Canvas>() == null || EventSystem.current == null)
        {
            Debug.LogWarning("StageStartPopup: Canvasの子に置き、シーンにEventSystemを用意してください。", this);
            return;
        }
        if (IsShowing)
        {
            Debug.LogWarning("StageStartPopup: 同時表示は1枚です。シーンにはPrefabを1つだけ配置してください。", this);
            return;
        }

        _popupImage.sprite = image;
        _previousTimeScale = Time.timeScale;
        _previousCursorVisible = Cursor.visible;
        _previousCursorLock = Cursor.lockState;
        _previousSelection = EventSystem.current.currentSelectedGameObject;
        _active = this;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _view.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_closeButton.gameObject);
    }

    /// <summary>共通ボタンから閉じ、表示前のゲーム速度とカーソル状態に戻す。</summary>
    public void Close()
    {
        if (_active != this) return;

        _active = null;
        _closedFrame = Time.frameCount;
        if (_view != null) _view.SetActive(false);
        Time.timeScale = _previousTimeScale;
        Cursor.lockState = _previousCursorLock;
        Cursor.visible = _previousCursorVisible;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(
                _previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null);
    }

    private void OnDisable() => Close();

    private void OnDestroy()
    {
        Close();
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
    }
}
