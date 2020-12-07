// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "PhysWater/WaterShader"
{
	Properties
	{
		_Color("Color", Color) = (0, 0, 1, 1)
		_AlphaShallow("Alpha Shallow", Range(0, 1)) = 0.1
		_AlphaDeep("Alpha Deep", Range(0, 1)) = 0.9
		_MaxDepthAlpha("Max Depth Alpha", Float) = 1

		_MainTex("Texture", 2D) = "white"{}
		_Metallic("Metallic", Float) = 0
		_Smoothness("Smoothness", Float) = 0
		_NoiseTex("Noise", 2D) = "black"{}

		_DepthGradientShallow ("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)
		_DepthGradientDeep ("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)
		_DepthMaxDistance ("Depth Max Distance", Float) = 1
    }
    SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
		}
		Pass
		{
			zWrite off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD2;
			};

			float4 _Color;
			float _Metallic;
			float _Smoothness;
			float4 waveParams; // frequency, amplitude, inverseLength.x, inverseLength.y

			float _AlphaShallow;
			float _AlphaDeep;
			float _MaxDepthAlpha;

			sampler2D _CameraDepthTexture;

			float4 getNormalAndHeight(float2 coord) {
				waveParams = float4(0.9, 1.2, 0.1, 0.1);
				float p = waveParams.x * _Time.y + coord.x * waveParams.z + coord.y * waveParams.w;
				float4 h;

				//normal
				//h.x = cos(p) * waveParams.z * waveParams.y;
				//h.y = cos(p) * waveParams.w * waveParams.y;
				//h.z = -1;
				//h = h / length(h.xyz);

				//height
				h.w = sin(p) * waveParams.y;
				//Add noise
				//
				return h;
			}

			v2f vert(appdata v)
			{
				v2f o;

				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float4 surfData = getNormalAndHeight(worldPos.xz);

				worldPos.y = surfData.w;
				v.vertex = mul(unity_WorldToObject, worldPos);

				o.vertex = UnityObjectToClipPos(v.vertex);

				o.screenPos = ComputeScreenPos(o.vertex);

				return o;
			}

			float4 frag(v2f IN) : SV_Target
			{
				float depth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)).r;
				depth = LinearEyeDepth(depth);
				depth = depth - IN.screenPos.w;
				depth = saturate(depth / _MaxDepthAlpha);

				float4 color = _Color;

				color.a = lerp(_AlphaShallow, _AlphaDeep, depth);

				return color;
			}

			ENDCG
		}
	}
}
