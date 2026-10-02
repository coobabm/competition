Shader "LingGuang/PixelComposite"
{
    Properties
    {
        [PerRendererData] _MainTex ("World", 2D) = "black" {}
        _GlowStrength ("Soft glow", Range(0, 1)) = 0.48
        _WaterRefraction ("Water refraction in world units", Range(0, 0.05)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend One Zero
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _GlowStrength;
            float _WaterRefraction;
            float4 _Waves[8];
            float _WaveClock, _WaveGain;
            float4 _WaveGeometry, _WaterLobe0, _WaterLobe1;
            float4 _WaterViewOrigin, _WaterViewU, _WaterViewV;
            #include "WaterRipple.hlsl"
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            half3 bright(float2 uv)
            {
                half3 c = tex2D(_MainTex, uv).rgb;
                return max(c - 0.34h, 0.0h);
            }
            half4 frag(v2f i) : SV_Target
            {
                float2 sampleUV = i.uv;
                if (_WaterRefraction > 0)
                {
                    float2 world = _WaterViewOrigin.xy + i.uv.x * _WaterViewU.xy + i.uv.y * _WaterViewV.xy;
                    float mask = WaterMask(world);
                    if (mask > 0)
                    {
                        float height;
                        float2 slope;
                        WaterSurface(world, height, slope);
                        // Bound visual displacement to 0.032 world units; collider/grid
                        // coordinates stay unchanged, and overlay HUD is never sampled.
                        float2 offset = slope / (1 + length(slope)) * _WaterRefraction * mask;
                        sampleUV += float2(dot(offset, _WaterViewU.xy) / max(dot(_WaterViewU.xy, _WaterViewU.xy), 0.001),
                            dot(offset, _WaterViewV.xy) / max(dot(_WaterViewV.xy, _WaterViewV.xy), 0.001));
                    }
                }
                // Only the core is point sampled. Glow samples use continuously varying
                // native-screen UVs, so the halo does not turn into a blocky low-res blur.
                float2 texel = abs(_MainTex_TexelSize.xy);
                float2 coreUV = (floor(sampleUV / texel) + 0.5) * texel;
                half3 core = tex2D(_MainTex, coreUV).rgb;
                float2 d = texel * 1.6;
                half3 glow = bright(sampleUV) * 0.20h;
                glow += (bright(sampleUV + float2(d.x, 0)) + bright(sampleUV - float2(d.x, 0))
                       + bright(sampleUV + float2(0, d.y)) + bright(sampleUV - float2(0, d.y))) * 0.12h;
                glow += (bright(sampleUV + d) + bright(sampleUV - d)
                       + bright(sampleUV + float2(d.x, -d.y)) + bright(sampleUV + float2(-d.x, d.y))) * 0.08h;
                half3 color = core + glow * _GlowStrength;
                color = color / (1.0h + color * 0.32h);
                float2 p = i.uv * 2.0 - 1.0;
                color *= 1.0 - 0.12 * saturate(dot(p, p) * 0.5);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
