Shader "Hidden/HightSpeed/ChromaticHorizontalBlur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _SpeedParameters; // strength, RGB offset px, blur px, line opacity
            float _SpeedPhase;

            half4 ReadColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 original = ReadColor(uv);
                // ブラーと横線は中央を保護。色収差だけ中央にも薄く残す。
                float radius = length((uv - 0.5) / float2(0.43, 0.44));
                float edgeMask = smoothstep(0.72, 1.28, radius);
                float mask = edgeMask * _SpeedParameters.x;
                if (_SpeedParameters.x < 0.001) return original;
                float2 pixel = float2(_BlitTexture_TexelSize.x, 0);
                // 中央は周辺設定の12%、最大1.5pxに制限し、輪郭の判読性を保つ。
                float centerSplit = min(_SpeedParameters.y * 0.12, 1.5);
                float2 split = pixel * lerp(centerSplit, _SpeedParameters.y, edgeMask) * _SpeedParameters.x;
                half3 blurred = 0;
                [unroll] for (int i = -2; i <= 2; i++)
                {
                    float2 p = uv + pixel * (i * 0.5 * _SpeedParameters.z * mask);
                    half weight = (3.0 - abs(i)) / 9.0;
                    blurred += half3(ReadColor(p + split).r, ReadColor(p).g, ReadColor(p - split).b) * weight;
                }
                // 横方向の光跡には、引き続き中央保護マスクを適用する。
                float row = floor(uv.y * 60);
                float seed = frac(sin(row * 127.1) * 43758.5453);
                float lineMask = pow(saturate(1 - abs(frac(uv.y * 60) - 0.5) * 2), 6);
                float travel = frac(uv.x * 1.8 + _SpeedPhase * (0.45 + seed) + seed * 7);
                float streak = pow(saturate(1 - abs(travel - 0.5) * 2), 3) * step(0.65, seed);
                blurred += half3(0.5, 0.85, 1) * lineMask * streak * mask * _SpeedParameters.w;
                return half4(blurred, original.a);
            }
            ENDHLSL
        }
    }
}

