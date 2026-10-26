using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトルロゴの移動・回転中に残像を出し、
/// 登場終了後に光沢を横切らせる。
/// </summary>
public class TitleLogoEffects : MonoBehaviour
{
    [Header("参照")]
    [SerializeField, Tooltip("演出を付けるロゴのImage一覧")]
    private Image[] _logoImages;

    [SerializeField, Tooltip("Canvas直下の残像用の親")]
    private RectTransform _afterimageRoot;

    [SerializeField, Tooltip("UI/LogoShineを使ったMaterial")]
    private Material _shineMaterial;

    [Header("残像")]
    [SerializeField, Min(0.01f), Tooltip("残像を出す間隔")]
    private float _emissionInterval = 0.05f;

    [SerializeField, Min(0.01f), Tooltip("残像が消えるまでの時間")]
    private float _afterimageLifetime = 0.18f;

    [SerializeField, Range(0f, 1f), Tooltip("残像の濃さ")]
    private float _afterimageAlpha = 0.25f;

    [Header("光沢")]
    [SerializeField, Min(0f), Tooltip("登場終了から光沢までの待ち時間")]
    private float _shineDelay = 0.15f;

    [SerializeField, Min(0.01f), Tooltip("光が画面を横切る時間")]
    private float _shineDuration = 0.8f;

    [SerializeField, Min(0f), Tooltip("次の光沢までの待ち時間。0なら一度だけ")]
    private float _repeatInterval = 5f;

    private static readonly int ShinePositionId =
        Shader.PropertyToID("_ShinePosition");

    private Material _runtimeMaterial;
    private Material[] _originalMaterials;
    private Vector3[] _previousPositions;
    private Quaternion[] _previousRotations;

    private bool _emitting;
    private float _emissionTimer;

    private class Afterimage
    {
        public Image image;
        public Color initialColor;
        public float age;
    }

    private readonly List<Afterimage> _afterimages = new();

    private void OnEnable()
    {
        int count = _logoImages != null ? _logoImages.Length : 0;

        _originalMaterials = new Material[count];
        _previousPositions = new Vector3[count];
        _previousRotations = new Quaternion[count];

        if (_shineMaterial != null)
        {
            // 他のUIへ影響しないよう、実行用Materialを作る。
            _runtimeMaterial = new Material(_shineMaterial);
            _runtimeMaterial.SetFloat(ShinePositionId, -2f);
        }

        for (int i = 0; i < count; i++)
        {
            Image source = _logoImages[i];

            if (source == null)
                continue;

            _originalMaterials[i] = source.material;
            _previousPositions[i] = source.transform.position;
            _previousRotations[i] = source.transform.rotation;

            if (_runtimeMaterial != null)
                source.material = _runtimeMaterial;
        }

        _emitting = true;
        _emissionTimer = 0f;
    }

    private void LateUpdate()
    {
        // 既存の残像を少しずつ透明にする。
        for (int i = _afterimages.Count - 1; i >= 0; i--)
        {
            Afterimage ghost = _afterimages[i];

            if (ghost.image == null)
            {
                _afterimages.RemoveAt(i);
                continue;
            }

            ghost.age += Time.deltaTime;

            float t = Mathf.Clamp01(
                ghost.age / Mathf.Max(0.01f, _afterimageLifetime)
            );

            Color color = ghost.initialColor;
            color.a *= 1f - t;
            ghost.image.color = color;

            if (t >= 1f)
            {
                Destroy(ghost.image.gameObject);
                _afterimages.RemoveAt(i);
            }
        }

        if (!_emitting ||
            _afterimageRoot == null ||
            _logoImages == null)
        {
            return;
        }

        _emissionTimer += Time.deltaTime;

        if (_emissionTimer < _emissionInterval)
            return;

        _emissionTimer = 0f;

        // Animatorが更新した後の位置・回転を記録する。
        for (int i = 0; i < _logoImages.Length; i++)
        {
            Image source = _logoImages[i];

            if (source == null)
                continue;

            bool moved =
                (source.transform.position - _previousPositions[i])
                .sqrMagnitude > 0.01f;

            bool rotated = Quaternion.Angle(
                source.transform.rotation,
                _previousRotations[i]
            ) > 0.5f;

            if (source.isActiveAndEnabled &&
                source.color.a > 0.01f &&
                (moved || rotated))
            {
                CreateAfterimage(source);
            }

            _previousPositions[i] = source.transform.position;
            _previousRotations[i] = source.transform.rotation;
        }
    }

    private void CreateAfterimage(Image source)
    {
        Sprite sprite = source.overrideSprite;

        if (sprite == null)
            return;

        GameObject obj = new GameObject(
            "LogoAfterimage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        Image image = obj.GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        RectTransform sourceRect = source.rectTransform;

        rect.SetParent(_afterimageRoot, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        // 現在の形・位置・回転を複製する。
        rect.pivot = sourceRect.pivot;
        rect.sizeDelta = sourceRect.rect.size;
        rect.position = sourceRect.position;
        rect.rotation = sourceRect.rotation;

        Vector3 sourceScale = sourceRect.lossyScale;
        Vector3 parentScale = _afterimageRoot.lossyScale;

        rect.localScale = new Vector3(
            sourceScale.x / Mathf.Max(0.0001f, parentScale.x),
            sourceScale.y / Mathf.Max(0.0001f, parentScale.y),
            1f
        );

        image.sprite = sprite;
        image.type = source.type;
        image.preserveAspect = source.preserveAspect;
        image.fillAmount = source.fillAmount;
        image.fillMethod = source.fillMethod;
        image.fillOrigin = source.fillOrigin;
        image.fillClockwise = source.fillClockwise;

        // 残像には通常のUI Materialを使う。
        image.material = null;
        image.raycastTarget = false;

        Color color = source.color;
        color.a *= _afterimageAlpha;
        image.color = color;

        _afterimages.Add(new Afterimage
        {
            image = image,
            initialColor = color,
            age = 0f
        });
    }

    /// <summary>
    /// 登場アニメーションの最後から呼び出す。
    /// </summary>
    public void FinishIntro()
    {
        if (!_emitting)
            return;

        // 新しい残像は止め、すでに出た残像だけ消えていく。
        _emitting = false;

        if (_runtimeMaterial != null)
            StartCoroutine(ShineRoutine());
    }

    private IEnumerator ShineRoutine()
    {
        yield return new WaitForSeconds(_shineDelay);

        while (true)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _shineDuration);

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);

                // 画面左の外側から、右の外側まで光を動かす。
                _runtimeMaterial.SetFloat(
                    ShinePositionId,
                    Mathf.Lerp(-0.8f, 1.8f, t)
                );

                yield return null;
                elapsed += Time.deltaTime;
            }

            // 光を画面外に戻す。
            _runtimeMaterial.SetFloat(ShinePositionId, -2f);

            if (_repeatInterval <= 0f)
                yield break;

            yield return new WaitForSeconds(_repeatInterval);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        foreach (Afterimage ghost in _afterimages)
        {
            if (ghost.image != null)
                Destroy(ghost.image.gameObject);
        }

        _afterimages.Clear();

        // 元のMaterialへ戻す。
        if (_logoImages != null && _originalMaterials != null)
        {
            int count = Mathf.Min(
                _logoImages.Length,
                _originalMaterials.Length
            );

            for (int i = 0; i < count; i++)
            {
                if (_logoImages[i] != null)
                    _logoImages[i].material = _originalMaterials[i];
            }
        }

        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);

        _runtimeMaterial = null;
    }
}
