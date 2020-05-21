// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ImposterSystem/Imposter/DrawMeshRenderType" {
	Properties{
		_MainTex("Texture", 2D) = "white" { }
		_Cutoff("Alpha Cutoff", Float) = 0.7
	}


	SubShader{

			Pass{
			Tags{ "LightMode" = "ShadowCaster" "RenderType" = "Opaque" }
			ZWrite On
			ZTest Less
			ColorMask 0
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

			uniform float _Cutoff;
		sampler2D _MainTex;

		struct appdata {
			float4 vertex : POSITION;
			float4 texcoord : TEXCOORD0;//texture - uv1
			float4 color : COLOR;
		};

		struct v2f {
			float4  pos : SV_POSITION;
			float2  uv : TEXCOORD0;
		};

		v2f vert(appdata v)
		{
			v2f o;
			float max = 100000;
			float3 world = v.color.xyz;
			float3 worldPos = float3(
				world.x * max - max / 2,
				world.y * max - max / 2,
				world.z * max - max / 2
				);

			float3 eyeVector = _WorldSpaceCameraPos - worldPos;

			float3 upVector = float3(0,1,0);

			float3 right = cross(eyeVector, upVector);
			float3 up = normalize(cross(eyeVector, right));

			right = normalize(right);
			eyeVector = normalize(eyeVector);

			float3 finalposition = worldPos;
			finalposition += v.vertex.x * right;
			finalposition -= v.vertex.y * up;
			finalposition += v.vertex.z * eyeVector;

			float4 pos = float4(finalposition, 1);

			o.pos = mul(UNITY_MATRIX_MVP, pos);
			o.uv = v.texcoord.xy;

			return o;
		}

		half4 frag(v2f i) : COLOR
		{
			float4 textureColor = tex2D(_MainTex, i.uv.xy);
			if (textureColor.a < _Cutoff)
			{
				discard;
			}
			return textureColor;
		}
			ENDCG
		}

		Pass{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

			uniform float _Cutoff;
			sampler2D _MainTex;

			struct appdata {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;//texture - uv1
				float4 color : COLOR;
			};

			struct v2f {
				float4  pos : SV_POSITION;
				float2  uv : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				float max = 100000;
				float3 world = v.color.xyz;
				float3 worldPos = float3(
					world.x * max - max / 2,
					world.y * max - max / 2,
					world.z * max - max / 2
					);

				float3 eyeVector = _WorldSpaceCameraPos - worldPos;

				float3 upVector = float3(0,1,0);

				float3 right = cross(eyeVector, upVector);
				float3 up = normalize(cross(eyeVector, right));

				right = normalize(right);
				eyeVector = normalize(eyeVector);

				float3 finalposition = worldPos;
				finalposition += v.vertex.x * right;
				finalposition -= v.vertex.y * up;
				finalposition += v.vertex.z * eyeVector;

				float4 pos = float4(finalposition, 1);

				o.pos = mul(UNITY_MATRIX_MVP, pos);
				o.uv = v.texcoord.xy;

				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				float4 textureColor = tex2D(_MainTex, i.uv.xy);
				if (textureColor.a < _Cutoff)
				{
					discard;
				}
				return textureColor;
			}
			ENDCG
		}
	}
}