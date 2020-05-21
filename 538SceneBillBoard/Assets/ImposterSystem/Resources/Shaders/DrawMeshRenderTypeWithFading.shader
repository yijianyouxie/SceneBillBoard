// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ImposterSystem/Imposter/DrawMeshRenderTypeWithFading" {
	Properties{
		_MainTex("Texture", 2D) = "white" { }
		_Cutoff("Alpha Cutoff", Float) = 0.7
		[HideInInspector] _Color("Main Color", Color) = (1,1,1,1)
	}

		SubShader{
				Pass{
				Tags{ 
				"LightMode" = "ShadowCaster" 
				"RenderType" = "Opaque" }
				ZWrite On
				ZTest Less
				ColorMask 0
				CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"
				uniform float _Cutoff;
				sampler2D _MainTex;
				uniform sampler2D _ImposterSystem_Noise;
				uniform float _ImposterSystem_NoiseResolution;
				uniform float _ImposterSystem_FadeTime;
				uniform float _ImposterSystem_MinAngleToStopLookAtCamera;

				inline float angleBetween(half3 colour, half3 original) {
					return acos(dot(colour, original) / (length(colour) * length(original)));
				}

				struct appdata {
					float4 vertex : POSITION;
					float4 normal : NORMAL;
					float4 texcoord : TEXCOORD0;//texture - uv1
					float4 color : COLOR;
				};

				struct v2f {
					float4  pos : SV_POSITION;
					float4  uv : TEXCOORD0;
					float4 alpha : COLOR;
					float4  screenPos : TEXCOORD1;
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

					float3 upVector = float3(0, 100, 0);
					float3 eyeVector = _WorldSpaceCameraPos - worldPos;
					float3 right = cross(eyeVector, upVector);
					float3 up = cross(eyeVector, right);
					if (v.texcoord.z < 0.1 || abs(degrees(angleBetween(eyeVector, upVector))) < _ImposterSystem_MinAngleToStopLookAtCamera) {
						eyeVector = v.normal;
						right = cross(eyeVector, upVector);
						up = cross(eyeVector, right);
					}

					up = normalize(up);
					right = normalize(right);
					eyeVector = normalize(eyeVector);

					float3 finalposition = worldPos;
					finalposition += v.vertex.x * right;
					finalposition -= v.vertex.y * up;
					finalposition += v.vertex.z * eyeVector;

					float4 pos = float4(finalposition, 1);
					float alpha = 1;
					float side = 1;
					float useFade = 0;
					float targetTime = v.color.a * 100000.0;
					float raznica = abs(targetTime) - _Time.y;
					if (raznica > 0) {
						useFade = 1;
						//alpha = 0.1;
						if (v.color.a > 0) {
							alpha = raznica;
							side = 1;
						}
						else {
							alpha = _ImposterSystem_FadeTime - raznica;
							side = 0;
						}
						alpha /= _ImposterSystem_FadeTime;
					}
					else {
						if (v.color.a < 0) {
							useFade = 1;
							side = 0;
							alpha = 2;
						}
					}
					o.pos = mul(UNITY_MATRIX_MVP, pos);
					o.screenPos = ComputeScreenPos(o.pos);
					o.alpha = float4 (alpha, side, useFade, v.normal.x);
					o.uv = float4(v.texcoord.x, v.texcoord.y, (-sign(v.vertex.x) + 1) * 0.5, (-sign(v.vertex.y) + 1) * 0.5);
					return o;
				}

				half4 frag(v2f i) : COLOR
				{
					float4 textureColor = tex2D(_MainTex, i.uv.xy);
					if (textureColor.a < _Cutoff)
						discard;
					float2 noiseUV = i.screenPos.xy / i.screenPos.w;
					noiseUV.x *= _ScreenParams.x / _ImposterSystem_NoiseResolution;
					noiseUV.y *= _ScreenParams.y / _ImposterSystem_NoiseResolution;
					float noiseValue = abs(i.alpha.y - tex2D(_ImposterSystem_Noise, noiseUV).a);
					//noiseValue = cos(1.57*noiseValue);
					if (i.alpha.z == 1 && noiseValue < i.alpha.x)
					{
						discard;
					}

					return textureColor;
				}

				ENDCG
			}
			
			Pass{

				Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
				ZWrite On
				Blend SrcAlpha OneMinusSrcAlpha
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
							uniform float _Cutoff;
							sampler2D _MainTex;
							uniform sampler2D _ImposterSystem_Noise;
							uniform float _ImposterSystem_NoiseResolution;
							uniform float _ImposterSystem_FadeTime;
							uniform float _ImposterSystem_MinAngleToStopLookAtCamera;

							inline float angleBetween(half3 colour, half3 original) {
								return acos(dot(colour, original) / (length(colour) * length(original)));
							}

							struct appdata {
								float4 vertex : POSITION;
								float4 normal : NORMAL;
								float4 texcoord : TEXCOORD0;//texture - uv1
								float4 color : COLOR;
							};

							struct v2f {
								float4  pos : SV_POSITION;
								float4  uv : TEXCOORD0;
								float4 alpha : COLOR;
								float4  screenPos : TEXCOORD1;
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

								float3 upVector = float3(0, 100, 0);
								float3 eyeVector = _WorldSpaceCameraPos - worldPos;
								float3 right = cross(eyeVector, upVector);
								float3 up = cross(eyeVector, right);
								if (v.texcoord.z < 0.1 || abs(degrees(angleBetween(eyeVector, upVector))) < _ImposterSystem_MinAngleToStopLookAtCamera) {
									eyeVector = v.normal;
									right = cross(eyeVector, upVector);
									up = cross(eyeVector, right);
								}

								up = normalize(up);
								right = normalize(right);
								eyeVector = normalize(eyeVector);

								float3 finalposition = worldPos;
								finalposition += v.vertex.x * right;
								finalposition -= v.vertex.y * up;
								finalposition += v.vertex.z * eyeVector;

								float4 pos = float4(finalposition, 1);
								float alpha = 1;
								float side = 1;
								float useFade = 0;
								float targetTime = v.color.a * 100000.0;
								float raznica = abs(targetTime) - _Time.y;
								if (raznica > 0) {
									useFade = 1;
									//alpha = 0.1;
									if (v.color.a > 0) {
										alpha = raznica;
										side = 1;
									}
									else {
										alpha = _ImposterSystem_FadeTime - raznica; 
										side = 0;
									}
									alpha /= _ImposterSystem_FadeTime;
								}
								else {
									if (v.color.a < 0) {
										useFade = 1;
										side = 0;
										alpha = 2;
									}
								}
								o.pos = mul(UNITY_MATRIX_MVP, pos);
								o.screenPos = ComputeScreenPos(o.pos);
								o.alpha = float4 (alpha, side, useFade, v.normal.x);
								o.uv = float4(v.texcoord.x, v.texcoord.y, (-sign(v.vertex.x) + 1) * 0.5, (-sign(v.vertex.y) + 1) * 0.5);
								return o;
							}

							half4 frag(v2f i) : COLOR
							{
								float4 textureColor = tex2D(_MainTex, i.uv.xy);
								if (textureColor.a < _Cutoff)
									discard;
								float2 noiseUV = i.screenPos.xy / i.screenPos.w;
								noiseUV.x *= _ScreenParams.x / _ImposterSystem_NoiseResolution;
								noiseUV.y *= _ScreenParams.y / _ImposterSystem_NoiseResolution;
								float noiseValue = abs(i.alpha.y - tex2D(_ImposterSystem_Noise, noiseUV).a);
								//noiseValue = cos(1.57*noiseValue);
								if (i.alpha.z == 1 && noiseValue < i.alpha.x)
								{    
									discard;
								}
								
								return textureColor;
							}
							ENDCG
						}
		}
}