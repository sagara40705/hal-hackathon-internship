using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 画面遷移に使う演出の種類。
/// </summary>
public enum FadeType
{
    Normal,     // Imageの透明度を変える通常フェード
    SquareGrid  // 四角いトランジション
}

/// <summary>
/// 通常フェードと四角トランジションを切り替える。
/// 作成者：石田怜
/// </summary>
public class FadeUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField, Tooltip("通常フェード用の全面Image")]
    private Image _image;

    [SerializeField, Tooltip("シーン内の四角トランジション")]
    private SquareGridTransition _transition;

    [Header("通常フェード")]
    [SerializeField, Min(0.01f)]
    private float _fadeDuration = 0.5f;

    [Header("標準の演出")]
    [SerializeField, Tooltip("このシーンを直接再生したときの演出")]
    private FadeType _initialFadeIn = FadeType.SquareGrid;

    [SerializeField, Tooltip("種類を指定せずに画面を隠すときの演出")]
    private FadeType _defaultFadeOut = FadeType.SquareGrid;

    [SerializeField, Tooltip("その後のシーンを見せる演出")]
    private FadeType _defaultNextFadeIn = FadeType.SquareGrid;

    [Header("Inspectorから呼び出したときの完了処理")]
    [SerializeField]
    private UnityEvent _onFadeOutCompleted = new UnityEvent();

    // 次のシーンのFadeUIへ引き継ぐ情報。
    // シーン内のオブジェクト自体は保持しない。
    private static bool s_hasNextFadeIn;
    private static FadeType s_nextFadeIn;

    private FadeType _openingType;
    private bool _started;

    public bool IsBusy { get; private set; }

    // Domain Reloadが無効でも、Play開始時には初期状態へ戻す。
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_hasNextFadeIn = false;
        s_nextFadeIn = FadeType.Normal;
    }

    private void Awake()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        // 前のシーンからの指定があれば、1回だけ受け取る。
        _openingType = s_hasNextFadeIn
            ? s_nextFadeIn
            : _initialFadeIn;

        s_hasNextFadeIn = false;

        IsBusy = true;

        // 開く演出の準備ができるまで画面を覆う。
        SetNormalImage(1f, true);
    }

    private void Start()
    {
        _started = true;
        ResolveTransition();

        // 四角側の初期表示をいったん解除する。
        if (_transition != null && _transition.isActiveAndEnabled)
            _transition.ResetVisual();

        // 四角が未設定の場合は通常フェードを使う。
        if (_openingType == FadeType.SquareGrid
            && !CanUseSquare())
        {
            Debug.LogWarning(
                "SquareGridTransitionが未設定のため、通常フェードで開きます。",
                this);

            _openingType = FadeType.Normal;
        }

        StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// 既存の呼び出し方。
    /// Inspectorで設定した標準の演出を使用する。
    /// </summary>
    public void StartFadeOut(UnityAction action = null)
    {
        StartFadeOut(
            _defaultFadeOut,
            _defaultNextFadeIn,
            action);
    }

    /// <summary>
    /// 隠す演出と、次のシーンを見せる演出を指定する。
    /// 開始できた場合はtrueを返す。
    /// </summary>
    public bool StartFadeOut(
        FadeType fadeOut,
        FadeType nextFadeIn,
        UnityAction action)
    {
        if (!isActiveAndEnabled || !_started || IsBusy)
            return false;

        ResolveTransition();

        if (fadeOut == FadeType.Normal && _image == null)
        {
            Debug.LogError(
                "通常フェード用のImageを設定してください。",
                this);
            return false;
        }

        if (fadeOut == FadeType.SquareGrid && !CanUseSquare())
        {
            Debug.LogError(
                "有効なSquareGridTransitionを設定してください。",
                this);
            return false;
        }

        IsBusy = true;

        // 完全に覆った後、次のシーンへ渡す種類を記録する。
        UnityAction completed = () =>
        {
            SetNextFadeIn(nextFadeIn);
            action?.Invoke();
        };

        if (fadeOut == FadeType.SquareGrid)
        {
            SetNormalImage(0f, false);
            _transition.Close(completed);
        }
        else
        {
            if (CanUseSquare())
                _transition.ResetVisual();

            StartCoroutine(CloseNormalRoutine(completed));
        }

        return true;
    }

    /// <summary>
    /// 次に読み込むシーンで使う、開く演出を指定する。
    /// シーンを読み込む直前に設定する。
    /// </summary>
    public static void SetNextFadeIn(FadeType type)
    {
        s_nextFadeIn = type;
        s_hasNextFadeIn = true;
    }

    // 既存のゴールイベントから呼び出す。
    public void FadeOutFromEvent()
    {
        StartFadeOut(() => _onFadeOutCompleted.Invoke());
    }

    private IEnumerator OpenRoutine()
    {
        if (_openingType == FadeType.SquareGrid)
        {
            // 四角で覆ってから通常Imageを消す。
            _transition.Open();
            SetNormalImage(0f, false);

            while (_transition.IsTransitioning)
                yield return null;
        }
        else
        {
            yield return AnimateNormal(1f, 0f);
            SetNormalImage(0f, false);
        }

        IsBusy = false;
    }

    private IEnumerator CloseNormalRoutine(UnityAction completed)
    {
        yield return AnimateNormal(0f, 1f);

        // シーンが切り替わるまで覆った状態を維持する。
        completed?.Invoke();
    }

    private IEnumerator AnimateNormal(float from, float to)
    {
        if (_image == null)
            yield break;

        float duration = Mathf.Max(0.01f, _fadeDuration);
        float elapsed = 0f;

        // フェードインの場合、まず完全に黒い状態にする。
        SetNormalImage(from, true);

        // シーン読み込み直後でも、開始状態を描画してから進める。
        yield return null;

        while (elapsed < duration)
        {
            float deltaTime = Mathf.Min(
                Time.unscaledDeltaTime, 0.05f);

            elapsed += deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, progress);

            SetNormalImage(alpha, true);

            yield return null;
        }

        // 最後は指定した透明度に正確に合わせる。
        SetNormalImage(to, true);
    }

    private void SetNormalImage(float alpha, bool visible)
    {
        if (_image == null)
            return;

        Color color = _image.color;
        color.a = alpha;
        _image.color = color;

        _image.enabled = visible;
        _image.raycastTarget = visible;
    }

    private void ResolveTransition()
    {
        if (_transition == null)
            _transition = SquareGridTransition.Instance;
    }

    private bool CanUseSquare()
    {
        return _transition != null
            && _transition.isActiveAndEnabled;
    }
}
