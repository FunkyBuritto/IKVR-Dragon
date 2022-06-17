    Shader "Hidden/Gaia/TreeCollisionMaskBaking" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}				
				//number of positions to process
				_PositionsCount("Position Count", int) = 0
				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			int _PositionsCount = 0;
			float4 _TargetPosition[1000];

			
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
				float filter = 1.0f;

				for (int k = 0; k < _PositionsCount; k++) {
					float currentFilter = smoothstep(0, _TargetPosition[k].w, distance(i.pcUV, float2(_TargetPosition[k].x, _TargetPosition[k].z)));
					if(currentFilter<filter)
					{
						filter = currentFilter;
					}
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

				return PackHeightmap(filter);
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

				float result = height;
				if (filter > height)
				{
					result = filter;
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

				float result = height;
				if (filter < height)
				{
					result = filter;
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
				float result = height + filter;
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
				float result = height - filter;
				return PackHeightmap(result);
			}
			ENDCG
		}

    }
    Fallback Off
}
