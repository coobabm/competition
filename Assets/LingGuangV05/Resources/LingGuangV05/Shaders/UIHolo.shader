// Holographic foil for UI cards (the self-insight "闪卡"): a laser-rainbow band whose phase follows the card's
// tilt, a diagonal specular sweep and Voronoi star sparkles. Drawn additively over the card art; masked by the
// sprite's alpha so rounded corners stay clean. Works with RectMask2D and stencil masks like UI/Default.
Shader "LingGuang/UIHolo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Tilt ("Tilt (x, y)", Vector) = (0,0,0,0)
        _Strength ("Rainbow strength", Range(0,1)) = 0.55
        _Density ("Stripe density", Range(0.5,12)) = 3.2
        _Sparkle ("Sparkle", Range(0,1)) = 0.6
        _Finish ("Finish 0 gold 1 silver 2 pearl 3 rainbow", Range(0,3)) = 3

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex; fixed4 _Color; float4 _Tilt; float _Strength, _Density, _Sparkle, _Finish;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color;
                return o;
            }

            float3 Hue(float h) { h = frac(h) * 6; return saturate(float3(abs(h - 3) - 1, 2 - abs(h - 2), 2 - abs(h - 4))); }
            float2 Hash2(float2 p) { p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))); return frac(sin(p) * 43758.5453); }

            // Distance to the nearest Voronoi point and that point's random id.
            float2 Voronoi(float2 p)
            {
                float2 cell = floor(p), f = frac(p); float best = 8; float id = 0;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y), r = Hash2(cell + g); float d = length(g + r - f);
                    if (d < best) { best = d; id = dot(cell + g, float2(7.13, 3.71)); }
                }
                return float2(best, frac(sin(id) * 91.7));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).a * i.color.a;
                float2 t = _Tilt.xy;
                // Rainbow stripes: the phase moves with the tilt, so the colour runs across the card as it turns.
                float stripe = dot(i.uv, normalize(float2(1, .65))) * _Density + t.x * .9 + t.y * .55 + sin(i.uv.y * 9 + t.x * 3) * .04;
                float3 rainbow = Hue(stripe);
                float3 finish = _Finish < .5 ? lerp(float3(1, .78, .3), rainbow, .25)      // gold
                              : _Finish < 1.5 ? lerp(float3(.85, .9, 1), rainbow, .3)    // silver
                              : _Finish < 2.5 ? lerp(float3(1, .9, .95), rainbow, .55)   // pearl
                              : rainbow;                                                 // full laser
                // Strongest where the light catches: a soft band that follows the tilt.
                float band = exp(-pow((i.uv.x + i.uv.y) * .5 - (.5 + t.x * .45 - t.y * .25), 2) * 10);
                float3 col = finish * _Strength * (.35 + .9 * band);
                // A thin specular sweep.
                col += exp(-pow((i.uv.x - i.uv.y * .4) - (.3 + t.x * .7), 2) * 260) * .6;
                // Star sparkles that twinkle as the card turns.
                float2 v = Voronoi(i.uv * float2(26, 38));
                float twinkle = saturate(sin(v.y * 40 + t.x * 9 + t.y * 7 + _Time.y * 2) * .5 + .5);
                col += smoothstep(.09, .0, v.x) * twinkle * _Sparkle * (.4 + band);
                col *= mask;
                #ifdef UNITY_UI_CLIP_RECT
                col *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(mask - .001);
                #endif
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
