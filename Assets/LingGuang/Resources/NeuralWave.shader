Shader "LingGuang/NeuralWave"
{
    Properties
    {
        _WaveGain ("Water lighting", Float) = 0.68
        _WaveGeometry ("Lifetime / speed / frequency / packet width", Vector) = (4.2, 2.6, 9, 1.35)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Waves[8];
                float _WaveClock;
                float _WaveGain;
                float4 _WaveGeometry;
                float4 _WaterLobe0, _WaterLobe1;
            CBUFFER_END
            #include "WaterRipple.hlsl"
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.world = TransformObjectToWorld(v.positionOS.xyz).xy;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float height;
                float2 slope;
                WaterSurface(i.world, height, slope);
                float mask = WaterMask(i.world);
                slope *= mask;
                height *= mask;
                // A continuous surface: highlights on one face, shadows on the other.
                // No dot grid, random particles, spiral scan, or opaque expanding disc.
                float3 normal = normalize(float3(-slope * 1.35, 1));
                float3 light = normalize(float3(-0.38, 0.58, 1));
                float facing = dot(normal, light);
                float flat = light.z;
                float relief = facing - flat;
                float specular = max(0, pow(saturate(facing), 26) - pow(flat, 26));
                float crest = saturate(height * 3.2);
                float strength = _WaveGain;
                half3 base = half3(0.010, 0.014, 0.034);
                half3 color = base * (1 + clamp(relief * 1.6, -0.65, 0.35) * strength);
                color += half3(0.15, 0.39, 0.52) * max(0, relief) * strength;
                color += half3(0.34, 0.71, 0.86) * specular * strength * 0.65;
                color += half3(0.06, 0.16, 0.23) * crest * strength;
                return half4(color, 0.96);
            }
            ENDHLSL
        }
    }
}
