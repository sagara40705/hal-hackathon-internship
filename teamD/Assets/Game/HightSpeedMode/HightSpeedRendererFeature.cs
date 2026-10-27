using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// Overlay UIより前にカメラ画像だけを加工するRender Graphパス。
/// </summary>
public class HightSpeedRendererFeature : ScriptableRendererFeature
{
    // 参照を保存してビルド時のShader strippingを防ぐ。
    [SerializeField, Tooltip("高速化用のシェーダー")]
    private Shader _shader;

    private Material _material;
    private SpeedPass _pass;

    public override void Create()
    {
        CoreUtils.Destroy(_material);
        _pass = null;
        if (_shader == null) return;
        _material = CoreUtils.CreateEngineMaterial(_shader);
        _pass = new SpeedPass(_material)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
            requiresIntermediateTexture = true
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var mode = HighSpeedController.Active;
        var camera = renderingData.cameraData.camera;
        if (_pass == null || mode == null || !mode.IsSpeedModified || mode.EffectParameters.x <= 0f
            || camera.cameraType != CameraType.Game || !camera.CompareTag("MainCamera")) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(_material);

    private sealed class SpeedPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private static readonly int ParametersId = Shader.PropertyToID("_SpeedParameters");
        private static readonly int PhaseId = Shader.PropertyToID("_SpeedPhase");
        private class PassData
        {
            public TextureHandle source;
            public Material material;
            public Vector4 parameters;
            public float phase;
        }

        public SpeedPass(Material material) { _material = material; }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var mode = HighSpeedController.Active;
            if (resources.isActiveTargetBackBuffer || mode == null || !mode.IsSpeedModified) return;
            var source = resources.activeColorTexture;
            var descriptor = graph.GetTextureDesc(source);
            descriptor.name = "HightSpeedColor";
            descriptor.clearBuffer = false;
            descriptor.depthBufferBits = 0;
            var destination = graph.CreateTexture(descriptor);
            using (var builder = graph.AddRasterRenderPass<PassData>("Hight Speed Chromatic Horizontal Blur", out var data))
            {
                data.source = source;
                data.material = _material;
                data.parameters = mode.EffectParameters;
                data.phase = Time.unscaledTime;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                {
                    d.material.SetVector(ParametersId, d.parameters);
                    d.material.SetFloat(PhaseId, d.phase);
                    Blitter.BlitTexture(context.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                });
            }
            resources.cameraColor = destination;
        }
    }
}
