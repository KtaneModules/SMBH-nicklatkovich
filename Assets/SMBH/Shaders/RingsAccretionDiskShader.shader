Shader "Accretion Disk/Rings Colored Accretion Disk" {
	Properties {
		_Noise_0 ("Noise 0", 2D) = "white" {}
		_Noise_1 ("Noise 1", 2D) = "white" {}
		_Noise_2 ("Noise 2", 2D) = "white" {}
		_Rotation_Speed ("Rotation Speed", float) = 1.5
		_Color_0 ("Color 0", Color) = (1, 0.5 ,0)
		_Color_1 ("Color 1", Color) = (1, 0 ,0)
		_Color_2 ("Color 2", Color) = (1, 1 ,0)
		_Color_0_Min ("Color 0 Min", Float) = 0.7
		_Color_1_Max ("Color 1 Max", Float) = 0.575
		_Color_1_Min ("Color 1 Min", Float) = 0.425
		_Color_2_Max ("Color 2 Max", Float) = 0.3
		_Alpha ("Alpha", Float) = 1.0
	}
	SubShader {
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ZWrite Off
		Lighting Off
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha 
		Cull Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			#define PI 3.1415926538

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Noise_0;
			sampler2D _Noise_1;
			sampler2D _Noise_2;
			float _Rotation_Speed;
			float4 _Color_0;
			float4 _Color_1;
			float4 _Color_2;
			float _Color_0_Min;
			float _Color_1_Max;
			float _Color_1_Min;
			float _Color_2_Max;
			float _Alpha;
			float4 _Noise_0_ST;
			float4 _Noise_1_ST;
			float4 _Noise_2_ST;

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Noise_0);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				float2 pos = i.uv;
				fixed4 n_0 = tex2D(_Noise_0, float2(pos.x + _Time.x * 0.3 * _Rotation_Speed, pos.y));
				fixed4 n_1 = tex2D(_Noise_1, float2(pos.x + _Time.x * 0.7 * _Rotation_Speed, pos.y));
				fixed4 n_2 = tex2D(_Noise_2, float2(pos.x + _Time.x * 1.3 * _Rotation_Speed, pos.y));
				float a_0 = (n_0.r + n_0.g + n_0.b) / 3.0;
				float a_1 = (n_1.r + n_1.g + n_1.b) / 3.0;
				float a_2 = (n_2.r + n_2.g + n_2.b) / 3.0;
				float b = sin(pos.y * PI / 2);
				float aa = (a_0 + a_1 + a_2) / 3.0;
				float a = aa * min(1.0, pos.y * 5.0) * (min(1.0, (1.0 - pos.y) * 5.0));
				float3 col;
				if (pos.y >= _Color_0_Min) col = _Color_0.rgb;
				else if (pos.y > _Color_1_Max) col = lerp(_Color_1, _Color_0, (pos.y - _Color_1_Max) / (_Color_0_Min - _Color_1_Max)).rgb;
				else if (pos.y >= _Color_1_Min) col = _Color_1.rgb;
				else if (pos.y > _Color_2_Max) col = lerp(_Color_2, _Color_1, (pos.y - _Color_2_Max) / (_Color_1_Min - _Color_2_Max)).rgb;
				else col = _Color_2.rgb;
				return float4(lerp(float3(0.0, 0.0, 0.0), col, lerp(1.0, a, b)), lerp(a, 1.0, b) * _Alpha);
			}
			ENDCG
		}
	}
}
