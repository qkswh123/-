Shader "Penguin/Level1 Volume Fog"
{
    Properties
    {
        _NoiseTex ("Cloud noise (3D)", 3D) = "white" {}
        _FogColor ("Fog color", Color) = (0.78, 0.89, 0.96, 1)
        _Density ("Density", Float) = 0.035
        _Wind ("Wind XYZ / seed", Vector) = (0.015, 0, 0.004, 0)
        _FlowTime ("Animation time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        Pass
        {
            Name "VolumeFog"
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE3D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float4 _Wind;
                float _Density;
                float _FlowTime;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float Cloud(float3 p)
            {
                float3 uvw = p * float3(1.35, 0.65, 1.1)
                    - _Wind.xyz * _FlowTime + _Wind.w;
                float large = SAMPLE_TEXTURE3D_LOD(_NoiseTex, sampler_NoiseTex, uvw, 0).r;
                float detail = SAMPLE_TEXTURE3D_LOD(_NoiseTex, sampler_NoiseTex, uvw * 2.17 + 0.31, 0).r;
                float noise = large * 0.78 + detail * 0.22;
                float body = smoothstep(0.32, 0.66, noise);
                // All six sides fade away, concealing the volume's box boundaries.
                float3 edge = saturate((0.5 - abs(p)) * float3(10, 5, 7));
                edge = edge * edge * (3 - 2 * edge);
                return body * edge.x * edge.y * edge.z;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 ray = normalize(input.positionWS - _WorldSpaceCameraPos);
                if (unity_OrthoParams.w > 0.5) ray = -UNITY_MATRIX_V[2].xyz;
                float3 origin = TransformWorldToObject(input.positionWS);
                float3 direction = mul((float3x3)unity_WorldToObject, ray);
                float3 safeDirection = float3(
                    direction.x >= 0 ? max(direction.x, 0.000001) : min(direction.x, -0.000001),
                    direction.y >= 0 ? max(direction.y, 0.000001) : min(direction.y, -0.000001),
                    direction.z >= 0 ? max(direction.z, 0.000001) : min(direction.z, -0.000001));
                float3 a = (-0.5 - origin) / safeDirection;
                float3 b = (0.5 - origin) / safeDirection;
                float3 entry = min(a, b), exitPoint = max(a, b);
                float start = max(0, max(entry.x, max(entry.y, entry.z)));
                float end = min(exitPoint.x, min(exitPoint.y, exitPoint.z));
                if (end <= start) return 0;

                const int steps = 24;
                float stepLength = (end - start) / steps;
                float transmission = 1;
                float3 color = 0;
                [loop] for (int i = 0; i < steps; i++)
                {
                    float3 p = origin + direction * (start + (i + 0.5) * stepLength);
                    float density = Cloud(p);
                    // A second density sample approximates light travelling through the cloud.
                    float shade = Cloud(p + float3(0.03, 0.12, -0.06));
                    float light = lerp(1.12, 0.72, shade);
                    float opacity = 1 - exp(-density * _Density * stepLength);
                    color += transmission * opacity * _FogColor.rgb * light;
                    transmission *= 1 - opacity;
                    if (transmission < 0.02) break;
                }
                return half4(color, 1 - transmission);
            }
            ENDHLSL
        }
    }
}
