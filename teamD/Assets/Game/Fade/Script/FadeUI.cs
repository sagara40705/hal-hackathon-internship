using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// フェードイン・フェードアウトを行うUI
/// 作成者：石田怜
/// </summary>
public class FadeUI : MonoBehaviour
{
    //[SerializeField, Tooltip("フェード状態")]
    //private bool _isFade = false;

    //[SerializeField, Tooltip("フェード割合")]
    //private float _fadeRatio = 1.0f;

    //[SerializeField, Tooltip("フェード速度")]
    //private float _fadeSpeed = 1.0f;


    [SerializeField, Tooltip("シーン内の四角トランジション")]
    private SquareGridTransition _transition;

    [SerializeField, Tooltip("フェード用イメージ")]
	private Image _image;

    [SerializeField, Tooltip("画面を覆い終わったときの処理")]
    private UnityEvent _onFadeOutCompleted = new UnityEvent();

    private bool _closingRequested;

    private UnityAction _action;        // フェードアウト完了時のコールバック

    private void Awake()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        // 透明度フェードは使わないため、古いImageだけを止める。
        if (_image != null)
        {
            _image.raycastTarget = false;
            _image.enabled = false;
        }
    }

    private void Start()
    {
        // Awakeの実行順に依存しないよう、Startで取得する。
        ResolveTransition();

        if (_transition == null || !_transition.isActiveAndEnabled)
            return;

        // シーン開始時に、中央から外側へ開く。
        if (!_closingRequested)
            _transition.Open();
    }

    /// <summary>
    /// 既存の呼び出し方を維持する。
    /// </summary>
    public void StartFadeOut(UnityAction action = null)
    {
        if (!isActiveAndEnabled || _closingRequested)
            return;

        ResolveTransition();

        if (_transition == null || !_transition.isActiveAndEnabled)
            return;

        _closingRequested = true;

        // 演出が終わったら、既存の完了処理を実行する。
        _transition.Close(action);
    }

    private void ResolveTransition()
    {
        if (_transition == null)
            _transition = SquareGridTransition.Instance;
    }


    // Inspectorのイベントから呼び出す。
    public void FadeOutFromEvent()
    {
        StartFadeOut(() =>
        {
            _onFadeOutCompleted.Invoke();
        });
    }
}
