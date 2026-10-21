Shader "Penguin/Level Sky"
{
    Properties
    {
        _Top ("Upper sky", Color) = (0.08,0.35,0.7,1)
        _Horizon ("Horizon", Color) = (0.6,0.85,0.95,1)
        _Bottom ("Lower sky", Color) = (0.2,0.45,0.6,1)
        _SunColor ("Sun / moon", Color) = (1,0.9,0.65,1)
        _Stars ("Stars", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" }
        Pass
        {
            Cull Front ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Top, _Horizon, _Bottom, _SunColor;
                float _Stars;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            V Vert(A v) { V o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.direction=v.positionOS.xyz; return o; }
            half4 Frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);
                float h=saturate(d.y*1.35+0.25);
                float3 col=lerp(_Horizon.rgb,_Top.rgb,smoothstep(0,1,h));
                col=lerp(col,_Bottom.rgb,saturate(-d.y*1.7));
                float sun=dot(d,normalize(float3(-0.45,0.43,0.8)));
                col+=_SunColor.rgb*(smoothstep(0.997,0.999,sun)*0.8+pow(saturate(sun),45)*0.12);
                float2 starUV=float2(atan2(d.x,d.z),asin(d.y))*120;
                float2 cell=floor(starUV);
                float random=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
                float star=(1-smoothstep(0.02,0.13,length(frac(starUV)-0.5)))*step(0.982,random);
                col+=star*_Stars*float3(0.7,0.85,1);
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
