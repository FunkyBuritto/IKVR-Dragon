Shader "Hidden/Gaia/CombineCollisionMasks" {

	Properties{
	//The input texture
	_InputTex("Input Texture", any) = "" {}
	//The image mask texture
	_ImageMaskTex("Image Mask Texture", any) = "" {}
	//V4 to store width & height dimensions of textures: x,y=_InputTex z,w=_ImageMaskText
	_Dimensions("Texture Dimensions", Vector) = (1,1,1,1)
	//Flag to determine whether the Mask is inverted or not
	_Invert("Invert Distance Mask", Int) = 0
	//Strength from 0 to 1 to determine how "strong" the image mask effect is applied
	_Strength("Strength", Float) = 0


	}

		SubShader{

			ZTest Always Cull Off ZWrite Off

			CGINCLUDE

				#include "UnityCG.cginc"
				#include "TerrainTool.cginc"

				sampler2D _InputTex;
				sampler2D _ImageMaskTex;
				int _Invert;
				float _Strength;
				float4 _Dimensions;

				float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height



				struct appdata_t {
					float4 vertex : POSITION;
					float2 pcUV : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 pcUV : TEXCOORD0;
				};

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.pcUV = v.pcUV;
					return o;
				}
			ENDCG


			Pass    // 1 Filter Image Greater Than
			{
				Name "Filter Image Greater Than"

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment FilterImageGreaterThan

				float4 FilterImageGreaterThan(v2f i) : SV_Target
				{
					float col = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
					//scale the UV input according to the size difference so that the image mask tex
					float2 relativeUV = i.pcUV;
					relativeUV.x = i.pcUV.x * 1.0f;// (_Dimensions.z / _Dimensions.x);
					relativeUV.y = i.pcUV.y * 1.0f; // (_Dimensions.w / _Dimensions.y);
					float col2 = UnpackHeightmap(tex2D(_ImageMaskTex, relativeUV));



					if (_Invert > 0)
					{
						col2 = (1.0f - col2);
					}

					float result = col;
					if (col2 < col)
					{
						result = col2;
					}

					return PackHeightmap(result);
					//return PackHeightmap(lerp(col,result,_Strength));
				}
				ENDCG
			}


	}
		Fallback Off
}
