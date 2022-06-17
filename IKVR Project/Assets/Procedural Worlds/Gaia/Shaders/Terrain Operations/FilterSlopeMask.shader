    Shader "Hidden/Gaia/FilterSlopeMask" {

    Properties {
				//The main texture (existing heightmap)
				_NormalMapTex("Normalmap Texture", any) = "" {}
				//The input texture
				_InputTex ("Input Texture", any) = "" {}				
				//1-pixel high distance mask texture representing an animation curve
				_SlopeMaskTex ("Slope Mask Texture", any) = "" {}
				//Lower height bound where the height mask will be applied
				_MinSlope ("Minimum Slope", Float) = 0
				//Upper height bound where the height mask will be applied
				_MaxSlope ("Maximum Slope", Float) = 0
				//1-pixel height transform texture representing an animation curve
				_HeightTransformTex ("Height Transform Texture", any) = "" {}

				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

			sampler2D _NormalMapTex;
            sampler2D _InputTex;
			sampler2D _SlopeMaskTex;
			sampler2D _HeightTransformTex;
			float _MinSlope;
			float _MaxSlope;
			
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

           sampler2D _BrushTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_TARGETHEIGHT  (_BrushParams[1])

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

			float GetFilter(v2f i)
			{
				float3 normal = tex2D(_NormalMapTex, i.pcUV);
				//float slope = 1.0f - normal.y;
				float3  upwardsVector = { 0.0f, 1.0f, 0.0f };
				float slope = clamp(1.0f - dot(upwardsVector, normal), 0.0f, 1.0f);
				float filter = UnpackHeightmap(tex2D(_SlopeMaskTex, smoothstep(_MinSlope, _MaxSlope, slope)));
				return filter;
			}


		ENDCG
            

         Pass    // 0 Slope Mask Multiply
        {
            Name "Slope Mask Multiply"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SlopeMaskMultiply

            float4 SlopeMaskMultiply(v2f i) : SV_Target
            {
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height*transformedHeight;
				return PackHeightmap(result);
            }
            ENDCG
        }

		Pass    // 1 Slope Mask Greater Than
		{
			Name "Slope Mask Greater Than"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment SlopeMaskGreaterThan

			float4 SlopeMaskGreaterThan(v2f i) : SV_Target
			{
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height;
				if (transformedHeight > height)
				{
					result = transformedHeight;
				}
				return PackHeightmap(result);
			}
			ENDCG
		}

		Pass    // 2 Slope Mask Smaller Than
		{
			Name "Slope Mask Smaller Than"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment SlopeMaskSmallerThan

			float4 SlopeMaskSmallerThan(v2f i) : SV_Target
			{
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height;
				if (transformedHeight < height)
				{
					result = transformedHeight;
				}
				return PackHeightmap(result);
			}
			ENDCG
		}
	

		Pass    // 3 Slope Mask Add
		{
			Name "Slope Mask Add"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment SlopeMaskAdd

			float4 SlopeMaskAdd(v2f i) : SV_Target
			{
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height + transformedHeight;
				return PackHeightmap(result);
			}
			ENDCG
		}

		Pass    // 4 Slope Mask Subtract
		{
			Name "Slope Mask Subtract"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment SlopeMaskSubtract

			float4 SlopeMaskSubtract(v2f i) : SV_Target
			{
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height - transformedHeight;
				return PackHeightmap(result);
			}

			ENDCG
		}
	

    }
    Fallback Off
}
