Shader "Penguin/Backdrop Depth"
{
    Properties
    {
        _BaseMap ("Original texture", 2D) = "white" {}
        _BaseColor ("Original tint", Color) = (1,1,1,1)
        _HazeColor ("Distance haze", Color) = (0.58,0.67,0.76,1)
        _Saturation ("Background saturation", Range(0,1)) = 0.18
        _HazeStart ("Haze start", Float) = 20
        _HazeEnd ("Haze end", Float) = 135
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _HazeColor;
                float _Saturation, _HazeStart, _HazeEnd;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float depth:TEXCOORD2; };
            V Vert(A input)
            {
                V o; float3 world=TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(world);o.normalWS=TransformObjectToWorldNormal(input.normalOS);
                o.depth=-TransformWorldToView(world).z;o.uv=TRANSFORM_TEX(input.uv,_BaseMap);return o;
            }
            half4 Frag(V i):SV_Target
            {
                float3 original=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                float grey=dot(original,float3(0.2126,0.7152,0.0722));
                float3 muted=lerp(grey.xxx,original,_Saturation);
                float shade=0.84+0.16*saturate(dot(normalize(i.normalWS),normalize(float3(-0.4,0.8,-0.5))));
                float haze=lerp(0.48,0.9,smoothstep(_HazeStart,_HazeEnd,i.depth));
                return half4(lerp(muted*shade,_HazeColor.rgb,haze),1);
            }
            ENDHLSL
        }
    }
}
