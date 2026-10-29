using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnergyGridTransition : MonoBehaviour
{
    public static EnergyGridTransition Instance { get; private set; }

    public bool IsTransitioning { get; private set; }

    [Header("参照")]
    [SerializeField]
    private RawImage _transitionImage;

    [Header("演出時間")]
    [SerializeField, Min(0f)]
    private float _spreadTime = 0.45f;

    [SerializeField, Min(0.01f)]
    private float _tileTime = 0.25f;

    [SerializeField, Min(0f)]
    private float _holdTime = 0.1f;

    private Material _runtimeMaterial;
    private Material _originalMaterial;

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int OpeningId = Shader.PropertyToID("_Opening");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int SpreadTimeId = Shader.PropertyToID("_SpreadTime");
    private static readonly int TileTimeId = Shader.PropertyToID("_TileTime");

    private void Awake()
    {
        // シーン切り替え先に同じPrefabがあっても重複させない。
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (_transitionImage == null ||
            _transitionImage.material == null ||
            !_transitionImage.material.HasProperty(ProgressId))
        {
            Debug.LogError(
                "Transition Imageと専用Materialを設定してください。",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        // シーン読み込み中もCanvasを表示し続ける。
        DontDestroyOnLoad(gameObject);

        // 元のMaterialを書き換えないように複製する。
        _originalMaterial = _transitionImage.material;
        _runtimeMaterial = new Material(_originalMaterial);
        _transitionImage.material = _runtimeMaterial;

        SetState(false, 0f);

        _transitionImage.raycastTarget = false;
        _transitionImage.enabled = false;
    }

    private void LateUpdate()
    {
        if (_runtimeMaterial == null)
            return;

        // 表示領域の比率をShaderへ渡す。
        Rect rect = _transitionImage.rectTransform.rect;

        float aspect = rect.width / Mathf.Max(rect.height, 1f);
        _runtimeMaterial.SetFloat(AspectId, aspect);
    }

    // ボタンなどから呼び出す。
    public void TransitionTo(string sceneName)
    {
        if (IsTransitioning || !isActiveAndEnabled)
            return;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"シーン「{sceneName}」を読み込めません。" +
                "Build ProfilesのScene Listを確認してください。",
                this
            );

            return;
        }

        IsTransitioning = true;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        _transitionImage.enabled = true;
        _transitionImage.raycastTarget = true;

        // 今回の演出時間を設定する。
        _runtimeMaterial.SetFloat(
            SpreadTimeId,
            Mathf.Max(0f, _spreadTime)
        );

        _runtimeMaterial.SetFloat(
            TileTimeId,
            Mathf.Max(0.01f, _tileTime)
        );

        Canvas.ForceUpdateCanvases();
        LateUpdate();

        // 中央から四角が広がって画面を覆う。
        yield return Animate(false);

        yield return new WaitForSecondsRealtime(_holdTime);

        // 確認用ではシーン切り替えを行わない。
        if (!string.IsNullOrEmpty(sceneName))
        {
            AsyncOperation operation = null;

            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;
            }

            // 新しいシーンの開始処理を待つ。
            yield return null;
        }

        // 中央から四角が縮み、新しい画面を見せる。
        yield return Animate(true);

        _transitionImage.raycastTarget = false;
        _transitionImage.enabled = false;

        IsTransitioning = false;
    }

    private IEnumerator Animate(bool opening)
    {
        float duration =
            Mathf.Max(0f, _spreadTime) +
            Mathf.Max(0.01f, _tileTime);

        float elapsed = 0f;

        SetState(opening, 0f);

        while (elapsed < duration)
        {
            // ポーズ中でも進む時間を使う。
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            SetState(opening, progress);

            yield return null;
        }

        SetState(opening, 1f);
    }

    private void SetState(bool opening, float progress)
    {
        _runtimeMaterial.SetFloat(
            OpeningId,
            opening ? 1f : 0f
        );

        _runtimeMaterial.SetFloat(ProgressId, progress);
    }

    // シーンを切り替えずに見た目だけ確認する。
    [ContextMenu("確認用：光る画面遷移")]
    private void PreviewTransition()
    {
        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            Instance != this ||
            IsTransitioning)
        {
            return;
        }

        IsTransitioning = true;
        StartCoroutine(TransitionRoutine(null));
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            if (_transitionImage != null)
                _transitionImage.material = _originalMaterial;

            Destroy(_runtimeMaterial);
        }

        if (Instance == this)
            Instance = null;
    }
}
