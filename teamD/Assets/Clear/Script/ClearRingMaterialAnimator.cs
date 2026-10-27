using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// クリア演出用リングのマテリアルアニメーション
/// 作成者：石田怜
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
public class ClearRingMaterialAnimator : MonoBehaviour, IMaterialModifier
{
	[SerializeField, Range(0.0f, 1.0f)]
	private float _radius = 0.0f;

	[SerializeField, Range(0.0f, 1.0f)]
	private float _opacity = 1.0f;

	private Graphic _graphic;
	private Material _material;

	private static readonly int RadiusID = Shader.PropertyToID("_Radius");
	private static readonly int OpacityID = Shader.PropertyToID("_Opacity");

	private void OnEnable()
	{
		Initialize();
		Refresh();
	}

	private void OnDisable()
	{
		if (_material != null)
		{
			if (Application.isPlaying)
				Destroy(_material);
			else
				DestroyImmediate(_material);

			_material = null;
		}

		Refresh();
	}

	private void OnDidApplyAnimationProperties()
	{
		Refresh();
	}

	private void OnValidate()
	{
		Refresh();
	}

	// 描画用マテリアルを取得
	public Material GetModifiedMaterial(Material baseMaterial)
	{
		if (baseMaterial == null)
			return null;

		if (!isActiveAndEnabled)
			return baseMaterial;

		if (_material == null)
		{
			_material = new Material(baseMaterial)
			{
				name = "Clear Ring (Instance)",
				hideFlags = HideFlags.HideAndDontSave
			};
		}

		// 元のマテリアル設定を引き継ぐ
		_material.CopyPropertiesFromMaterial(baseMaterial);

		// アニメーション対象のパラメータを反映
		_material.SetFloat(RadiusID, _radius);
		_material.SetFloat(OpacityID, _opacity);

		return _material;
	}

	// 描画コンポーネントを取得
	private void Initialize()
	{
		if (_graphic == null)
			_graphic = GetComponent<Graphic>();
	}

	// マテリアルを再描画
	private void Refresh()
	{
		Initialize();

		if (_graphic == null)
			return;

		_graphic.SetMaterialDirty();
	}
}
