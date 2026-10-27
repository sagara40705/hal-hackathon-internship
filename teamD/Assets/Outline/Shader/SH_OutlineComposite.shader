Shader "Hidden/Custom/SH_OutlineComposite"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "OutlineComposite"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_OutlineMaskTexture);
            SAMPLER(sampler_OutlineMaskTexture);

            float4 _OutlineColor;
            float _OutlineWidth;

            // マスクをサンプリング
            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _OutlineMaskTexture,
                    sampler_OutlineMaskTexture,
                    uv
                ).r;
            }

            // アウトラインを生成
            float CalculateOutline(float2 uv)
            {
                float center = SampleMask(uv);
                float2 texelSize = rcp(_ScreenParams.xy);
                float2 offset = texelSize * _OutlineWidth;

                float expanded = center;

                // 8方向からマスクを取得
                expanded = max(expanded, SampleMask(uv + float2( offset.x, 0.0)));
                expanded = max(expanded, SampleMask(uv + float2(-offset.x, 0.0)));
                expanded = max(expanded, SampleMask(uv + float2(0.0,  offset.y)));
                expanded = max(expanded, SampleMask(uv + float2(0.0, -offset.y)));

                expanded = max(expanded, SampleMask(uv + float2( offset.x,  offset.y)));
                expanded = max(expanded, SampleMask(uv + float2(-offset.x,  offset.y)));
                expanded = max(expanded, SampleMask(uv + float2( offset.x, -offset.y)));
                expanded = max(expanded, SampleMask(uv + float2(-offset.x, -offset.y)));

                return saturate(expanded - center);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // 元のシーンカラーを取得
                half4 sceneColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                // アウトラインを計算
                float outline = CalculateOutline(uv);

                // アウトラインを合成
                float alpha = outline * _OutlineColor.a;
                sceneColor.rgb = lerp(
                    sceneColor.rgb,
                    _OutlineColor.rgb,
                    alpha
                );

                return sceneColor;
            }

            ENDHLSL
        }
    }
}
