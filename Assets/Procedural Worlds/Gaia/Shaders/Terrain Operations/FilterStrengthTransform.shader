    Shader "Hidden/GaiaPro/StrengthTransform" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}	
				//The brush texture
				_BrushTex("Brush Texture", any) = "" {}
				//1-pixel high distance mask texture representing an animation curve
				_HeightTransformTex ("Height Transform Texture", any) = "" {}
				 //Flag to determine whether the Distance mask is inverted or not
				 _Invert("Invert Image Mask", Float) = 0
				 //Strength from 0 to 1 to determine how "strong" the distance mask effect is applied
				 _Strength ("Strength", Float) = 0

				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			sampler2D _BrushTex;
			sampler2D _HeightTransformTex;
			float _Invert;
			float _Strength;
			
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
            

         Pass    // 0 Strength Transform Multiply
        {
            Name "Strength Transform Multiply"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment StrengthTransformMultiply

            float4 StrengthTransformMultiply(v2f i) : SV_Target
            {
				float inputHeight = tex2D(_InputTex, i.pcUV);
				float test = UnpackHeightmap(tex2D(_HeightTransformTex, i.pcUV));
				float transformedHeight = lerp(0.0f,1.0f,UnpackHeightmap(tex2D(_HeightTransformTex, inputHeight)));
				if (_Invert > 0)
				{
					transformedHeight = (1.0f - transformedHeight);
				}

				//return PackHeightmap(transformedHeight * brushStrength);
				return PackHeightmap(lerp(inputHeight, transformedHeight, _Strength));
            }
            ENDCG
        }

		Pass    // 1 Strength Transform Greater Than
		{
			Name "Strength Transform Greater Than"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment StrengthTransformGreaterThan

			float4 StrengthTransformGreaterThan(v2f i) : SV_Target
			{
				float inputHeight = tex2D(_InputTex, i.pcUV);
				float test = UnpackHeightmap(tex2D(_HeightTransformTex, i.pcUV));
				float transformedHeight = lerp(0.0f,1.0f,UnpackHeightmap(tex2D(_HeightTransformTex, inputHeight)));

				if (_Invert > 0)
				{
					transformedHeight = (1.0f - transformedHeight);
				}

				float result = inputHeight;
				if (transformedHeight > inputHeight)
				{
					result = transformedHeight;
				}
				//return PackHeightmap(transformedHeight * brushStrength);
				return PackHeightmap(lerp(inputHeight, result, _Strength));
			}
			ENDCG
		}

		Pass    // 2 Strength Transform Smaller Than
		{
			Name "Strength Transform Smaller Than"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment StrengthTransformSmallerThan

			float4 StrengthTransformSmallerThan(v2f i) : SV_Target
			{
				float inputHeight = tex2D(_InputTex, i.pcUV);
				float test = UnpackHeightmap(tex2D(_HeightTransformTex, i.pcUV));
				float transformedHeight = lerp(0.0f,1.0f,UnpackHeightmap(tex2D(_HeightTransformTex, inputHeight)));

				if (_Invert > 0)
				{
					transformedHeight = (1.0f - transformedHeight);
				}

				float result = inputHeight;
				if (transformedHeight < inputHeight)
				{
					result = transformedHeight;
				}
				//return PackHeightmap(transformedHeight * brushStrength);
				return PackHeightmap(lerp(inputHeight, result, _Strength));
			}
			ENDCG
		}


		Pass    // 3 Strength Transform Add
		{
			Name "Strength Transform Add"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment StrengthTransformAdd

			float4 StrengthTransformAdd(v2f i) : SV_Target
			{
				float inputHeight = tex2D(_InputTex, i.pcUV);
				float test = UnpackHeightmap(tex2D(_HeightTransformTex, i.pcUV));
				float transformedHeight = lerp(0.0f,1.0f,UnpackHeightmap(tex2D(_HeightTransformTex, inputHeight)));

				if (_Invert > 0)
				{
					transformedHeight = (1.0f - transformedHeight);
				}
				
				//return PackHeightmap(transformedHeight * brushStrength);
				return PackHeightmap(lerp(inputHeight, inputHeight + transformedHeight, _Strength));
			}
			ENDCG
		}

				Pass    // 4 Strength Transform Subtract
			{
				Name "Strength Transform Subtract"

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment StrengthTransformSubtract

				float4 StrengthTransformSubtract(v2f i) : SV_Target
				{
					float inputHeight = tex2D(_InputTex, i.pcUV);
					float test = UnpackHeightmap(tex2D(_HeightTransformTex, i.pcUV));
					float transformedHeight = lerp(0.0f,1.0f,UnpackHeightmap(tex2D(_HeightTransformTex, inputHeight)));

					if (_Invert > 0)
					{
						transformedHeight = (1.0f - transformedHeight);
					}

					//return PackHeightmap(transformedHeight * brushStrength);
					return PackHeightmap(lerp(inputHeight, inputHeight - transformedHeight, _Strength));
				}
				ENDCG
			}

    }
    Fallback Off
}
