Shader "LDoE/TerrainDecal" {
	Properties {
		_MainTex ("MainTex", 2D) = "white" {}
		_Mask ("Mask Noise", 2D) = "black" {}
		_Smooth1 ("Smooth 1", Float) = 0
		_Smooth2 ("Smooth 2", Float) = 0.3
		[Toggle(USE_UV_TILING)] _UVTileEnable ("Use UV for Texture", Float) = 0
		[Toggle(USE_UV_MASK)] _UVMaskTileEnable ("Use UV for Mask", Float) = 0
		_Mask2 ("Mask2 ", 2D) = "white" {}
		_Offset ("Offset", Range(-1, 0)) = 0
		_Offset2 ("Offset2", Range(0, 1)) = 0
		_Transparenty ("Offset2", Vector) = (0.5,0.9,0.6,0.7)
		[Toggle(USE_BCHANNEL_TO_MIX)] _UseBChannelToMix ("Use B channel to Mix Wet", Float) = 0
		_LayerWet ("LayerWet", 2D) = "white" {}
		_Smooth3 ("Smooth Wet All", Float) = 0
		_Smooth4 ("Smooth Wet All2", Float) = 0.3
		_Smooth5 ("Smooth Wet Mask", Float) = 0
		_Smooth6 ("Smooth Wet Mask2", Float) = 0.3
		_Color ("Tint Color", Vector) = (1,1,1,1)
		[HideInInspector] _ZValue ("__zw", Float) = 0
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
	//CustomEditor "Assets.ContentAddressable.OtherAssets.Shaders.Editor.ShaderCustomTerrainDecalGUI"
}