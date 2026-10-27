Shader "Custom/SH_ParticleGlitch"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        [MainColor] _MainColor("Main Color", Color) = (1, 1, 1, 1)

        [Header(Glitch)]
        _GlitchStrength("Glitch Strength", Range(0, 1)) = 0.6
        _GlitchSpeed("Glitch Speed", Float) = 15.0
        _GlitchChance("Glitch Chance", Range(0, 1)) = 0.35

        [Header(Position)]
        _GlitchOffset("Glitch Offset", Range(0, 0.2)) = 0.04

        [Header(Stretch)]
        _StretchChance("Stretch Chance", Range(0, 1)) = 0.25
        _StretchStrength("Stretch Strength", Range(0, 4)) = 2.0

        [Header(Flicker)]
        _FlickerChance("Flicker Chance", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ParticleGlitch"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

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

                float _GlitchStrength;
                float _GlitchSpeed;
                float _GlitchChance;

                float _GlitchOffset;

                float _StretchChance;
                float _StretchStrength;

                float _FlickerChance;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;

                // xy : UV
                // z  : StableRandom.x
                float3 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float flicker : TEXCOORD1;
            };

            // 疑似乱数を生成する
            float Random(float2 seed)
            {
                return frac(sin(dot(
                    seed,
                    float2(12.9898, 78.233)
                )) * 43758.5453);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float4 positionCS = TransformObjectToHClip(input.positionOS);

                // パーティクルごとの固定乱数を取得
                float seed = input.uv.z;

                // 一定間隔でグリッチを更新
                float timeStep = floor(
                    _Time.y * max(_GlitchSpeed, 0.001)
                );

                // 発生判定と移動方向を独立して計算
                float randomChance = Random(
                    float2(seed * 127.1, timeStep * 311.7)
                );

                float randomDirection = Random(
                    float2(seed * 269.5, timeStep * 183.3)
                );

                float randomDistance = Random(
                    float2(seed * 419.2, timeStep * 97.1)
                );

                // グリッチを発生させるか判定
                float glitchMask = step(
                    1.0 - _GlitchChance,
                    randomChance
                );

                // X+とX-の方向を決める
                float direction = randomDirection < 0.5
                    ? -1.0
                    : 1.0;

                // グリッチの移動量を計算
                float offset = direction
                    * randomDistance
                    * glitchMask
                    * _GlitchOffset
                    * _GlitchStrength;

                // スクリーン水平方向に移動
                positionCS.x += offset * positionCS.w;

                // 引き伸ばしの発生判定
                float randomStretch = Random(
                    float2(seed * 573.3, timeStep * 191.9)
                );

                float stretchMask = step(
                    1.0 - _StretchChance,
                    randomStretch
                );

                // パーティクルの中心に対して左右に引き伸ばす
                float stretch = 1.0
                    + stretchMask
                    * _StretchStrength
                    * _GlitchStrength;

                // Quadの中心からの相対位置
                float localX = input.uv.x - 0.5;

                // 横方向のスケールを変更
                positionCS.x += localX
                    * (stretch - 1.0)
                    * 0.03
                    * positionCS.w;

                // 点滅の発生判定
                float randomFlicker = Random(
                    float2(seed * 739.7, timeStep * 223.1)
                );

                float flickerMask = step(
                    1.0 - _FlickerChance,
                    randomFlicker
                );

                output.flicker = 1.0
                    - flickerMask * _GlitchStrength;

                output.positionCS = positionCS;
                output.color = input.color;
                output.uv = input.uv.xy;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // パーティクルのテクスチャをサンプリング
                float2 uv = input.uv
                    * _MainTex_ST.xy
                    + _MainTex_ST.zw;

                half4 color = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                );

                // マテリアルカラーと頂点カラーを適用
                color *= _MainColor * input.color;

                // グリッチによる点滅
                color.a *= input.flicker;

                return color;
            }

            ENDHLSL
        }
    }
}
