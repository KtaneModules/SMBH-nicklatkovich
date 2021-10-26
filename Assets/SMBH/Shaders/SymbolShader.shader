Shader "Black Hole/Symbol" {
	Properties {
		_Texture ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1.0, 0.0, 0.0)
	}
	SubShader {
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ZTest Always
		ZWrite Off
		Lighting Off
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha 

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Texture;
			float4 _Color;
			float4 _Texture_ST;

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Texture);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				float4 col = tex2D(_Texture, i.uv);
				return float4(_Color.rgb, col.r);
			}
			ENDCG
		}
	}
}
