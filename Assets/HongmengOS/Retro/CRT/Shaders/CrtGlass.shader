Shader "HongmengOS/Retro/CRT Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Desktop render texture", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Focus ("Focus: soft to sharp", Range(0,1)) = 0.52
        _Brightness ("Brightness", Range(0,2)) = 1
        _Curvature ("Convex barrel curvature", Range(0,0.25)) = 0.10
        _Bloom ("Phosphor bloom", Range(0,0.6)) = 0.18
        _Halation ("Glass halation", Range(0,0.4)) = 0.10
        _Chromatic ("Edge colour separation in pixels", Range(0,2)) = 0.48
        _ScanLines ("Scanline count", Float) = 410
        _ScanStrength ("Scanline density", Range(0,0.25)) = 0.095
        _Phosphor ("Phosphor aperture grille", Range(0,0.12)) = 0.035
        _Vignette ("Glass edge absorption", Range(0,0.65)) = 0.30
        _Reflection ("Glass reflection", Range(0,0.25)) = 0.095
        _Power ("Power", Range(0,1)) = 1
        _Flash ("Single power transition white pulse", Range(0,1)) = 0
        _Collapse ("Vertical raster height", Range(0,1)) = 1
        _Aspect ("Physical screen aspect", Float) = 1.3333333
        [Toggle] _FlipY ("Flip source vertically", Float) = 0
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
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "CRTGlass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float4 _ClipRect;
            float _Focus, _Brightness, _Curvature, _Bloom, _Halation, _Chromatic;
            float _ScanLines, _ScanStrength, _Phosphor, _Vignette, _Reflection;
            float _Power, _Flash, _Collapse, _Aspect, _FlipY;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            // CPU pointer relay must use the identical mapping, with bottom-left UVs.
            // q = 2*displayUV-1; q.y /= max(_Collapse,.001);
            // sourceUV = .5 + .5 * (q.x*(1+k*q.y*q.y), q.y*(1+k*q.x*q.x));
            // sourceUV.y = 1-sourceUV.y only when _FlipY > .5.
            float2 SourceUV(float2 displayUV)
            {
                float2 q = displayUV * 2.0 - 1.0;
                q.y /= max(_Collapse, 0.001);
                float2 source = float2(q.x * (1.0 + _Curvature * q.y * q.y),
                    q.y * (1.0 + _Curvature * q.x * q.x)) * 0.5 + 0.5;
                if (_FlipY > 0.5) source.y = 1.0 - source.y;
                return source;
            }

            float3 SoftRaster(float2 uv, float2 pixel)
            {
                float2 d = pixel * lerp(2.10, 0.42, saturate(_Focus));
                float3 color = tex2D(_MainTex, uv).rgb * 0.25;
                color += tex2D(_MainTex, uv + float2(d.x, 0)).rgb * 0.125;
                color += tex2D(_MainTex, uv - float2(d.x, 0)).rgb * 0.125;
                color += tex2D(_MainTex, uv + float2(0, d.y)).rgb * 0.125;
                color += tex2D(_MainTex, uv - float2(0, d.y)).rgb * 0.125;
                color += tex2D(_MainTex, uv + d).rgb * 0.0625;
                color += tex2D(_MainTex, uv - d).rgb * 0.0625;
                color += tex2D(_MainTex, uv + float2(d.x, -d.y)).rgb * 0.0625;
                color += tex2D(_MainTex, uv + float2(-d.x, d.y)).rgb * 0.0625;
                return color;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 source = SourceUV(uv);
                float2 pixel = abs(_MainTex_TexelSize.xy);
                float2 q = uv * 2.0 - 1.0;
                float2 aq = abs(q);
                // Rounded glass uses physical aspect rather than stretched UV radii.
                float aspect = max(_Aspect, 0.1);
                float2 physical = (uv - 0.5) * float2(aspect, 1.0);
                float radius = 0.055;
                float2 corner = abs(physical) - (float2(aspect,1.0) * 0.5 - radius);
                float signedDistance = length(max(corner,0.0)) + min(max(corner.x,corner.y),0.0) - radius;
                float glassMask = 1.0 - smoothstep(-0.002, 0.002, signedDistance);
                float rasterMask = smoothstep(0.0, 0.007, source.x) * smoothstep(0.0, 0.007, source.y)
                    * smoothstep(0.0, 0.007, 1.0-source.x) * smoothstep(0.0, 0.007, 1.0-source.y);

                float3 raster = SoftRaster(saturate(source), pixel);
                float edgeRadius = saturate(dot(q,q) * 0.5);
                float2 colourOffset = pixel * q * _Chromatic * (0.35 + edgeRadius);
                raster.r = lerp(raster.r, tex2D(_MainTex, saturate(source + colourOffset)).r, 0.25);
                raster.b = lerp(raster.b, tex2D(_MainTex, saturate(source - colourOffset)).b, 0.25);

                float2 bloomStep = pixel * lerp(5.2, 2.8, saturate(_Focus));
                float3 halo = tex2D(_MainTex, saturate(source + float2(bloomStep.x,0))).rgb;
                halo += tex2D(_MainTex, saturate(source - float2(bloomStep.x,0))).rgb;
                halo += tex2D(_MainTex, saturate(source + float2(0,bloomStep.y))).rgb;
                halo += tex2D(_MainTex, saturate(source - float2(0,bloomStep.y))).rgb;
                halo *= 0.25;
                float3 brightHalo = max(halo - 0.40, 0.0);
                raster += brightHalo * _Bloom;
                raster += halo * _Halation * float3(0.86,0.98,0.82) * 0.35;

                // Fade modulation before Nyquist while retaining its mean absorption.
                // This also suppresses moire as the raster contracts during power-off.
                float scanCyclesPerPixel = fwidth(source.y) * abs(_ScanLines);
                float scanContrast = 1.0 - smoothstep(0.35, 0.50, scanCyclesPerPixel);
                float scan = (0.5 + 0.5*cos(source.y * _ScanLines * 6.2831853) * scanContrast) * _ScanStrength;
                float grille = (0.5 + 0.5*cos(source.x * _MainTex_TexelSize.z * 2.0943951)) * _Phosphor;
                float vignette = pow(saturate(max(aq.x,aq.y)), 4.5) * _Vignette;
                raster *= (1.0-scan) * (1.0-grille) * (1.0-vignette);
                raster *= _Brightness * float3(0.96,1.0,0.945);

                float3 glassBase = float3(0.006,0.011,0.010);
                float3 resultColor = glassBase + max(raster,0.0) * rasterMask * saturate(_Power);
                // Convex glass has a restrained upper-right softbox and a curved perimeter halo.
                float diagonal = uv.x * 0.60 + uv.y;
                float reflection = smoothstep(1.00,1.32,diagonal) * (1.0-smoothstep(1.44,1.69,diagonal));
                reflection *= _Reflection * (0.18 + 0.30 * pow(saturate(uv.y),2.0));
                // Soft reflected window panes make the glass visible without
                // bending the desktop more or washing out its entire face.
                float2 reflectedWindow = uv - float2(0.81,0.78);
                reflectedWindow.x += reflectedWindow.y * 0.18;
                float windowShape = (1.0-smoothstep(0.074,0.105,abs(reflectedWindow.x)))
                    * (1.0-smoothstep(0.12,0.20,abs(reflectedWindow.y)));
                float windowDivider = smoothstep(0.006,0.016,abs(reflectedWindow.x))
                    * smoothstep(0.008,0.020,abs(reflectedWindow.y+0.015));
                reflection += windowShape * windowDivider * _Reflection * 0.45;
                float rim = pow(saturate(1.0 - abs(length(q * float2(0.84,1.0)) - 1.01) * 18.0), 2.0);
                float crown = pow(saturate(uv.y), 26.0) * 0.022;
                resultColor += float3(0.55,0.66,0.63) * (reflection + crown + rim * 0.020);
                // Controller drives this once per power transition; the shader has no time oscillator.
                float collapsedBeam = exp(-abs(q.y) * 300.0) * pow(1.0-saturate(_Collapse),5.0) * saturate(_Power) * 0.75;
                resultColor += float3(1,1,0.94) * collapsedBeam;
                resultColor = lerp(resultColor, float3(1,1,0.97), saturate(_Flash));
                fixed4 result = fixed4(resultColor, glassMask) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
