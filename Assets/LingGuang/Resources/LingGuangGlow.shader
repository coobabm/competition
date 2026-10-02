// Unlit glow for sprites / lines / meshes. HDR _Color drives URP Bloom.
Shader "LingGuang/Glow"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _Monochrome ("Recolor source art", Range(0,1)) = 0
        _PixelGrid ("Local pixel grid (0 disables)", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Monochrome;
                float _PixelGrid;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = _PixelGrid > 0 ? (floor(i.uv * _PixelGrid) + 0.5) / _PixelGrid : i.uv;
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half luminance = max(t.r, max(t.g, t.b));
                t.rgb = lerp(t.rgb, luminance.xxx, _Monochrome);
                half4 c = t * i.color * _Color;
                return c;
            }
            ENDHLSL
        }
    }
}
