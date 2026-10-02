Shader "LingGuang/ConductA01"
{
    Properties
    {
        _Geometry ("0 body, 1 filaments", Float) = 0
        _Signal ("gain fire flicker visibility", Vector) = (1,0,0,1)
        _Scan ("position line afterglow reduced", Vector) = (-.246,0,0,1)
        _Flow ("position intensity", Vector) = (0,0,0,0)
        _Tint ("A01 cold cyan", Color) = (.353,.863,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float3 direction:TEXCOORD1; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                float _Geometry;
                float4 _Signal, _Scan, _Flow, _Tint;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv; o.color = v.color;
                if (_Geometry > .5)
                {
                    float4 other = TransformObjectToHClip(v.positionOS.xyz + v.direction);
                    float2 tangent = (other.xy / other.w - o.positionCS.xy / o.positionCS.w) * _ScreenParams.xy;
                    tangent *= rsqrt(max(dot(tangent,tangent), 1e-8));
                    float2 normal = float2(-tangent.y,tangent.x);
                    // Subpixel antialiasing at native size; minimum readable coverage in the pixel-world buffer.
                    float halfWidth = max(.48, v.uv.y * .5) + .45;
                    o.positionCS.xy += normal * v.uv.x * halfWidth * 2 / _ScreenParams.xy * o.positionCS.w;
                }
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float3 cold = lerp(float3(28,64,84)/255, _Tint.rgb, .37);
                float3 hi = float3(240,251,255)/255;
                float gain = _Signal.x + _Signal.y;
                if (_Geometry > .5)
                {
                    float aa = 1-smoothstep(.35,1,abs(i.uv.x));
                    float3 filament = lerp(cold, hi, saturate(i.color.r + _Signal.z * .5));
                    return float4(filament * i.color.a * aa * gain, 0);
                }
                float2 p = (i.uv-.5)*2.4 + float2(.2,0);
                float pixel = max(length(fwidth(p)), .003);
                // Asymmetric rhombus, exactly the source polygon: tip .39, rear -.246, half width .126.
                float span = p.x >= 0 ? .39 : .246;
                float q = abs(p.x)/span + abs(p.y)/.126;
                float mask = 1-smoothstep(1-fwidth(q), 1+fwidth(q), q);
                float3 light = _Tint.rgb * exp(-dot(p,p)/.022) * .125 * gain;
                float core = exp(-dot(p,p)/.0011);
                light += hi * core * (.8+_Signal.z) * gain;
                float dx = _Scan.x-p.x;
                float phosphor = saturate(1-dx/.1924) * step(0,dx) * _Scan.z * .22;
                float scanline = (1-smoothstep(pixel*.12,pixel*.8,abs(dx))) * _Scan.y * .85;
                light += (hi*scanline + _Tint.rgb*phosphor)*mask*gain;
                float2 fd=p-_Flow.xy;
                light += hi*exp(-dot(fd,fd)/max(.0004,pixel*pixel*.22))*_Flow.z*.6*gain;
                float radius=length(p);
                float ring=(1-smoothstep(pixel*.15,pixel*.8,abs(radius-.51)))*.18;
                light += _Tint.rgb*ring*gain;
                // The outer threshold marker remains in world-up, provided by the view's existing charge UI.
                float alpha=mask*_Signal.w*.96;
                float3 fill=lerp(float3(4,10,16)/255,cold,.16);
                return float4(fill*alpha+light,alpha);
            }
            ENDHLSL
        }
    }
}
