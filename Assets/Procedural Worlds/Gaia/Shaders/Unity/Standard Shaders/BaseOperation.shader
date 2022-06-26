// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

    Shader "Hidden/Gaia/BaseOperation" {

    Properties {
				//The main texture
				_MainTex ("Main Texture", any) = "" {}				
				//The global mask texture
				 _GlobalMaskTex ("Global Mask Texture", any) = "" {}
				 //Flag to determine whether the image mask affects the total stamp result (global) or the stamp itself only (local)
				// _ImageMaskIsGlobal("Image Mask is Global", Float) = 0
				 //Flag to determine whether the image mask is inverted or not
				 //_InvertImageMask("Invert Image Mask", Float) = 0
				 //1-pixel high gradient texture to bring the distance mask curve information in the shader
				 //_DistanceMaskTex ("Distance Mask Texture", any) = "" {}
				 //Flag to determine whether the distance mask affects the total stamp result (global) or the stamp itself only (local)
				 //_DistanceMaskIsGlobal("Distance Mask is Global", Float) = 0
				 //1-pixel high gradient texture to bring the transform height curve information in the shader
				 //_TransformHeightTex ("Transform Height Texture", any) = "" {}
				 //the y position of the stamp, in 0 to 1 relative to terrain height
				 _yPos ("y Position", Float) = 0
				 //the base level of the stamp
				 _BaseLevel ("Base Level", Float) = 0
				 //Flag to determine whether the base should be stamped or not
				 _StampBase("Stamp Base", Float) = 0
				 //Flag to determine whether the stamp should be adjusted automatically to the terrain when a base level is used
				 _AdaptiveBase("Adaptive Base", Float) = 0
				 //Blend strength from 0 to 1 for the blend terrain pass
				 //_BlendStrength ("Blend Strength", Float) = 0
				 //Erosion texture, contains the original heightmap changed by Erosion
				 //_ErosionTex("Erosion Texture", any) = ""

				 }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _MainTex;
			sampler2D _GlobalMaskTex;
			//float _ImageMaskIsGlobal;
			//float _InvertImageMask;
			//sampler2D _DistanceMaskTex;
			//float _DistanceMaskIsGlobal;
			//sampler2D _TransformHeightTex;
			float _yPos;
			float _BaseLevel;
			float _StampBase;
			float _AdaptiveBase;
			float _BlendStrength;
			//sampler2D _ErosionTex;
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

            float ApplyBrush(float height, float brushStrength)
            {
                float targetHeight = BRUSH_TARGETHEIGHT;
                if (targetHeight > height)
                {
                    height += brushStrength;
                    height = height < targetHeight ? height : targetHeight;
                }
                else
                {
                    height -= brushStrength;
                    height = height > targetHeight ? height : targetHeight;
                }
                return height;
            }

			float GetFinalHeight(v2f i)
			{
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				//value from the image mask texture multiplied with the multiplicator
				float globalMaskValue = UnpackHeightmap(tex2D(_GlobalMaskTex, brushUV));
                float brushShape = oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
				float finalBrushLevel = BRUSH_STRENGTH * brushShape; // * transformheight * localMaskValue; 
				
				//only return the height value changed by the brush if we are actually in bounds
				//otherwise we get an ugly square base when rotating stamps
				if(all(saturate(brushUV) == brushUV))
				{
					float finalResult = _yPos + finalBrushLevel;
					return clamp(((_yPos + finalBrushLevel) * globalMaskValue) + height * (1.0f - globalMaskValue), 0, 1.0f);
				}
				else
				{
					return height;
				}
			}

        ENDCG

        Pass    // 0 raise heights
        {
            Name "Raise Heights"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment RaiseHeight

            float4 RaiseHeight(v2f i) : SV_Target
            {
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float finalheight = GetFinalHeight(i);

				//process baseLevel
				if(_StampBase>0 || finalheight>_BaseLevel)
				{
					if(!all(saturate(brushUV) == brushUV))
					{
						_BaseLevel = 0;
					}
					if(_AdaptiveBase && _BaseLevel > height)
					{
						finalheight -= _BaseLevel - height; 
					}
					else
					{
						finalheight = clamp (finalheight, _BaseLevel,0.5f); 
					}
				}
				else
				{
					finalheight = 0;
				}

				if(finalheight>=height)
				return PackHeightmap(finalheight);
				else
				return PackHeightmap(height);
				
            }
            ENDCG
        }

        Pass    // 1 Lower heights
        {
            Name "Lower Heights"

			CGPROGRAM
			#pragma vertex vert
            #pragma fragment LowerHeight

            float4 LowerHeight(v2f i) : SV_Target
            {
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float finalheight = GetFinalHeight(i);

				//process baseLevel
				if(_StampBase>0 || finalheight<_BaseLevel)
				{
					if(!all(saturate(brushUV) == brushUV))
					{
						_BaseLevel = 0.5f;
					}
					if(_AdaptiveBase && _BaseLevel < height)
					{
						finalheight +=  height - _BaseLevel; 
					}
					else
					{
						finalheight = clamp (finalheight, 0.0f, _BaseLevel); 
					}
				}
				else
				{
					finalheight = 0.5f;
				}

				if(finalheight<=height)
				return PackHeightmap(finalheight);
				else
				return PackHeightmap(height);
				
            }
            ENDCG
        }

        Pass    // 2 Blend Heights
        {
            Name "Blend Heights"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment BlendHeight

            float4 BlendHeight(v2f i) : SV_Target
            {
              float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float finalheight = GetFinalHeight(i);

				return PackHeightmap((finalheight * _BlendStrength) + (height * (1-_BlendStrength)));
            }
            ENDCG
        }

        Pass    // 3 Set Height
        {
            Name "Set Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SetHeight

            float4 SetHeight(v2f i) : SV_Target
            {
               float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float finalheight = GetFinalHeight(i);
				return PackHeightmap(finalheight);
            }
            ENDCG
        }

        Pass    // 4 Add Height
        {
            Name "Add Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment AddHeight

            float4 AddHeight(v2f i) : SV_Target
            {
				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				if(!oob)
				{
					return height;
				}
				float finalheight = GetFinalHeight(i);

				

				//add the existing height
				finalheight += height;

				//process baseLevel
				if(_StampBase>0 || finalheight>_BaseLevel)
				{
					if(!all(saturate(brushUV) == brushUV))
					{
						_BaseLevel = 0;
					}
					if(_AdaptiveBase && _BaseLevel > height)
					{
						finalheight -= _BaseLevel - height; 
					}
					else
					{
						finalheight = clamp (finalheight, _BaseLevel,0.5f); 
					}
				}
				else
				{
					finalheight = 0;
				}
				
				if(finalheight>=height)
				return PackHeightmap(finalheight);
				else
				return PackHeightmap(height);
				

            }
			ENDCG
        }

		 Pass    // 5 Subrtract Height
        {
            Name "Subtract Height"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SubtractHeight

            float4 SubtractHeight(v2f i) : SV_Target
            {
				//invert ypos for better usability
				_yPos*=-1;


				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				if(!oob)
				{
					return height;
				}
				float finalheight = GetFinalHeight(i);

				//subtract the existing height
				finalheight = height - finalheight;

				//process baseLevel
				if(_StampBase>0 || finalheight<_BaseLevel)
				{
					if(!all(saturate(brushUV) == brushUV))
					{
						finalheight = height;
					}
					else
					{
						if(_AdaptiveBase && _BaseLevel < height)
						{
							finalheight +=  height - _BaseLevel; 
						}
						else
						{
							finalheight = clamp (finalheight, 0.0f, _BaseLevel); 
						}
					}
				}
				else
				{
					finalheight = height;
				}

				if(finalheight<=height)
				return PackHeightmap(finalheight);
				else
				return PackHeightmap(height);
			
            }
			ENDCG
        }

		//This pass is special in so far as it relies on the heightmap changes being passed in
		//_MainTex, it will not alter the heightmap by itself

		 Pass    // 6 Effects
		 
        {
            Name "Effects"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment Effects

            float4 Effects(v2f i) : SV_Target
            {

				float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
				float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);
				float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
				float finalheight = GetFinalHeight(i);
				return PackHeightmap(finalheight);

				//float erodedHeight = UnpackHeightmap(tex2D(_ErosionTex, heightmapUV));

				 // out of bounds multiplier
                /*float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;
				//value from the image mask texture multiplied with the multiplicator
				float imageMask = UnpackHeightmap(tex2D(_ImageMaskTex, brushUV));
				if(_InvertImageMask>0)
				{
					//Invert the imagemask
					imageMask=1.0f-imageMask;
				}
				
				float distanceMask = UnpackHeightmap(tex2D(_DistanceMaskTex, smoothstep(0,sqrt(1)/2,distance(brushUV,float2(0.5f,0.5f)))));
                
				

				float globalMaskValue = 1.0f;
				globalMaskValue *= imageMask;
				globalMaskValue *= distanceMask;*/
			
			
				//only return the height value changed by the brush if we are actually in bounds
				//otherwise we get an ugly square base when rotating stamps
				/*if(all(saturate(brushUV) == brushUV))
				{
					return PackHeightmap(clamp(erodedHeight, 0, 0.5f));
				}
				else
				{
					return PackHeightmap(height);;
				}*/
				
			
            }
			ENDCG
        }

    }
    Fallback Off
}
