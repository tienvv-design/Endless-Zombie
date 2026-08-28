Shader "Endless Zombie/VFX/LDoE Particle Alpha Blended"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _AlphaR ("Alpha (Red Channel)", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _ColorStrength ("Color Strength", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "ParticleAlpha"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaR);
            SAMPLER(sampler_AlphaR);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                half _ColorStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 mainSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alphaMask = SAMPLE_TEXTURE2D(_AlphaR, sampler_AlphaR, input.uv).r;
                half4 output = mainSample * _TintColor * input.color;
                output.rgb *= _ColorStrength;
                output.a = min(mainSample.a, alphaMask) * _TintColor.a * input.color.a;
                return output;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
