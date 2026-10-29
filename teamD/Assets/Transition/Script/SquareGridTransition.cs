using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 作成者 : 酒井康佑
/// </summary>
public class SquareGridTransition : MonoBehaviour
{
    public static SquareGridTransition Instance {  get; private set; }

    // 入力処理側からも確認できる遷移中フラグ
    public bool IsTransitioning { get; private set; }

    [Header("参照")]
    [SerializeField]
    private RectTransform _gridRoot;

    [SerializeField]
    private Image _inputBlocker;

    [Header("四角")]
    [SerializeField, Range(4,40)]
    private int _columns = 16;

    [SerializeField]
    private Color _coverColor = Color.black;

    [Header("時間")]
    [SerializeField, Min(0f), Tooltip("中央から端まで開始が広がる時間")]
    private float _spreadTime = 0.45f;

    [SerializeField, Min(0.01f), Tooltip("四角1つが拡大・縮小する時間")]
    private float _squareDuration = 0.25f;

    [SerializeField, Min(0f), Tooltip("画面を覆ってからの待ち時間")]
    private float _coveredHoldTime = 0.1f;

    private class Square
    {
        public RectTransform rect;
        public float distanceRatio;

        // 演出開始時の大きさ。
        public float startScale;
    }

    private readonly List<Square> _squares = new();

    // 閉じる処理の重複を防ぐ。
    private bool _closing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (_gridRoot == null || _inputBlocker == null)
        {
            Debug.LogError(
                "Grid RootとInput Blockerを設定してください。",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        // 開く演出が始まるまで、画面全体を覆っておく。
        _inputBlocker.gameObject.SetActive(true);
        _inputBlocker.enabled = true;
        _inputBlocker.raycastTarget = true;

        SetBlockerOpaque(true);

        _gridRoot.gameObject.SetActive(false);
    }


    public void TransitionTo(string sceneName)
    {
        if (!isActiveAndEnabled || IsTransitioning)
            return;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(
                "遷移先のシーン名とScene Listを確認してください。",
                this
            );
            return;
        }

        Close(() => SceneManager.LoadScene(sceneName));
    }
    /// <summary>
    /// 中央から外側へ四角を縮め、画面を見せる。
    /// </summary>
    public void Open()
    {
        if (!isActiveAndEnabled || IsTransitioning)
            return;

        _closing = false;
        IsTransitioning = true;

        StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// 外側から中央へ四角を拡大する。
    /// 完全に覆ったらonCompleteを1回だけ呼ぶ。
    /// </summary>
    public void Close(UnityAction onComplete = null)
    {
        if (!isActiveAndEnabled || _closing)
            return;

        _closing = true;
        IsTransitioning = true;

        // 開く途中でも、現在の大きさから閉じる演出へ移れる。
        StopAllCoroutines();

        StartCoroutine(CloseRoutine(onComplete));
    }

    // 初回だけ四角を生成する。
    private void PrepareGrid()
    {
        _gridRoot.gameObject.SetActive(true);
        _inputBlocker.raycastTarget = true;

        Canvas.ForceUpdateCanvases();

        if (_squares.Count == 0)
        {
            BuildGrid();

            // 閉じる演出を初めて使う場合にも備え、0から作る。
            SetAllScales(0f);
        }
    }

    private IEnumerator OpenRoutine()
    {
        PrepareGrid();

        // 開くときは、四角で覆った状態から始める。
        SetAllScales(1.02f);

        SetBlockerOpaque(false);

        yield return AnimateSquares(false);

        _gridRoot.gameObject.SetActive(false);
        _inputBlocker.raycastTarget = false;

        IsTransitioning = false;
    }

    private IEnumerator CloseRoutine(UnityAction onComplete)
    {
        PrepareGrid();

        SetBlockerOpaque(false);

        yield return AnimateSquares(true);

        // 完全に覆った状態でシーン切り替えを待つ。
        SetBlockerOpaque(true);

        yield return new WaitForSecondsRealtime(_coveredHoldTime);

        // ここではシーンを読み込まず、呼び出し元へ通知する。
        onComplete?.Invoke();

        // 次のシーンへ切り替わるまで、
        // IsTransitioningと_closingはtrueのままにする。
    }

    // 画面サイズに合わせて正方形を生成する
    private void BuildGrid()
    {
        foreach (Square square in _squares)
        {
            if (square.rect == null)
                continue;

            // Destroyが実行されるまで映らないようにする
            square.rect.gameObject.SetActive(false);
            Destroy(square.rect.gameObject);
        }

        _squares.Clear();

        float width = Mathf.Max(1f, _gridRoot.rect.width);
        float height = Mathf.Max(1f, _gridRoot.rect.height);

        int columns = Mathf.Max(4, _columns);
        float cellSize = width / columns;

        // 縦方向も画面橋まで覆える行数を用意する
        int rows = Mathf.CeilToInt(height / cellSize);

        float gridWidth = columns * cellSize;
        float gridHeight = rows * cellSize;

        float maxDistance = 0.0001f;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject obj =
                    new GameObject($"Square_{x}_{y}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.SetParent(_gridRoot, false);

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.one * cellSize;

                Vector2 position = new Vector2(
                    -gridWidth * 0.5f + (x + 0.5f) * cellSize,
                    -gridHeight * 0.5f + (y + 0.5f) * cellSize
                );

                rect.anchoredPosition = position;
                rect.localScale = Vector3.zero;

                Image image = obj.GetComponent<Image>();

                Color color = _coverColor;
                color.a = 1f;

                image.color = color;
                image.raycastTarget = false;

                float distance = position.magnitude;
                maxDistance = Mathf.Max(maxDistance, distance);

                _squares.Add(new Square
                {
                    rect = rect,
                    distanceRatio = distance
                });
            }
        }

        // 距離を0～1程度の値へ変換する
        foreach (Square square in _squares)
            square.distanceRatio /= maxDistance; 
    }

    private IEnumerator AnimateSquares(bool covering)
    {
        float spread = Mathf.Max(0f, _spreadTime);
        float duration = Mathf.Max(0.01f, _squareDuration);
        float totalTime = spread + duration;
        float elapsed = 0f;

        // 現在の大きさを記録する。
        foreach (Square square in _squares)
            square.startScale = square.rect.localScale.x;

        float targetScale = covering ? 1.02f : 0f;

        while (elapsed < totalTime)
        {
            elapsed += Time.unscaledDeltaTime;

            foreach (Square square in _squares)
            {
                // 閉じる：外側→中央
                // 開く：中央→外側
                float order = covering
                    ? 1f - square.distanceRatio
                    : square.distanceRatio;

                float delay = order * spread;

                float t = Mathf.Clamp01(
                    (elapsed - delay) / duration
                );

                float eased = Mathf.SmoothStep(0f, 1f, t);

                float scale = Mathf.Lerp(
                    square.startScale,
                    targetScale,
                    eased
                );

                square.rect.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        SetAllScales(targetScale);
    }

    private void SetAllScales(float scale)
    {
        foreach (Square square in _squares)
            square.rect.localScale = Vector3.one * scale;
    }


    private void SetBlockerOpaque(bool opaque)
    {
        Color color = _coverColor;
        color.a = opaque ? 1f : 0f;
        _inputBlocker.color = color;
    }

    // シーンを変えずに、閉じる・開く動きだけを確認する
    [ContextMenu("確認用：画面遷移")]
    private void PreviewTransition()
    {
        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            Instance != this ||
            IsTransitioning)
        {
            return;
        }

        // 閉じた後、そのまま開く。シーンは変えない。
        Close(() =>
        {
            _closing = false;
            IsTransitioning = false;

            Open();
        });
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    /// <summary>
    /// 四角の演出と入力ブロックを解除する。
    /// 通常フェードへ切り替えるときにも使用する。
    /// </summary>
    public void ResetVisual()
    {
        StopAllCoroutines();

        _closing = false;
        IsTransitioning = false;

        SetAllScales(0f);
        _gridRoot.gameObject.SetActive(false);

        SetBlockerOpaque(false);
        _inputBlocker.raycastTarget = false;
    }

}
