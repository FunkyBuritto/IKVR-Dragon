    Shader "Hidden/Gaia/FlipY" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}				
				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			
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
            

        Pass    //0 - Pass with strength transform
        {
            Name "FlipY"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FlipY
            float4 FlipY(v2f i) : SV_Target
            {
                float4 flippedInput = tex2D(_InputTex, float2(i.pcUV.x,1-i.pcUV.y));

				return flippedInput;
            }
            ENDCG
        }
    }
    Fallback Off
}
