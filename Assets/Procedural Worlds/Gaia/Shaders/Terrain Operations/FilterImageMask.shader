    Shader "Hidden/Gaia/FilterImageMask" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}				
				//The image mask texture
				_ImageMaskTex ("Image Mask Texture", any) = "" {}
				//Flag to determine which Filter Mode to use (grayscale, color selection, RGBA channel)
				_FilterMode("FilterMode", Int) = 0
				//The selected color (if in color selection filter mode)
				_Color("Selected Color", Color) = (1, 1, 1, 1)
				//The selection accuracy (if in color selection filter mode)
				_ColorAccuracy("Color Accuracy", Float) = 0
				//1-pixel height transform texture representing an animation curve
				_HeightTransformTex ("Height Transform Texture", any) = "" {}
				 //X-Offset on the terrain for the center of this image mask
				_XOffset("X Offset", Float) = 0
				//Z-Offset on the terrain for the center of this image mask
				_ZOffset("Z Offset", Float) = 0
				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			sampler2D _ImageMaskTex;
			int _FilterMode;
			float4 _Color;
			float _ColorAccuracy;
			sampler2D _HeightTransformTex;
			float _XOffset;
			float _ZOffset;
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

			


			/// <summary>
			/// Return true if the values are approximately equal
			/// </summary>
			/// <param name="a">Parameter A</param>
			/// <param name="b">Parameter B</param>
			/// <returns>True if approximately equal</returns>
			bool ApproximatelyEqual(float a, float b, float delta)
			{
				if (a == b || abs(a - b) < delta)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			/// <summary>
			/// Convert XYZ colour to LAB colour, where resulting x = L, y = a, z = b
			/// </summary>
			/// <param name="c">Source xyz colour</param>
			/// <returns>LAB colour where x = L, y = a, z = b</returns>
			float3 XYZtoLAB(float3 c)
			{
				// Based on http://www.easyrgb.com/index.php?X=MATH&H=07
				float ref_Y = 100.0f;
				float ref_Z = 108.883f;
				float ref_X = 95.047f; // Observer= 2°, Illuminant= D65
				float Y = c.y / ref_Y;
				float Z = c.z / ref_Z;
				float X = c.x / ref_X;
				if (X > 0.008856f)
					X = pow(X, 1.0f / 3.0f);
				else
					X = (7.787f * X) + (16.0f / 116.0f);
				if (Y > 0.008856f)
					Y = pow(Y, 1.0f / 3.0f);
				else
					Y = (7.787f * Y) + (16.0f / 116.0f);
				if (Z > 0.008856f)
					Z = pow(Z, 1.0f / 3.0f);
				else
					Z = (7.787f * Z) + (16.0f / 116.0f);
				float L = (116.0f * Y) - 16.0f;
				float a = 500.0f * (X - Y);
				float b = 200.0f * (Y - Z);
				return float3(L, a, b);
			}

			/// <summary>
			/// Convert RGB to XYZ colour
			/// </summary>
			/// <param name="c">Source colour to convert</param>
			/// <returns>Source colour as xyz colour</returns>
			float3 RGBtoXYZ(float4 c)
			{
				// Based on http://www.easyrgb.com/index.php?X=MATH&H=02
				float R = c.r;
				float G = c.g;
				float B = c.b;

				if (R > 0.04045f)
					R = pow(((R + 0.055f) / 1.055f), 2.4f);
				else
					R = R / 12.92f;
				if (G > 0.04045f)
					G = pow(((G + 0.055f) / 1.055f), 2.4f);
				else
					G = G / 12.92f;
				if (B > 0.04045f)
					B = pow(((B + 0.055f) / 1.055f), 2.4f);
				else
					B = B / 12.92f;

				R *= 100.0f;
				G *= 100.0f;
				B *= 100.0f;

				// Observer. = 2°, Illuminant = D65
				float X = R * 0.4124f + G * 0.3576f + B * 0.1805f;
				float Y = R * 0.2126f + G * 0.7152f + B * 0.0722f;
				float Z = R * 0.0193f + G * 0.1192f + B * 0.9505f;
				return float3(X, Y, Z);
			}

			/// <summary>
			/// Convert rgb to lab colour
			/// </summary>
			/// <param name="c">Source colour to convert</param>
			/// <returns>Lab colour x = L, y = a, z = b</returns>
			float3 RGBtoLAB(float4 c)
			{
				return XYZtoLAB(RGBtoXYZ(c));
			}

			

			


			/// <summary>
	   /// Calculate the CIE76 difference between two colours
	   /// </summary>
	   /// <param name="c1"></param>
	   /// <param name="c2"></param>
	   /// <returns></returns>
		float RGBDifference(float4 c1, float4 c2, float epsilon)
		{
			if (ApproximatelyEqual(c1.r, c2.r, epsilon) && ApproximatelyEqual(c1.g, c2.g, epsilon) && ApproximatelyEqual(c1.b, c2.b, epsilon))
			{
				return 0.0f;
			}
			float3 l1 = RGBtoLAB(c1);
			float3 l2 = RGBtoLAB(c2);
			float sum = 0.0f;
			sum += pow(l1.x - l2.x, 2.0f);
			sum += pow(l1.y - l2.y, 2.0f);
			sum += pow(l1.z - l2.z, 2.0f);
			return max(min(sqrt(sum), 100.0f), 0.0f);
		}

		//Gets the "second color" that represents the fitness from the image
		float GetCol2(v2f i)
		{
			float2 UVOffset = i.pcUV;
			UVOffset.x += _XOffset;
			UVOffset.y += _ZOffset;

			if(UVOffset.x > 1.0f || UVOffset.x < 0.0f || UVOffset.y > 1.0f || UVOffset.y < 0.0f)
			{
				return 0.0f;
			}


			float col2 = 0;
			float4 imageMaskColor = tex2D(_ImageMaskTex, UVOffset);//smoothstep(0, sqrt(1) / 2, distance(UVOffset, float2(0.5f, 0.5f))));

			switch (_FilterMode)
			{
			case 0:
				
				col2 = max(imageMaskColor.r, max(imageMaskColor.g ,imageMaskColor.b));
				break;
			case 1:
				const float epsilon = 0.00001;
				float difference = RGBDifference(imageMaskColor, _Color, epsilon);
				if (difference < (1.0f - _ColorAccuracy) * 100.0f)
				{
					col2 = 1.0f - (difference / 100.0f) * _ColorAccuracy;
				}
				break;
			case 2:
				col2 = imageMaskColor.r;
				break;
			case 3:
				col2 = imageMaskColor.g;
				break;
			case 4:
				col2 = imageMaskColor.b;
				break;
			case 5:
				col2 = imageMaskColor.a;
				break;
			}
			return col2;
		}
	

			

		ENDCG
            

        Pass    // 0 Filter Image Multiply
        {
            Name "Filter Image Multiply"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FilterImageMultiply

            float4 FilterImageMultiply(v2f i) : SV_Target
            {
				float col = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float col2 = GetCol2(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, col2));
				float result = col * transformedHeight;
				return PackHeightmap(result);
            }
            ENDCG
        }

		Pass    // 1 Filter Image Greater Than
        {
            Name "Filter Image Greater Than"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FilterImageGreaterThan

            float4 FilterImageGreaterThan(v2f i) : SV_Target
            {
				float col = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float col2 = GetCol2(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, col2));
				float result = col;
				if(transformedHeight>col)
				{
					result = transformedHeight;
				}
				return PackHeightmap(result);
            }
            ENDCG
        }

		Pass    // 2 Filter Image Smaller Than
        {
            Name "Filter Image Smaller Than"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FilterImageSmallerThan

            float4 FilterImageSmallerThan(v2f i) : SV_Target
            {
				float col = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float col2 = GetCol2(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, col2));
				float result = col;
				if(transformedHeight<col)
				{
					result = transformedHeight;
				}

				return PackHeightmap(result);
            }
            ENDCG
        }

		Pass    // 3 Filter Image Add
        {
            Name "Filter Image Add"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FilterImageAdd

            float4 FilterImageAdd(v2f i) : SV_Target
            {
				float col = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float col2 = GetCol2(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, col2));
				float result = col + transformedHeight;
				return PackHeightmap(result);
            }
            ENDCG
        }

		Pass    // 4 Filter Image Subtract
        {
            Name "Filter Image Subtract"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FilterImageSubtract

            float4 FilterImageSubtract(v2f i) : SV_Target
            {
				float col = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float col2 = GetCol2(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, col2));
				float result = col - transformedHeight;
				return PackHeightmap(result);
            }
            ENDCG
        }

    }
    Fallback Off
}
