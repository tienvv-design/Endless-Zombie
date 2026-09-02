Shader "LDoE/Character" {
	Properties {
		_Color ("Tint Color", Vector) = (1,1,1,1)
		_MaskTex ("Mask", 2D) = "white" {}
		_MainTex ("Main Tex", 2D) = "white" {}
		_EmitTex ("Emission Texture", 2D) = "white" {}
		_EmitValue ("Emission Value", Range(0, 1)) = 0
		_EdgeColor ("XRay Edge Color", Vector) = (0,0,0,0)
		_RimColor ("Rim Color", Vector) = (0,0,0,0)
		_LightIntensity ("LightPower", Range(0, 2)) = 0.5
		_AlphaR ("Alpha Texture", 2D) = "white" {}
		_Dissolve ("Dissolve", Range(0, 1)) = 0
		_EmissionTex ("Emission Tex", 2D) = "black" {}
		_GradienTex ("Gradient Tex", 2D) = "black" {}
		_EmissionAdd ("Emission Add", 2D) = "black" {}
		_Intencity ("Glow Intencity", Range(0, 1)) = 0
		_IntencityEmAdd ("Emm Add Intencity", Range(0, 1)) = 0
		_EmitSpeed ("Emission Speed", Range(0, 10)) = 1
		_AOMask ("Specular Mask", 2D) = "white" {}
		_Mask ("Mask Noise", 2D) = "black" {}
		_DirtTex ("Dirt Tex", 2D) = "white" {}
		_Dirtiness ("Dirtiness", Range(0, 2)) = 0
		_DF ("Falloff", Range(0, 1)) = 0.5
		_IconTex ("Icon", 2D) = "black" {}
		_ColorR ("Color R Channel", Vector) = (1,1,1,1)
		_ColorG ("Color G Channel", Vector) = (1,1,1,1)
		_ColorB ("Color B Channel", Vector) = (1,1,1,1)
		_CharacterMask ("Character Mask", 2D) = "black" {}
		_MaskColorR ("Mask Color R", Vector) = (1,1,1,1)
		_MaskColorG ("Mask Color G", Vector) = (1,1,1,1)
		_MaskTextureB ("Mask Texture B", 2D) = "black" {}
		_Cube ("Cubemap", Cube) = "" {}
		_SpecTex ("Specular Texture", 2D) = "white" {}
		_SpecPower ("Specular Power", Range(0, 1)) = 1
		[HideInInspector] _Shininess ("Shininess", Range(0.5, 50)) = 8
		[HideInInspector] _Mode ("__mode", Float) = 0
		[HideInInspector] _SrcBlend ("__src", Float) = 1
		[HideInInspector] _DstBlend ("__dst", Float) = 0
		[HideInInspector] _ZWrite ("__zw", Float) = 1
		[HideInInspector] _QueueShift ("Offset", Float) = 0
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		LOD 200

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _Color;
		CBUFFER_END

		TEXTURE2D(_MainTex);
		SAMPLER(sampler_MainTex);

		struct Attributes {
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};
		ENDHLSL

		Pass {
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma vertex ForwardVertex
			#pragma fragment ForwardFragment
			#pragma multi_compile_instancing

			Varyings ForwardVertex(Attributes input) {
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}

			half4 ForwardFragment(Varyings input) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(input);
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
			}
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull Back

			HLSLPROGRAM
			#pragma vertex ShadowVertex
			#pragma fragment ShadowFragment
			#pragma multi_compile_instancing

			Varyings ShadowVertex(Attributes input) {
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half4 ShadowFragment(Varyings input) : SV_Target {
				return 0;
			}
			ENDHLSL
		}
	}
	//CustomEditor "Assets.ContentAddressable.OtherAssets.Shaders.Editor.ShaderCustomCharacterGUI"
}
