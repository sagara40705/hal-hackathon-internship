
Shader "Custom/SH_EnemyBurst"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        [MainColor] _MainColor("Main Color", Color) = (1, 1, 1, 1)

        [Header(Burst)]
        _Burst("Burst", Range(0, 1)) = 0
        _BurstDistance("Burst Distance", Range(0, 3)) = 0.8
        _HorizontalStretch("Horizontal Stretch", Range(1, 5)) = 1.5
        _Center("Burst Center", Vector) = (0, 0, 0, 0)

        [Header(Fragment)]
        _FragmentScale("Fragment Scale", Float) = 12
        _FragmentNoise("Fragment Noise", Range(0, 1)) = 0.5

        [Header(Flash)]
        [HDR] _FlashColor("Flash Color", Color) = (3, 0.25, 0.1, 1)
        _FlashStrength("Flash Strength", Range(0, 5)) = 2

        [Header(Dissolve)]
        _DissolveStart("Dissolve Start", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "EnemyBurst"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainColor;
                float4 _FlashColor;
                float4 _Center;

                float _Burst;
                float _BurstDistance;
                float _HorizontalStretch;
                float _FragmentScale;
                float _FragmentNoise;
                float _FlashStrength;
                float _DissolveStart;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);

                return frac((p.x + p.y) * p.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float progress = saturate(_Burst);

                // 頂点ごとに異なる方向を計算
                float3 direction = input.positionOS - _Center.xyz;

                float3 randomDirection = float3(
                    Hash(input.positionOS + 11.3) * 2.0 - 1.0,
                    Hash(input.positionOS + 37.7) * 2.0 - 1.0,
                    Hash(input.positionOS + 81.9) * 2.0 - 1.0
                );

                direction += randomDirection * _FragmentNoise;
                direction /= max(length(direction), 0.0001);

                // 横方向に広がりやすくする
                direction.x *= _HorizontalStretch;

                // 破裂の進行に合わせて移動量を増やす
                float burstT = progress * progress;
                float3 offset = direction * burstT * _BurstDistance;

                float3 positionOS = input.positionOS + offset;

                output.positionCS = TransformObjectToHClip(positionOS);
                output.positionOS = input.positionOS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float progress = saturate(_Burst);

                // オブジェクト空間を小さなセルに分割
                float3 cell = floor(
                    input.positionOS * max(_FragmentScale, 0.001)
                );

                float random = Hash(cell);

                // 破裂後半でセル単位の消滅を進める
                float dissolve = saturate(
                    (progress - _DissolveStart)
                    / max(1.0 - _DissolveStart, 0.0001)
                );

                // 最初は全表示、最後は全消滅
                clip(random - dissolve);

                // 通常のテクスチャカラー
                half4 color = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv
                );

                color *= _MainColor;

                // 破裂開始時に強く光らせる
                float flash = saturate(1.0 - abs(progress - 0.15) / 0.15);

                // 消え際のセル境界を発光させる
                float edge = 1.0 - smoothstep(
                    0.0,
                    0.12,
                    random - dissolve
                );

                float emission = max(
                    flash * _FlashStrength,
                    edge * progress
                );

                color.rgb += _FlashColor.rgb * emission;

                return color;
            }

            ENDHLSL
        }
    }
}
