    Shader "Hidden/GaiaPro/HeightTransform" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}	
				//The brush texture
				_BrushTex("Brush Texture", any) = "" {}
				//1-pixel high distance mask texture representing an animation curve
				_HeightTransformTex ("Height Transform Texture", any) = "" {}
				 //Flag to determine whether the Distance mask is inverted or not
				 _InvertImageMask("Invert Image Mask", Float) = 0
				 //Scalar Max World Height value to normalize transform curve against
				 _MaxWorldHeight ("Scalar Max World Height", Float) = 0.8
                 //Scalar Min World Height value to normalize transform curve against
				 _MinWorldHeight ("Scalar Min World Height", Float) = 0.2
				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			sampler2D _BrushTex;
			sampler2D _HeightTransformTex;
			float _InvertImageMask;
			float _MaxWorldHeight;
            float _MinWorldHeight;
			
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
            

         Pass    // 0 Height Transform
        {
            Name "Height Transform"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment HeightTransform

            float4 HeightTransform(v2f i) : SV_Target
            {
				float inputHeight = tex2D(_InputTex, i.pcUV);
        		float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				if(!oob)
				{
					return inputHeight;
				}
                float normalizedHeight = smoothstep(_MinWorldHeight,_MaxWorldHeight, inputHeight);
			    float transformedHeight = (_MaxWorldHeight - _MinWorldHeight) * UnpackHeightmap(tex2D(_HeightTransformTex, normalizedHeight));
				float brushStrength = UnpackHeightmap(tex2D(_BrushTex, i.pcUV));
				return PackHeightmap(lerp(inputHeight, transformedHeight, brushStrength));
            }
            ENDCG
        }

    }
    Fallback Off
}
