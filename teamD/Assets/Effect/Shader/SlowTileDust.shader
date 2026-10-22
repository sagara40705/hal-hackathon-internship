Shader "TeamD/Effects/Slow Tile Dust"
{
    Properties { _Tint ("Particle Tint", Color) = (0.7,0.95,1,0.65) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END
            struct A {float4 position:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            V Vert(A v)
            {
                V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color*_Tint;return o;
            }
            half4 Frag(V i):SV_Target
            {
                float r=length(i.uv*2-1);
                half alpha=1-smoothstep(.25,1,r);
                return half4(i.color.rgb,i.color.a*alpha);
            }
            ENDHLSL
        }
    }
}
