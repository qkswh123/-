Shader "Penguin/Platform Outline"
{
    Properties
    {
        _OutlineColor ("Edge color", Color) = (0.04,0.09,0.16,1)
        _OutlineWidth ("Edge width", Float) = 0.035
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry-1" }
        Pass
        {
            Cull Front
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            float4 Vert(A input):SV_POSITION
            {
                return TransformObjectToHClip(input.positionOS.xyz + input.normalOS * _OutlineWidth);
            }
            half4 Frag():SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
