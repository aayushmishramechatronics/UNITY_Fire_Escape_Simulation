Shader "FireEscape/SmokeAndFire"
{
    Properties
    {
        _MainTex("RGBA sprite", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _DstBlend("Destination blend", Float) = 10
        _Softness("Depth intersection fade", Float) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _DstBlend;
                float _Softness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float eye:TEXCOORD1; float fog:TEXCOORD2; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS; o.eye=-p.positionVS.z;
                o.color=v.color*_Tint; o.uv=v.uv; o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
                float2 screenUV=GetNormalizedScreenSpaceUV(i.positionCS);
                float depth=LinearEyeDepth(SampleSceneDepth(screenUV),_ZBufferParams);
                c.a*=saturate((depth-i.eye)/max(_Softness,.001));
                c.a*=saturate((i.eye-.12)/.38);
                c.rgb=MixFog(c.rgb,i.fog);
                return c;
            }
            ENDHLSL
        }
    }
}
