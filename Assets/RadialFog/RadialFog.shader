Shader "Hidden/RPG/RadialFog"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Radial Fog"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _RadialFogColor;
            float3 _RadialFogCenter;
            float _RadialFogClearRadius;
            float _RadialFogDensity;
            float _RadialFogMaxOpacity;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float rawDepth = SampleSceneDepth(uv);

                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.00001) return sceneColor;
                #else
                    if (rawDepth >= 0.99999) return sceneColor;
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float radialDistance = max(distance(worldPosition, _RadialFogCenter) - _RadialFogClearRadius, 0.0);
                float fogAmount = saturate(1.0 - exp(-_RadialFogDensity * radialDistance)) * _RadialFogMaxOpacity;
                return half4(lerp(sceneColor.rgb, _RadialFogColor.rgb, fogAmount), sceneColor.a);
            }
            ENDHLSL
        }
    }
}
