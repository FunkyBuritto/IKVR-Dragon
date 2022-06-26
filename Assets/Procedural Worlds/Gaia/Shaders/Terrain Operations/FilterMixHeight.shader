    Shader "Hidden/Gaia/MixHeight" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}	
				//The brush texture
				_BrushTex("Brush Texture", any) = "" {}
                //The stamp texture we want to mix in
                StampTex("Stamp Texture", any) =""{}
                //The level at which the mixing with the brush value takes place
                //0=at the bottom level of the Brush
                //1=at the top level of the Brush 
				 _MixMidPoint("Mix Mid Point", Float) = 0.5
				 //Strength from 0 to 1 to determine how "strong" the effect is applied
				 _Strength ("Strength", Float) = 0
                 //The minimum scalar (0....1) height from the exisiting terrain
                 _WorldHeightMin("WorldHeightMin", Float) = 0
                 //The maximum scalar (0....1) height from the exisiting terrain
                 _WorldHeightMax("WorldHeightMax", Float) = 1

				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			sampler2D _BrushTex;
            sampler2D _StampTex;
			float _MixMidPoint;
			float _Strength;
            float _WorldHeightMin;
            float _WorldHeightMax;

			
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
            

         Pass    // 0 Height Mix
        {
            Name "Height Mix"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment HeightMix

            float4 HeightMix(v2f i) : SV_Target
            {
            	float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

				float inputHeight = tex2D(_InputTex, heightmapUV);
				float brushStrength = oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
                float stampHeight = oob * UnpackHeightmap(tex2D(_StampTex, brushUV));

                float target = inputHeight + ((stampHeight - _MixMidPoint) * (_WorldHeightMax - _WorldHeightMin) * _Strength) ;
				return PackHeightmap(lerp(inputHeight, target, brushStrength));
            }
            ENDCG
        }

    }
    Fallback Off
}
