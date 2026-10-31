Shader "FireEscape/SurfaceDecal"
{
 Properties { _MainTex("RGBA decal",2D)="white" {} _Tint("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent"}
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest LEqual
   Cull Off
   Offset -1,-1
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half3 shade:TEXCOORD1;float fog:TEXCOORD2;};
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.shade=max(SampleSH(TransformObjectToWorldNormal(a.normalOS)),half3(.08,.08,.08));o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*_Tint;c.rgb=MixFog(c.rgb*i.shade,i.fog);return c;}
   ENDHLSL
  }
 }
}
