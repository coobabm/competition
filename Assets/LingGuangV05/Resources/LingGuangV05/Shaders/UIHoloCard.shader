// Holographic collectible card for UI, ported from holo-card-studio (MIT, https://github.com/EverettFish/holo-card-studio):
// its holographic route (the pink → yellow → blue → white spectrum, the noisy diagonal wave that follows the viewing
// angle, the overlay-blended foil, the sharp specular sweep, the twinkling Voronoi-edge stars, the rainbow card edge)
// and its lenticular route (two artworks blended by the viewing angle through lens strips, ridge glints, the
// luminance dip mid-flip, dot-and-cross particles).
//
// The finish rises with the card's rarity (_Finish):
//   0 普通 silver      — a cool silver sheen sweeps across; no stars, no rim.
//   1 稀有 gold        — warm gold foil, a few gold sparkles, a gold edge.
//   2 史诗 holographic — the full holographic route: laser rainbow, twinkling stars, a soft rainbow edge.
//   3 传说 cosmos      — a starfield and nebula sunk deep behind the art, dot-and-cross particles, a bright rainbow edge.
//   4 隐藏 lenticular  — the lenticular route: tilting the card flips the art to its second state, strip by strip,
//                        with lens glints; the spectrum slowly cycles (prism).
//
// The web version stacks its layers in one shader; in UI the layers are separate graphics, so there are three modes:
//   _Mode 0 · art    — the recessed background, shifted back by the tilt (parallax).  Blend One OneMinusSrcAlpha
//   _Mode 1 · tint   — the foil over the subject glyph, as a 2× multiply (≈ overlay). Blend DstColor SrcColor
//   _Mode 2 · shine  — sweep, stars, particles and the edge, added on top.           Blend One One
// Drawn on a plain quad (RawImage, uv 0–1) so the effects span the whole card; the rounded corners come from
// _Size (width, height, corner radius in pixels) instead of a sliced sprite, whose uvs would bunch up at the borders.
// Works with RectMask2D and stencil masks like UI/Default.
Shader "LingGuang/UIHoloCard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Mode ("Mode 0 art 1 tint 2 shine", Float) = 0
        _Finish ("Finish 0 silver 1 gold 2 holo 3 cosmos 4 lenticular", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 10
        _Tilt ("Tilt (x, y)", Vector) = (0,0,0,0)
        _Foil ("Foil strength", Range(0,1)) = 0.65
        _BgDepth ("Background depth", Range(-1,1)) = -0.25
        _ColorA ("Theme light", Color) = (0.35,0.45,0.95,1)
        _ColorB ("Theme dark", Color) = (0.05,0.07,0.2,1)
        _ColorC ("Lenticular second light", Color) = (1,0.55,0.3,1)
        _ColorD ("Lenticular second dark", Color) = (0.2,0.04,0.08,1)
        _Rim ("Edge", Range(0,1)) = 0
        _Stars ("Star density", Range(10,140)) = 64
        _Size ("Size px (w, h, radius)", Vector) = (380,540,14,0)

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
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex; fixed4 _Color; float4 _Tilt, _ColorA, _ColorB, _ColorC, _ColorD, _Size;
            float _Mode, _Finish, _Foil, _BgDepth, _Rim, _Stars;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color;
                return o;
            }

            // ── holo-card-studio's shared functions, ported from GLSL ──
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }
            // The holographic ramp, recoloured by the finish: silver and gold are metals, the rest are rainbows.
            float3 spectrum(float t)
            {
                if (_Finish < .5) { float s = .72 + .28 * sin(t * 6.283); return float3(s * .94, s * .97, s); }
                if (_Finish < 1.5) return lerp(float3(.85, .55, .16), float3(1, .93, .68), .5 + .5 * sin(t * 6.283));
                if (_Finish > 3.5) t = frac(t + _Time.y * .08); // the prism: colours keep turning
                float3 pink = float3(1, .32, .62), yellow = float3(1, .85, .32), blue = float3(.22, .62, 1);
                t = frac(t);
                if (t < .35) return lerp(pink, yellow, t / .35);
                if (t < .7) return lerp(yellow, blue, (t - .35) / .35);
                return lerp(blue, float3(1, 1, 1), (t - .7) / .3);
            }
            float3 overlayBlend(float3 b, float3 f) { return lerp(2 * b * f, 1 - 2 * (1 - b) * (1 - f), step(float3(.5, .5, .5), b)); }
            // The view direction of the web version is the card's tilt here (z = 1).
            float2 view() { return _Tilt.xy; }
            float2 parallax(float2 p, float s, float d) { return (p - .5) * s + .5 + view() * d * .14; }
            float wave(float2 p) { float2 a = p + view() * 2.4; return .5 + .5 * sin((a.x * .848 - a.y * .530) * 6.283 * .55 + 7 * noise(a * 1.5)); }
            float star(float2 p, float density, float sparse)
            {
                float2 q = p * density, id = floor(q), f = frac(q); float first = 9, second = 9;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y); float2 o = float2(hash(id + g), hash(id + g + 43.3)); float d = length(g + o - f);
                    if (d < first) { second = first; first = d; } else second = min(second, d);
                }
                float edge = 1 - smoothstep(.01, .035, second - first);
                float twinkle = pow(.5 + .5 * sin(_Time.y * 1.8 + hash(id) * 30 + view().x * 27 + view().y * 21), 6);
                return edge * step(sparse, hash(id + 8.8)) * twinkle;
            }
            // The lenticular route's particles: a dot with a cross, blinking with the angle.
            float particle(float2 uv, float density)
            {
                float2 q = uv * density, id = floor(q), f = frac(q) - .5; float seed = hash(id);
                float dotMask = 1 - smoothstep(.04, .17, length(f));
                float crossMask = (1 - smoothstep(.015, .05, min(abs(f.x), abs(f.y)))) * (1 - smoothstep(.13, .28, max(abs(f.x), abs(f.y))));
                float blink = pow(.5 + .5 * sin(_Time.y * 1.7 + seed * 28 + view().x * 26), 8);
                return max(dotMask, crossMask * .6) * step(.96, seed) * blink;
            }
            float3 foilColor(float2 uv) { return spectrum(wave(uv) * .8 + noise(uv * 5) * .12); }

            // ── lenticular: the flip phase, strip by strip (lenticular-model.md §1–3) ──
            static const float Pitch = 60, FlipAngle = 14, Soft = .22, Stripe = .35, Sweep = 6;
            float lensC(float2 uv) { return frac(uv.x * Pitch) - .5; }
            float flip(float2 uv)
            {
                float angle = view().x * 20 + (uv.x - .5) * Sweep; // the card turns up to 20° after the pointer
                float t = min(abs(angle) / FlipAngle, 1) + lensC(uv) * Stripe * .5;
                return smoothstep(.5 - Soft, .5 + Soft, t);
            }

            // A web of neurons for the background: Voronoi cell edges as threads, cell points as nodes.
            float3 web(float2 bu, float3 light, float3 dark)
            {
                float3 col = lerp(dark, light, saturate(bu.y * .85 + noise(bu * 3) * .25));
                float2 q = bu * 7, id = floor(q), f = frac(q); float first = 9, second = 9;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y); float2 o = float2(hash(id + g), hash(id + g + 17.1)); float d = length(g + o - f);
                    if (d < first) { second = first; first = d; } else second = min(second, d);
                }
                col += (1 - smoothstep(.02, .07, second - first)) * .12;
                col += smoothstep(.12, .05, first) * .28;
                float rings = .5 + .5 * sin(length((bu - float2(.5, .62)) * float2(1, 1.3)) * 46);
                return col + pow(rings, 8) * .05;
            }

            float3 art(float2 uv)
            {
                float2 bu = parallax(uv, 1, _BgDepth);
                float3 col;
                if (_Finish > 3.5)
                {
                    // Two artworks; the lens refracts a little inside each strip; a dip where both half show.
                    float m = flip(uv);
                    float2 lu = bu + float2(lensC(uv) * .3 / Pitch, 0);
                    col = lerp(web(lu, _ColorA.rgb, _ColorB.rgb), web(lu.yx * float2(1, -1) + float2(0, 1), _ColorC.rgb, _ColorD.rgb), m);
                    col *= 1 - (1 - abs(2 * m - 1)) * .06;
                }
                else col = web(bu, _ColorA.rgb, _ColorB.rgb);
                if (_Finish > 2.5 && _Finish < 3.5)
                {
                    // Cosmos: a nebula and a field of far stars, sunk much deeper than the web.
                    float2 deep = parallax(uv, 1.1, _BgDepth * 3);
                    float neb = noise(deep * 2.3) * noise(deep * 4.7 + 3.1);
                    col += spectrum(noise(deep * 1.7) + .2) * neb * .35;
                    float2 sq = deep * 90, sid = floor(sq);
                    float s = step(.985, hash(sid)) * (1 - smoothstep(.05, .25, length(frac(sq) - .5)));
                    col += s * (.5 + .5 * sin(_Time.y * 2 + hash(sid) * 40));
                }
                float3 foil = foilColor(uv);
                return lerp(col, overlayBlend(col, foil), _Foil * .36);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Rounded rectangle: distance inside the edge in pixels (negative outside).
                float2 px = i.uv * _Size.xy, hs = _Size.xy * .5, r = min(_Size.z, min(hs.x, hs.y));
                float2 q = abs(px - hs) - (hs - r);
                float inside = -(length(max(q, 0)) + min(max(q.x, q.y), 0) - r);
                float mask = saturate(inside + .5) * tex2D(_MainTex, i.uv).a * i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                mask *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(mask - .001);
                #endif
                float2 uv = i.uv;
                if (_Mode < .5)
                {
                    float3 c = art(uv) * i.color.rgb;
                    return fixed4(c * mask, mask);
                }
                float3 foil = foilColor(uv);
                float2 v = view();
                if (_Mode < 1.5)
                {
                    // 2× multiply: .5 leaves the subject as it is; the foil darkens and lifts it like an overlay.
                    float3 tint = lerp(float3(.5, .5, .5), foil * .85 + .08, _Foil * .5);
                    if (_Finish > 3.5)
                    {
                        // Lens strips over the subject: faint ridges, darkest mid-flip.
                        float m = flip(uv), ridge = 1 - 4 * lensC(uv) * lensC(uv);
                        tint *= lerp(1, .82 + .18 * ridge, (1 - abs(2 * m - 1)) * .9 + .1);
                    }
                    return fixed4(lerp(float3(.5, .5, .5), tint, mask), 1);
                }
                // Shine: the sweep for every finish, then what the rarity adds.
                float sweep = pow(max(0, sin((uv.x * .83 + uv.y * .35 + v.x * 1.8 + v.y * .9) * 6.283)), 12);
                float3 c = foil * sweep * _Foil * (_Finish < .5 ? .35 : .5);
                float2 su = uv * float2(1, _Size.y / max(_Size.x, 1));
                if (_Finish > .5 && _Finish < 1.5) c += float3(1, .82, .45) * star(su, _Stars * .6, .95) * _Foil;
                if (_Finish > 1.5) c += float3(.66, .86, 1) * star(su, _Stars, .90) * _Foil;
                if (_Finish > 2.5) c += float3(1.4, 1.2, .8) * particle(su, _Stars * 1.4) * _Foil;
                if (_Finish > 3.5)
                {
                    // Lens glints: each ridge catches the light at its own angle (lenticular-model.md §5).
                    float3 n = normalize(float3(lensC(uv) * 2 * .5, 0, 1)), V = normalize(float3(v.x * .5, v.y * .5, 1));
                    float spec = pow(max(dot(n, normalize(V + normalize(float3(.35, .6, 1)))), 0), 28);
                    c += spec * spectrum(uv.y + v.x) * .12;
                }
                // The edge: gold for gold, a rainbow from epic up, running faster for legends.
                float rim = (1 - smoothstep(0, 9, inside)) * _Rim;
                float3 edge = _Finish < 1.5 ? spectrum(wave(uv)) : spectrum(wave(uv) + (_Finish > 2.5 ? _Time.y * .15 : 0));
                c += edge * rim * (.45 + _Foil * .4);
                return fixed4(c * mask, 1);
            }
            ENDCG
        }
    }
}
