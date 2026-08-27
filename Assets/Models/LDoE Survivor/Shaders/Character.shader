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
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	//CustomEditor "Assets.ContentAddressable.OtherAssets.Shaders.Editor.ShaderCustomCharacterGUI"
}