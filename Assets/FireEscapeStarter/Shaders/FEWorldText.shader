Shader "FireEscape/WorldTextDepth"
{
    Properties { _MainTex("Font atlas",2D)="white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float fog:TEXCOORD1; };
            V vert(A v) { V o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; o.color=v.color; o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag(V i):SV_Target { half4 c=i.color; c.a*=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a; c.rgb=MixFog(c.rgb,i.fog); return c; }
            ENDHLSL
        }
    }
}
