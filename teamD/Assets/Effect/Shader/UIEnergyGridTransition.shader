Shader "UI/EnergyGridTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        _DarkColor ("Dark Color", Color) = (0.015, 0.02, 0.04, 1)
        _LightColor ("Light Color", Color) = (0.04, 0.06, 0.1, 1)
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.65, 0.15, 1)

        _Columns ("Columns", Range(4, 40)) = 16
        _EdgeWidth ("Edge Width", Range(0.002, 0.08)) = 0.015
        _GlowWidth ("Glow Width", Range(0.005, 0.15)) = 0.045
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.6

        [HideInInspector] _Progress ("Progress", Range(0, 1)) = 0
        [HideInInspector] _Opening ("Opening", Float) = 0
        [HideInInspector] _Aspect ("Aspect", Float) = 1.77778
        [HideInInspector] _SpreadTime ("Spread Time", Float) = 0.45
        [HideInInspector] _TileTime ("Tile Time", Float) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _DarkColor;
            float4 _LightColor;
            float4 _EdgeColor;

            float _Columns;
            float _EdgeWidth;
            float _GlowWidth;
            float _GlowStrength;

            float _Progress;
            float _Opening;
            float _Aspect;
            float _SpreadTime;
            float _TileTime;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 画面比率に合わせ、各マスを正方形にする。
                float columns = max(4.0, floor(_Columns + 0.5));
                float2 gridSize = float2(
                    columns,
                    columns / max(_Aspect, 0.01)
                );

                // 画面中央を原点としたグリッド座標。
                float2 gridPosition = (i.uv - 0.5) * gridSize;

                // 各マスの中心位置。
                float2 cellCenter = floor(gridPosition) + 0.5;

                // 各マス内の座標。中央が0、端が約±0.5。
                float2 cellUV = frac(gridPosition) - 0.5;

                // 中央から遠いマスほど開始を遅らせる。
                float maxDistance =
                    length(gridSize * 0.5) + 0.7072;

                float distanceRatio =
                    saturate(length(cellCenter) / maxDistance);

                float tileTime = max(_TileTime, 0.01);
                float spreadTime = max(_SpreadTime, 0.0);
                float totalTime = spreadTime + tileTime;

                float localProgress = saturate(
                    (_Progress * totalTime -
                     distanceRatio * spreadTime) / tileTime
                );

                // 拡大・縮小の動きを滑らかにする。
                float eased =
                    localProgress * localProgress *
                    (3.0 - 2.0 * localProgress);

                float size = _Opening > 0.5
                    ? 1.0 - eased
                    : eased;

                // 内部は、下側が暗く上側が少し明るい色。
                float shade = saturate(cellUV.y + 0.5);
                float3 bodyColor = lerp(
                    _DarkColor.rgb,
                    _LightColor.rgb,
                    shade
                );

                // 最初と最後は確実に透明／不透明にする。
                bool fullyHidden =
                    (_Opening < 0.5 && _Progress <= 0.0) ||
                    (_Opening > 0.5 && _Progress >= 1.0);

                if (fullyHidden)
                    return float4(0, 0, 0, 0);

                bool fullyCovered =
                    (_Opening < 0.5 && _Progress >= 1.0) ||
                    (_Opening > 0.5 && _Progress <= 0.0);

                if (fullyCovered)
                    return float4(bodyColor * i.color.rgb, i.color.a);

                // 四角の中心から、縁までの距離。
                float squareDistance =
                    max(abs(cellUV.x), abs(cellUV.y));

                float halfSize = size * 0.505;

                // 輪郭のギザギザを軽減する。
                float aa = max(fwidth(squareDistance), 0.0001);

                float fill = 1.0 - smoothstep(
                    halfSize - aa,
                    halfSize + aa,
                    squareDistance
                );

                if (size <= 0.0001)
                    fill = 0.0;

                // 拡大・縮小の途中だけ光を強くする。
                float activity =
                    4.0 * localProgress * (1.0 - localProgress);

                float edgeDistance =
                    abs(squareDistance - halfSize);

                float edge = (
                    1.0 - smoothstep(
                        _EdgeWidth,
                        _EdgeWidth + aa,
                        edgeDistance
                    )
                ) * activity;

                // 縁から離れるほど弱くなる光のにじみ。
                float halo = exp(
                    -edgeDistance / max(_GlowWidth, 0.001)
                );

                halo *= activity * _GlowStrength;

                float lightAmount = saturate(edge + halo);

                float3 color = lerp(
                    bodyColor,
                    _EdgeColor.rgb,
                    lightAmount
                );

                // 四角の外側にも薄い光を表示する。
                float alpha = saturate(fill + edge + halo);

                return float4(
                    color * i.color.rgb,
                    alpha * i.color.a
                );
            }

            ENDHLSL
        }
    }
}
