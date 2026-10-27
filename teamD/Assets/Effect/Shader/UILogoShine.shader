Shader "UI/LogoShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineStrength ("Shine Strength", Range(0,2)) = 0.7
        _ShineWidth ("Shine Width", Range(0.005,0.2)) = 0.04
        _ShineTilt ("Shine Tilt", Range(-1,1)) = 0.25
        _ShinePosition ("Shine Position", Float) = -2

        // Unity UIのMaskで使う設定。
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
                float4 localPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _TextureSampleAdd;
            float4 _Color;
            float4 _ClipRect;

            float4 _ShineColor;
            float _ShineStrength;
            float _ShineWidth;
            float _ShineTilt;
            float _ShinePosition;

            v2f vert(appdata v)
            {
                v2f o;

                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.vertex);
                o.texcoord = v.texcoord;

                // ImageのColorやAnimationによる色変更を反映する。
                o.color = v.color * _Color;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 textureColor =
                    tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;

                float4 result = textureColor * i.color;

                // 文字ごとのUVではなく、画面全体の座標を使う。
                // 分かれたImageにも、同じ一本の光が通る。
                float2 screenUV =
                    i.screenPosition.xy / i.screenPosition.w;

                float diagonal =
                    screenUV.x + (screenUV.y - 0.5) * _ShineTilt;

                float distanceToLight =
                    abs(diagonal - _ShinePosition);

                float shine = 1.0 - smoothstep(
                    0.0,
                    max(_ShineWidth, 0.0001),
                    distanceToLight
                );

                // 黒い輪郭や暗い影には、光を弱くかける。
                float brightness = max(
                    result.r,
                    max(result.g, result.b)
                );

                float faceMask = smoothstep(0.05, 0.4, brightness);

                result.rgb +=
                    _ShineColor.rgb *
                    _ShineStrength *
                    shine *
                    faceMask;

                // 元画像の透明部分は透明なままにする。
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(
                    i.localPosition.xy,
                    _ClipRect
                );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }

            ENDCG
        }
    }
}
