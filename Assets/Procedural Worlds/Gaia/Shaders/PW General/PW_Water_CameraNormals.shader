Shader "Unlit/PW_Water_CameraNormals"
{
    Properties
    {
    }
    SubShader
    {
        Tags 
		{ 
			"RenderType" = "Opaque" 
		}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 viewNormal : NORMAL;
				float  depth : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex 		= UnityObjectToClipPos(v.vertex);
                o.viewNormal 	= COMPUTE_VIEW_NORMAL;
				o.depth 		= o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4 ( i.viewNormal, i.depth );
            }
            ENDCG
        }
    }
}
