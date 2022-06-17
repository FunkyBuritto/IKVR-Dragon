#ifndef PW_GENERALFUNCSWIND
#define PW_GENERALFUNCSWIND

inline void WindCalculations_float ( in float3 i_vertexPos, in float2 i_widthHeight, in float3 i_flex, in float3 i_frequency, out float3 o_vertexPos )
{
	#ifdef _PW_SF_WIND_ON
	// early out test
	float3 worldOffset = float3(unity_ObjectToWorld._m03,unity_ObjectToWorld._m13,unity_ObjectToWorld._m23); // grab pos from matrix translation

	// cheeky normalised square dist 
	float3 nrmObjDistVector = (worldOffset - _WorldSpaceCameraPos) / _PW_WindGlobalsB.x;
	float nrmSqrdDist = 1 - saturate(dot(nrmObjDistVector,nrmObjDistVector));
	nrmSqrdDist *= nrmSqrdDist;
	o_vertexPos = i_vertexPos; // prime out with pre wind data

	if(nrmSqrdDist > 0.0f)
	{
		// Just for clarity
		float3 windDir = _PW_WindGlobals.xyz;
		float windMain = _PW_WindGlobals.w;

		// curves for mixing flex by wind main
		float3 flex = i_flex * float3(saturate(windMain * 3),saturate(windMain * 2),1-windMain * windMain * 0.5) * windMain * nrmSqrdDist;

		// normalize effects by height scale and reduce effects when object is on its side.
		float3 up = float3(0,1,0);
		float3 zScaleRotVec = mul((float3x3)unity_ObjectToWorld,up);
		float3 zScaleRotVecNrm = normalize(zScaleRotVec);
		float xDot = max(dot(zScaleRotVec,zScaleRotVecNrm),0.4f); // clamp to 0.4 so really small scales dont become overly flexible
		float upDot = saturate(dot(zScaleRotVecNrm,up));
		i_widthHeight.xy = lerp(i_widthHeight.yy * float2(2,2),i_widthHeight.xy,upDot*upDot) * xDot;
		flex *= xDot;
		i_frequency *= xDot;
		
		// split translation from scale and rotation for later use
		float3 localWorldPos = mul((float3x3)unity_ObjectToWorld,i_vertexPos.xyz);
		float3 worldPos = localWorldPos + worldOffset;
		float windTime = -frac(_PW_WindGlobalsB.y * 6.0f) *  6.283185f;

		float3 normLocalWorldPos = localWorldPos / float3(i_widthHeight.x,i_widthHeight.y,i_widthHeight.x);
		float branchdist = dot(normLocalWorldPos.xz , normLocalWorldPos.xz);
		float stemheight = normLocalWorldPos.y; 

		// approximate length before flex
		float lengthA = dot(localWorldPos,localWorldPos);
	
		// Main wind force
		float gust = ((sin(windTime + dot(i_frequency.xxx,worldOffset)) * 0.3 + windMain * 0.5) + (_SinTime.y * 0.4 + windMain) * windMain) * (_SinTime.w * 0.3 + 0.7);

		// trunk flex
		float3 flexTally = 0;
		flexTally.xz = windDir.xz * (stemheight * stemheight * gust * flex.x);

		// scale gust for branch and leaf
		gust = gust * 0.7 + 0.3;

		// branch flex
		#ifndef _PW_SF_BILLBOARD_ON
		flexTally.xyz += windDir.xyz * stemheight * stemheight * (sin(windTime  * 2 + dot(worldPos + flexTally * 0.25,float3(1,1,1)) * i_frequency.y) *  branchdist * 0.7 + 0.3) * gust * flex.y;  // - flextally adds more noise when stem flex's
		#endif

		//normalize flex to maintain tree volume
		float3 vertOffset = localWorldPos + flexTally;
		float flexNorm =  saturate(lengthA/dot(vertOffset,vertOffset));
		flexTally = vertOffset * flexNorm; 

		// add to tree
		localWorldPos = flexTally;

		// leaf flex 
		#ifndef _PW_SF_BILLBOARD_ON
		#ifdef _ALPHATEST_ON 
		float leafTime = windTime * 5;
		float3 frequencyOffests = (worldPos + flexTally) * i_frequency.z;
		float3 leafBranchdist = float3(branchdist,branchdist * 0.75f,branchdist);
		float3 leafFlex = (sin(leafTime.xxx + frequencyOffests) * windDir.xyz + windDir.xyz) * leafBranchdist;
		localWorldPos += leafFlex * (stemheight * gust * flex.z) * (flexNorm + 0.5f);
		#endif
		#endif

		o_vertexPos = mul((float3x3)unity_WorldToObject,localWorldPos.xyz);
	}
	
#else
	o_vertexPos = i_vertexPos;
#endif
}

#endif // PW_GENERALFUNCS



