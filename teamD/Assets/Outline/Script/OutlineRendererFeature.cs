using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

/// <summary>
/// 指定レイヤーのアウトライン描画
/// </summary>
public class OutlineRendererFeature : ScriptableRendererFeature
{
    [SerializeField, Tooltip("アウトライン対象レイヤー")]
    private LayerMask _outlineLayer;

    [SerializeField, Tooltip("標準アウトラインカラー")]
    private Color _outlineColor = Color.cyan;

    [SerializeField, Tooltip("アウトラインの太さ")]
    [Range(0.0f, 20.0f)]
    private float _outlineWidth = 3.0f;

    private Material _maskMaterial;
    private Material _compositeMaterial;
    private OutlineRenderPass _outlineRenderPass;

    private Color _currentOutlineColor;

    public override void Create()
    {
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_compositeMaterial);

        Shader maskShader = Shader.Find("Hidden/Custom/SH_OutlineMask");
        Shader compositeShader = Shader.Find("Hidden/Custom/SH_OutlineComposite");

        _outlineRenderPass = null;

        if (maskShader == null || compositeShader == null)
        {
            Debug.LogError("アウトライン用シェーダーが見つかりません");
            return;
        }

        _maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);
        _compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);

        _currentOutlineColor = _outlineColor;

        _outlineRenderPass = new OutlineRenderPass(
            _maskMaterial,
            _compositeMaterial
        );

        _outlineRenderPass.renderPassEvent =
            RenderPassEvent.AfterRenderingTransparents;
    }

    // アウトラインカラーを変更
    public void SetOutlineColor(Color color)
    {
        _currentOutlineColor = color;
    }

    // アウトラインカラーを初期化
    public void ResetOutlineColor()
    {
        _currentOutlineColor = _outlineColor;
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (_outlineRenderPass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        _outlineRenderPass.Setup(
            _outlineLayer,
            _currentOutlineColor,
            _outlineWidth
        );

        renderer.EnqueuePass(_outlineRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_compositeMaterial);
    }

    /// <summary>
    /// アウトライン描画パス
    /// </summary>
    private class OutlineRenderPass : ScriptableRenderPass
    {
        private readonly Material _maskMaterial;
        private readonly Material _compositeMaterial;

        private LayerMask _layerMask;
        private Color _outlineColor;
        private float _outlineWidth;

        private static readonly int MaskTextureID =
            Shader.PropertyToID("_OutlineMaskTexture");

        private static readonly int OutlineColorID =
            Shader.PropertyToID("_OutlineColor");

        private static readonly int OutlineWidthID =
            Shader.PropertyToID("_OutlineWidth");

        private static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("UniversalGBuffer"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        private class MaskPassData
        {
            public RendererListHandle rendererList;
        }

        private class CompositePassData
        {
            public TextureHandle source;
            public Material material;
        }

        public OutlineRenderPass(
            Material maskMaterial,
            Material compositeMaterial)
        {
            _maskMaterial = maskMaterial;
            _compositeMaterial = compositeMaterial;
        }

        // 描画設定を更新
        public void Setup(
            LayerMask layerMask,
            Color outlineColor,
            float outlineWidth)
        {
            _layerMask = layerMask;
            _outlineColor = outlineColor;
            _outlineWidth = outlineWidth;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            UniversalResourceData resourceData =
                frameData.Get<UniversalResourceData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            UniversalRenderingData renderingData =
                frameData.Get<UniversalRenderingData>();

            UniversalLightData lightData =
                frameData.Get<UniversalLightData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            // アウトライン設定を反映
            _compositeMaterial.SetColor(
                OutlineColorID,
                _outlineColor
            );

            _compositeMaterial.SetFloat(
                OutlineWidthID,
                _outlineWidth
            );

            // マスク用テクスチャを生成
            RenderTextureDescriptor descriptor =
                cameraData.cameraTargetDescriptor;

            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

            TextureHandle maskTexture =
                UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    descriptor,
                    "OutlineMaskTexture",
                    false
                );

            // マスク描画
            using (var builder =
                renderGraph.AddRasterRenderPass<MaskPassData>(
                    "Outline Mask",
                    out var passData))
            {
                FilteringSettings filteringSettings =
                    new FilteringSettings(
                        RenderQueueRange.all,
                        _layerMask.value
                    );

                DrawingSettings drawingSettings =
                    RenderingUtils.CreateDrawingSettings(
                        ShaderTags,
                        renderingData,
                        cameraData,
                        lightData,
                        SortingCriteria.CommonOpaque
                    );

                drawingSettings.overrideMaterial = _maskMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;

                RendererListParams rendererListParams =
                    new RendererListParams(
                        renderingData.cullResults,
                        drawingSettings,
                        filteringSettings
                    );

                passData.rendererList =
                    renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(maskTexture, 0);

                builder.SetGlobalTextureAfterPass(
                    maskTexture,
                    MaskTextureID
                );

                builder.SetRenderFunc(
                    static (MaskPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(
                            false,
                            true,
                            Color.black
                        );

                        context.cmd.DrawRendererList(data.rendererList);
                    }
                );
            }

            // 合成先テクスチャを生成
            TextureHandle sourceTexture =
                resourceData.activeColorTexture;

            TextureDesc outputDesc =
                renderGraph.GetTextureDesc(sourceTexture);

            outputDesc.name = "OutlineCompositeTexture";
            outputDesc.clearBuffer = false;
            outputDesc.depthBufferBits = 0;

            TextureHandle outputTexture =
                renderGraph.CreateTexture(outputDesc);

            // アウトライン生成・合成
            using (var builder =
                renderGraph.AddRasterRenderPass<CompositePassData>(
                    "Outline Composite",
                    out var passData))
            {
                passData.source = sourceTexture;
                passData.material = _compositeMaterial;

                builder.UseTexture(
                    sourceTexture,
                    AccessFlags.Read
                );

                builder.UseGlobalTexture(
                    MaskTextureID,
                    AccessFlags.Read
                );

                builder.SetRenderAttachment(outputTexture, 0);

                builder.SetRenderFunc(
                    static (CompositePassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(
                            context.cmd,
                            data.source,
                            new Vector4(1, 1, 0, 0),
                            data.material,
                            0
                        );
                    }
                );
            }

            // 合成結果をカメラカラーに設定
            resourceData.cameraColor = outputTexture;
        }
    }
}
