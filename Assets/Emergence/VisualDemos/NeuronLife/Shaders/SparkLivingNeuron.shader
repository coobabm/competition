Shader "Spark/VisualDemos/LivingNeuron"
{
 Properties
 {
  [MainTexture] _MainTex("Texture",2D)="white"{}
  _Tint("Tint",Color)=(1,1,1,1)
  _Phase("Phase",Float)=0
  _Clock("Clock",Float)=0
  _PulseAge("Pulse Age",Float)=99
  _Sway("Sway",Float)=1
  _SomaUV("Soma UV",Vector)=(0.452,0.557,0,0)
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags {"LightMode"="SRPDefaultUnlit"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _MainTex_ST, _Tint, _SomaUV;
   float _Phase, _Clock, _PulseAge, _Sway;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
   V vert(A i)
   {
    V o;
    float2 d=i.uv-_SomaUV.xy;
    float weight=saturate(length(d)*2.3);
    float t=_Clock*0.85+_Phase;
    i.positionOS.x+=sin(t+d.y*7.0)*weight*weight*0.016*_Sway;
    i.positionOS.y+=cos(t*0.79+d.x*6.0)*weight*weight*0.012*_Sway;
    o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
    o.uv=i.uv; o.color=i.color*_Tint;
    return o;
   }
   half4 frag(V i):SV_Target
   {
    half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
    float radius=length(i.uv-_SomaUV.xy);
    float ring=exp(-pow((radius-_PulseAge*1.1)*30.0,2.0))*step(_PulseAge,0.8)*_Sway;
    c.rgb*=1.0+ring*2.7;
    c.rgb+=half3(0.34,0.22,0.04)*ring*c.a;
    return c;
   }
   ENDHLSL
  }
 }
}
