Shader "TeamD/Effects/Golden Goal Particles"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (1, 0.68, 0.12, 1)
        _Intensity ("Intensity", Range(0, 8)) = 2
        [Enum(Ring,0,SquareFrame,1,Diamond,2,Star,3,Streak,4,Halo,5)] _Shape ("Shape", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "GoldenGoalUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Intensity;
                float _Shape;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Tint;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = abs(input.uv * 2 - 1);
                float r = length(p);
                float a;
                if (_Shape < 0.5)
                {
                    float d = abs(r - 0.81);
                    a = (1 - smoothstep(0.015, 0.043, d)) + 0.22 * exp(-d * 25);
                }
                else if (_Shape < 1.5)
                {
                    float d = abs(max(p.x, p.y) - 0.86);
                    a = (1 - smoothstep(0.018, 0.055, d)) + 0.2 * exp(-d * 22);
                }
                else if (_Shape < 2.5)
                {
                    float d = p.x + p.y;
                    a = 1 - smoothstep(0.61, 0.74, d);
                    a += 0.14 * exp(-d * 3.5);
                }
                else if (_Shape < 3.5)
                {
                    float d = sqrt(p.x) + sqrt(p.y);
                    a = 1 - smoothstep(0.91, 1.04, d);
                    a += 0.12 * exp(-r * 5);
                }
                else if (_Shape < 4.5)
                    a = exp(-p.x * p.x * 28) * pow(saturate(1 - p.y), 0.65);
                else
                    a = exp(-r * r * 4) * (1 - smoothstep(0.75, 1, r));
            
                a *= 1 - smoothstep(0.94, 1, max(p.x, p.y));
                half alpha = saturate(a) * input.color.a;
                return half4(input.color.rgb * _Intensity * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
