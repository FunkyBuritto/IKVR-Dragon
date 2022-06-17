// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

    Shader "Hidden/GaiaPro/PowerOf" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}				
				//The image mask texture
				_BrushTex ("Brush Texture", any) = "" {}
				//Flag to determine whether the Mask is inverted or not
				_Invert("Invert Distance Mask", Int) = 0
				//Power function to be applied
				_Power("Strength", Float) = 1
				//Strength from 0 to 1 to determine how "strong" the image mask effect is applied
				_Strength ("Strength", Float) = 0


				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			sampler2D _BrushTex;
			int _Invert;
			float _Power;
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
            

        Pass    // 0 Power Of
        {
            Name "Power Of"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FilterPowerOf

            float4 FilterPowerOf(v2f i) : SV_Target
            {
				float height = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float powerOfHeight = pow(height,4.0f-_Power);
				// out of bounds multiplier
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				float brushStrength = oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
				//noise = ((noise - 0.5f) * _ContrastFilter) + 0.5f;
				//if (_Invert > 0)
				//{
				//	powerOfHeight = (1.0f - powerOfHeight);
				//}
				//return PackHeightmap(height + height * (powerOfHeight-height));
                return PackHeightmap(lerp(height, powerOfHeight, brushStrength));
				//return PackHeightmap(lerp(height,(height * powerOfHeight),_Strength));
            }
            ENDCG
        }

    }
    Fallback Off
}
