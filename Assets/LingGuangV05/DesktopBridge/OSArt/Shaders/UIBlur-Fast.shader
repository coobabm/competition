// LingGuang desktop frosted-glass blur (Aero style), written in-house.
//
// Built-in pipeline: two passes. The screen behind the element is grabbed, blurred horizontally and written back, grabbed
// again, then blurred vertically and tinted in the second pass. Each blur is a 9-tap gaussian done with 5 bilinear fetches,
// so the whole effect costs 10 texture reads per pixel (+1 for the optional distortion).
// Scriptable pipelines cannot GrabPass, so they get a cheap translucent frosted veil instead; its strength follows _Size,
// which means UIBlur's fade in/out animation still reads correctly.
Shader "LingGuang/OS/UI Blur Fast"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _BumpAmt ("Distortion", Range (0,128)) = 10
        [HideInInspector] _MainTex ("Tint Color (RGB)", 2D) = "white" {}
        [HideInInspector] _BumpMap ("Normalmap", 2D) = "bump" {}
        _Size ("Size", Range(0, 20)) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    // Shared by every pass: UI mask support.
    CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata_blur
    {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f_blur
    {
        float4 pos : SV_POSITION;
        float4 grab : TEXCOORD0;
        float2 uvMain : TEXCOORD1;
        float2 uvBump : TEXCOORD2;
    };

    sampler2D _GrabTexture;
    float4 _GrabTexture_TexelSize;
    sampler2D _MainTex;
    float4 _MainTex_ST;
    sampler2D _BumpMap;
    float4 _BumpMap_ST;
    fixed4 _Color;
    float _Size;
    float _BumpAmt;

    v2f_blur vertBlur(appdata_blur v)
    {
        v2f_blur o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.grab = ComputeGrabScreenPos(o.pos);
        o.uvMain = TRANSFORM_TEX(v.texcoord, _MainTex);
        o.uvBump = TRANSFORM_TEX(v.texcoord, _BumpMap);
        return o;
    }

    // 9-tap gaussian expressed as 5 bilinear fetches (centre + 2 pairs).
    half4 gaussian(float2 uv, float2 stride)
    {
        half4 c = tex2D(_GrabTexture, uv) * 0.2270270;
        c += tex2D(_GrabTexture, uv + stride * 1.3846154) * 0.3162162;
        c += tex2D(_GrabTexture, uv - stride * 1.3846154) * 0.3162162;
        c += tex2D(_GrabTexture, uv + stride * 3.2307692) * 0.0702703;
        c += tex2D(_GrabTexture, uv - stride * 3.2307692) * 0.0702703;
        return c;
    }
    ENDCG

    SubShader
    {
        // Scriptable pipelines: no grab available, draw a frosted veil.
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Pass
        {
            Name "FrostVeil"
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vertBlur
            #pragma fragment fragVeil

            fixed4 fragVeil(v2f_blur i) : SV_Target
            {
                fixed4 tint = tex2D(_MainTex, i.uvMain);
                fixed strength = saturate(_Size / 6.0) * 0.4;
                return fixed4(_Color.rgb * tint.rgb, strength * tint.a);
            }
            ENDCG
        }
    }

    SubShader
    {
        // Built-in pipeline: grab, blur, grab, blur + tint.
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        GrabPass { Tags { "LightMode" = "Always" } }

        Pass
        {
            Name "BlurHorizontal"
            Tags { "LightMode" = "Always" }
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            CGPROGRAM
            #pragma vertex vertBlur
            #pragma fragment fragH

            half4 fragH(v2f_blur i) : SV_Target
            {
                float2 uv = i.grab.xy / i.grab.w;
                return gaussian(uv, float2(_GrabTexture_TexelSize.x * _Size, 0));
            }
            ENDCG
        }

        GrabPass { Tags { "LightMode" = "Always" } }

        Pass
        {
            Name "BlurVerticalTint"
            Tags { "LightMode" = "Always" }
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            CGPROGRAM
            #pragma vertex vertBlur
            #pragma fragment fragV

            half4 fragV(v2f_blur i) : SV_Target
            {
                float2 uv = i.grab.xy / i.grab.w;
                if (_BumpAmt > 0.0)
                {
                    // Optional glass refraction: shift the lookup along the normal map.
                    half2 bump = UnpackNormal(tex2Dlod(_BumpMap, float4(i.uvBump, 0, 0))).rg;
                    uv += bump * _BumpAmt * _GrabTexture_TexelSize.xy;
                }
                half4 col = gaussian(uv, float2(0, _GrabTexture_TexelSize.y * _Size));
                half4 tint = tex2D(_MainTex, i.uvMain) * _Color;
                col.rgb *= tint.rgb;
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
