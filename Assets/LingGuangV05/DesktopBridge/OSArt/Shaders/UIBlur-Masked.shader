// LingGuang desktop frosted-glass blur whose strength follows the alpha of the UI sprite (_MainTex), so rounded or
// feathered panels blur only inside their shape. Written in-house.
//
// Built-in pipeline: grab, horizontal blur, grab, vertical blur; each blur is a 9-tap gaussian done with 5 bilinear fetches
// (10 texture reads per pixel in total). Scriptable pipelines cannot GrabPass, so they get a cheap translucent veil whose
// strength follows _Size and the sprite alpha.
Shader "LingGuang/OS/UI Blur Masked"
{
    Properties
    {
        _Size ("Blur", Range(0, 30)) = 1
        [HideInInspector] _MainTex ("Tint Color (RGB)", 2D) = "white" {}

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

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
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float _Size;

    v2f_blur vertBlur(appdata_blur v)
    {
        v2f_blur o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.grab = ComputeGrabScreenPos(o.pos);
        o.uvMain = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
    }

    // 9-tap gaussian expressed as 5 bilinear fetches (centre + 2 pairs).
    #define BLUR_SAMPLE(tex, uv, stride) ( \
        tex2D(tex, (uv)) * 0.2270270 + \
        tex2D(tex, (uv) + (stride) * 1.3846154) * 0.3162162 + \
        tex2D(tex, (uv) - (stride) * 1.3846154) * 0.3162162 + \
        tex2D(tex, (uv) + (stride) * 3.2307692) * 0.0702703 + \
        tex2D(tex, (uv) - (stride) * 3.2307692) * 0.0702703 )
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
                fixed4 mask = tex2D(_MainTex, i.uvMain);
                fixed strength = saturate(_Size / 6.0) * 0.4;
                return fixed4(0.96, 0.98, 1.0, strength * mask.a);
            }
            ENDCG
        }
    }

    SubShader
    {
        // Built-in pipeline.
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        GrabPass { "_HBlur" }

        Pass
        {
            Name "BlurHorizontal"
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

            sampler2D _HBlur;
            float4 _HBlur_TexelSize;

            half4 fragH(v2f_blur i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uvMain).a;
                float2 uv = i.grab.xy / i.grab.w;
                float2 stride = float2(_HBlur_TexelSize.x * _Size * mask, 0);
                return BLUR_SAMPLE(_HBlur, uv, stride);
            }
            ENDCG
        }

        GrabPass { "_VBlur" }

        Pass
        {
            Name "BlurVertical"
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

            sampler2D _VBlur;
            float4 _VBlur_TexelSize;

            half4 fragV(v2f_blur i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uvMain).a;
                float2 uv = i.grab.xy / i.grab.w;
                float2 stride = float2(0, _VBlur_TexelSize.y * _Size * mask);
                return BLUR_SAMPLE(_VBlur, uv, stride);
            }
            ENDCG
        }
    }

    Fallback Off
}
