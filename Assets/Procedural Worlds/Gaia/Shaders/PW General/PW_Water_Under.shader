Shader "PWS/PW_Water_Under"
{
    Properties
    {
		_WaveShoreClamp ("Shore Movment", Range ( 0.0, 1.0 ) ) 					= 0.1
		_WaveLength ("Wave Length", Float ) 									= 64
		_WaveSteepness ("Wave Steepness", Float ) 								= 0.3
		_WaveSpeed ("Wave Speed", Float ) 										= 0.4
		_WaveDirection ("Wave Direction", Vector ) 								= (1,0,0,0)        	
		_WaveDirGlobal ("WaveDirToggle", Float )								= 1  
		_WaveBackwashToggle ("Wave Backwash Toggle", Float )					= 1  
		_WavePeakToggle ("Wave Peak Toggle", Float )							= 1  
		_RefractionToggle ("Refraction", Float )								= 1  

		[NoScaleOffset][Normal] _NormalLayer0 ("Normal Layer 0", 2D) 			= "bump" {}	
		_NormalLayer0Scale ("NormalLayer0 Scale", Range ( 0.0, 3.0 ) ) 			= 1.0
		[NoScaleOffset][Normal] _NormalLayer1 ("Normal Layer 1", 2D)			= "bump" {}	
		_NormalLayer1Scale ("NormalLayer1 Scale", Range ( 0.0, 3.0 ) ) 			= 1.0

		_NormalTile ("Water Tiling", Float ) 									= 128

		_UnderWaterColor ("UnderWater Color", Color) 							= (0.7,0.8,1.0,1)
		_EdgeWaterColor ("UnderWater Color", Color) 							= (0.1,0.1,0.1,1)
		_EdgeWaterDist  ("Edge Fade Distance", Float )							= 5  
    }

    SubShader
    {
        Tags {"Queue" = "Transparent+1" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
			Cull Front
			ZWrite Off

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
			#include "UnityPBSLighting.cginc"
			#include "PW_GeneralVars.cginc"
			#include "PW_GeneralFuncs.cginc"
			#include "PW_Water_Vars.cginc"
			#include "PW_Water_Funcs.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal   : NORMAL;
				float4 tangent  : TANGENT;
            };

            struct v2f
            {
				float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
				float4 screenPos	: TEXCOORD1;
				float3 worldPos 	: TEXCOORD7;
				float2 bumpuv0 		: TEXCOORD2;
				float2 bumpuv1 		: TEXCOORD3;
				half3 tspace0 		: TEXCOORD8; 
				half3 tspace1 		: TEXCOORD9; 
				half3 tspace2 		: TEXCOORD10;
                UNITY_FOG_COORDS(11)
            };

            v2f vert (appdata v)
            {
				v2f o;

				o =(v2f)0;

//				float _MinOffset = 0.02;
//				float _OffsetAmount = 0.1;

				float4 screenPos 		= ComputeScreenPos(UnityObjectToClipPos(v.vertex));
				float sceneZ 			= LinearEyeDepth (tex2Dlod(_CameraDepthTexture, float4(screenPos.xy / screenPos.w, 0.0, 0.0)).r);
				float objectZ 			= screenPos.w;
				float shoreClamp 		= clamp ( saturate( (sceneZ - objectZ) / 32 ) + _WaveShoreClamp, 0.0, 1.0 );    

				float2 worldUVScale;
				worldUVScale.x = PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;
				worldUVScale.y = PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;

				// before the displacment
				float3 worldPos = mul ((float3x3)unity_ObjectToWorld, v.vertex.xyz );
				o.worldPos = worldPos;

				float3 v0 = worldPos;
				float2 waveDir;

#ifdef _PW_SF_WAVEDIR_GLOBAL_ON
			waveDir = _PW_Global_WindDirection.xz;
#else
			waveDir = _WaveDirection.xz;
#endif

				float steepness = _WaveSteepness;
				float waveLength = _WaveLength * PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;

				v0.y += EchoWave ( v0, waveDir, waveLength, _WaveSpeed, steepness, shoreClamp );
				float2 perp;
				perp.x = waveDir.y;
				perp.y = -waveDir.x;

				v0.y += EchoWave ( v0, perp, waveLength * 0.25, -_WaveSpeed, steepness * 0.66, shoreClamp );

				waveDir = normalize ( ( perp + waveDir ) * 0.5 );
				v0.y += EchoWave ( v0, waveDir, waveLength, _WaveSpeed, steepness * 0.33, shoreClamp );

				v.vertex.xyz = mul ((float3x3)unity_WorldToObject, v0 );
			
				o.vertex 		= UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos( UnityObjectToClipPos ( v.vertex ) );

				worldPos = mul ( unity_ObjectToWorld, v.vertex );

				float2 worldMapUV	= float2 ( 1.0, 1.0 ) - ( ( worldPos.xz * worldUVScale ) + half2 ( 0.5, 0.5 ) );

				float timeScale = _Time.x * ( _WaveSpeed * 0.00033 );

				float2 t; 
				t.x = waveDir.x * timeScale;
				t.y = waveDir.y * timeScale;

				o.bumpuv0 = ( worldMapUV - t ) * _NormalTile;
				o.bumpuv1 = ( worldMapUV + t ) * _NormalTile * 0.333;

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 col = half4(1,1,1,1);

				half3 bump1 = UnpackScaleNormal(tex2D( _NormalLayer0, i.bumpuv0 ),_NormalLayer0Scale );
				half3 bump2 = UnpackScaleNormal(tex2D( _NormalLayer1, i.bumpuv1 ), _NormalLayer1Scale );

				half3 bump  = BlendUnpacked( bump1, bump2 );

				half2 uvOffset 				= bump.xy * 0.5;
				half2 uv 					= (i.screenPos.xy + uvOffset) / i.screenPos.w;
				half3 underWaterColor	 	= tex2D( _CameraOpaqueTexture, uv ).rgb;

				col.rgb = underWaterColor;// * _UnderWaterColor;

				half dist2Cam 		= distance(_WorldSpaceCameraPos.xyz, i.worldPos );

				//col.rgb = WaterEdge ( col.rgb, i.worldPos, dist2Cam, 2 );

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
            	UNITY_APPLY_FOG(i.fogCoord, col);
            	UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
	CustomEditor "PW_WaterUnderMaterialEditor"
}
