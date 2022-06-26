    Shader "Hidden/Gaia/GrowShrink" {

    Properties {
				//The input texture
				_InputTex ("Input Texture", any) = "" {}				
				//Grow / Shrink radius distance to be applied (scalar to the terrain size)
				_Distance("Distance", Float) = 1
                //scalar size of one pixel 
                _TexelSize("Texel Size", Float) = 0.001
                //1-pixel height transform texture representing an animation curve
				_HeightTransformTex ("Height Transform Texture", any) = "" {}
				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _InputTex;
			float _Distance;
            float _TexelSize;
            sampler2D _HeightTransformTex;

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

            float GetGrowShrink(v2f i)
			{
                float input = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
                //float shiftedInput = (tex2D(_InputTex, float2(i.pcUV.x + 0.1f,i.pcUV.y +0.1f)));
                if(_Distance!=0)
                {
                    float absDistance = abs(_Distance);
                    for (float x = -absDistance; x <=absDistance; x+=_TexelSize) 
                    {
                        for (float y = -absDistance; y <=absDistance; y+=_TexelSize) 
                        {
                            float shiftedInput = (tex2D(_InputTex, float2(i.pcUV.x + x,i.pcUV.y +y)));
                            float interpolated = lerp(shiftedInput,input,smoothstep(0,distance(float2(0,0),float2(absDistance,absDistance)),distance(float2(0,0),float2(x,y))));

                            if(_Distance>0)
                            {
                                if(interpolated>input)
                                {
                                    input = interpolated;
                                }
                            }
                            else
                            {
                                if(interpolated<input)
                                {
                                    input = interpolated;
                                }
                            }
                        }
                    }
				}

                return input;
            }

		ENDCG
            

        Pass    //0 - Pass with strength transform
        {
            Name "GrowShrinkStrengthTransform"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment GrowShrinkStrengthTransform
            float4 GrowShrinkStrengthTransform(v2f i) : SV_Target
            {
                float input = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetGrowShrink(i);
				float strengthTransform = UnpackHeightmap(tex2D(_HeightTransformTex, filter));
				return PackHeightmap(strengthTransform);
            }
            ENDCG
        }

               Pass    //1 - Pass without strength transform
        {
            Name "GrowShrinkWithoutStrengthTransform"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment GrowShrinkWithoutStrengthTransform
            float4 GrowShrinkWithoutStrengthTransform(v2f i) : SV_Target
            {
                float input = UnpackHeightmap(tex2D(_InputTex, i.pcUV));
				float filter = GetGrowShrink(i);
				return PackHeightmap(filter);
            }
            ENDCG
        }
    }
    Fallback Off
}
