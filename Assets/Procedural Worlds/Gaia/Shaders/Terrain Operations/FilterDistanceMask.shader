    Shader "Hidden/Gaia/FilterDistanceMask" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}				
				//1-pixel high distance mask texture representing an animation curve
				_DistanceMaskTex ("Distance Mask Texture", any) = "" {}
				//Int to determine the axis mode, 0 = XZ, 1= X, 2=Z
				_AxisMode("Axis Mode", Int) = 0
			    //X-Offset on the terrain for the center of this distance mask
				_XOffset("X Offset", Float) = 0
				//Z-Offset on the terrain for the center of this distance mask
				_ZOffset("Z Offset", Float) = 0
				//1-pixel height transform texture representing an animation curve
				_HeightTransformTex ("Height Transform Texture", any) = "" {}

				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			sampler2D _DistanceMaskTex;
			sampler2D _HeightTransformTex;
			int _AxisMode;
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

			float GetFilter(v2f i)
			{
				float2 UVOffset = i.pcUV;
				UVOffset.x += _XOffset;
				UVOffset.y += _ZOffset;


				float filter = 0.0f;

				if (_AxisMode == 0)
				{
					filter = UnpackHeightmap(tex2D(_DistanceMaskTex, smoothstep(0, sqrt(1) / 2, distance(UVOffset, float2(0.5f, 0.5f)))));
				}
				else if (_AxisMode == 1)
				{
					filter = UnpackHeightmap(tex2D(_DistanceMaskTex, float2(i.pcUV.x, 0)));
				}
				else
				{
					filter = UnpackHeightmap(tex2D(_DistanceMaskTex, float2(i.pcUV.y, 0)));
				}

				return filter;
			}
		ENDCG
            

         Pass    // 0 Multiply
        {
            Name "Distance Mask Multiply"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment DistanceMaskMultiply

            float4 DistanceMaskMultiply(v2f i) : SV_Target
            {
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight = UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height * transformedHeight;
				return PackHeightmap(result);
            }
            ENDCG
        }
	
		
		Pass    // 1 Distance Mask Greater Than
        {
            Name "Distance Mask Greater Than"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment DistanceMaskGreaterThan

            float4 DistanceMaskGreaterThan(v2f i) : SV_Target
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

	
	   Pass    // 2 Distance Mask Smaller Than
		{
			Name "Distance Mask Smaller Than"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment DistanceMaskSmallerThan

			float4 DistanceMaskSmallerThan(v2f i) : SV_Target
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

		Pass    // 3 Distance Mask Add
		{
			Name "Distance Mask Add"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment DistanceMaskAdd

			float4 DistanceMaskAdd(v2f i) : SV_Target
			{
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight =UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height + transformedHeight;
				return PackHeightmap(result);
			}
			ENDCG
		}

		Pass    // 4 Distance Mask Subtract
		{
			Name "Distance Mask Subtract"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment DistanceMaskSubtract

			float4 DistanceMaskSubtract(v2f i) : SV_Target
			{
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetFilter(i);
				float transformedHeight =UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				float result = height - transformedHeight;
				return PackHeightmap(result);
			}
			ENDCG
		}

    }
    Fallback Off
}
