using System;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
public class PW_General_MaterialEditor : ShaderGUI
{
//	bool propsLoaded = false;
	MaterialEditor 				materialEditor;
	Material 					targetMat;
	
	MaterialProperty  pwShaderMode;
	MaterialProperty  cullMode;
	MaterialProperty  color;
	MaterialProperty  mainTex;
	MaterialProperty  cutoff;
	MaterialProperty  bumpMap;
	MaterialProperty  bumpMapScale;
	MaterialProperty  metallicGlossMap;
	MaterialProperty  glossiness;
	MaterialProperty  metallic;
	MaterialProperty  wrapLighting;

	MaterialProperty  aoPower;
	MaterialProperty  aoPowerExp;
	MaterialProperty  aoVertexMask;

	MaterialProperty  coverLayer0Image;
	MaterialProperty  coverLayer0Color;
	MaterialProperty  coverLayer0Tiling;
	MaterialProperty  coverLayer0Edge;
	MaterialProperty  coverLayer0AlphaClamp;
	MaterialProperty  coverLayer0Normal;
	MaterialProperty  coverLayer0NormalScale;
	MaterialProperty  coverLayer0Smoothness;
	MaterialProperty  coverLayer0Metallic;
	MaterialProperty  coverLayer0Wrap;
	MaterialProperty  coverLayer0Progress;
	MaterialProperty  coverLayer0FadeStart;
	MaterialProperty  coverLayer0FadeDist;

	MaterialProperty  coverLayer1Image;
	MaterialProperty  coverLayer1Color;
	MaterialProperty  coverLayer1Tiling;
	MaterialProperty  coverLayer1Edge;
	MaterialProperty  coverLayer1AlphaClamp;
	MaterialProperty  coverLayer1Normal;
	MaterialProperty  coverLayer1NormalScale;
	MaterialProperty  coverLayer1Smoothness;
	MaterialProperty  coverLayer1Metallic;
	MaterialProperty  coverLayer1Wrap;
	MaterialProperty  coverLayer1Progress;
	MaterialProperty  coverLayer1FadeStart;
	MaterialProperty  coverLayer1FadeDist;
	
	MaterialProperty  sssPower;
	MaterialProperty  sssDistortion;
	MaterialProperty  sssTint;
	
	MaterialProperty  windTreeWidthHeight;
	MaterialProperty  windTreeFlex;
	MaterialProperty  windTreeFrequency;
	
	MaterialProperty  worldMap;
	MaterialProperty  worldMapScale;
	MaterialProperty  worldMapColorObject;
	MaterialProperty  worldMapColorCover0;
	MaterialProperty  worldMapColorCover1;

	MaterialProperty  seasonalTintAmount;
	
	// Keyword toggles
	MaterialProperty _PW_SF_BILLBOARD;
	MaterialProperty _PW_SF_COVER;
	MaterialProperty _PW_SF_SSS;
	MaterialProperty _PW_SF_WIND;
	MaterialProperty _PW_SF_WORLDMAP;
	MaterialProperty _ALPHATEST;
	
	private bool AlphaTest_Toggle;
	private bool PW_WorldMap_Toggle;
	private bool PW_Cover_Toggle;
	private bool PW_SSS_Toggle;
	private bool PW_Wind_Toggle;
	private bool PW_Billboard_Toggle;

	public enum PW_SHADER_MODE
	{
		OBJECT_SOLID 		= 0,
		VEGETATION_CUTOUT 	= 1,
		VEGETATION_BILLBOARD =2,
	}

	public enum GAIA_SHADER_TYPE
	{
		FORWARD,
		DEFERRED,
		URP,
		HDRP
	};

	GAIA_SHADER_TYPE shaderType = GAIA_SHADER_TYPE.FORWARD;
	//-------------------------------------------------------------------------
	private void SetShaderId(MaterialProperty[] props)
	{
		var matShaderID = FindProperty("_PW_ShaderID", props);
		if (matShaderID != null)
		{
			if (GraphicsSettings.renderPipelineAsset == null) // check for SRP
			{

				var matShader = targetMat.shader.name;
				if (matShader != Gaia.ShaderUtilities.PWShaderNameUtility.ShaderName[(int) matShaderID.floatValue])
				{
					for (int i = 0; i < Gaia.ShaderUtilities.PWShaderNameUtility.ShaderName.Length; i++)
					{
						if (matShader == Gaia.ShaderUtilities.PWShaderNameUtility.ShaderName[i])
						{
							matShaderID.floatValue = i;
							break;
						}
					}
					if (matShader != Gaia.ShaderUtilities.PWShaderNameUtility.ShaderName[(int) matShaderID.floatValue])
					{
						matShaderID.floatValue = 0;
					}
				}
			}
			Header ( "Gaia Utility Data:" );
			var name = ((Gaia.ShaderUtilities.ShaderIDs)(int) matShaderID.floatValue).ToString();
			EditorGUILayout.LabelField(" Built-in Shader ID: (" + (int) matShaderID.floatValue + ") " + name);
		}
	}

	//-------------------------------------------------------------------------
	private void Toggle ( ref bool i_bool, string i_name )
	{
		i_bool = EditorGUILayout.Toggle ( i_name, i_bool );
	}

	//-------------------------------------------------------------------------
	private static void Line( int i_height = 1 )
	{
		Rect rect = EditorGUILayout.GetControlRect(false, i_height + 1);
		rect.height = i_height;
		EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
	}

	//-------------------------------------------------------------------------
	private static void Header ( string i_name, bool i_first = false )
	{
		if (!i_first)
			EditorGUILayout.LabelField (" ");

		EditorGUILayout.LabelField ( i_name );
		Line();
	}

	//-------------------------------------------------------------------------
	private void GetShaderType( Shader i_shader )
	{
		switch ( i_shader.name )
		{
		case "PWS/PW_General_Forward":
    		shaderType = GAIA_SHADER_TYPE.FORWARD;
			break;

		case "PWS/PW_General_URP":
    		shaderType = GAIA_SHADER_TYPE.URP;
			break;

		case "PWS/PW_General_HDRP":
    		shaderType = GAIA_SHADER_TYPE.HDRP;
			break;

		default:
		case "PWS/PW_General_Deferred":
			shaderType = GAIA_SHADER_TYPE.DEFERRED;
			break;
		}
	}

	//-------------------------------------------------------------------------
	private static void SetShaderFeature ( Material i_mat, bool i_toggle, string i_keyword )
	{
		if ( i_toggle )
		  i_mat.EnableKeyword ( i_keyword );
		else
		  i_mat.DisableKeyword ( i_keyword );
	}

	//-------------------------------------------------------------------------
	private void GetShaderKeywords()
	{
		PW_Wind_Toggle = targetMat.IsKeywordEnabled("_PW_SF_WIND_ON");
		PW_Cover_Toggle = targetMat.IsKeywordEnabled("_PW_SF_COVER_ON");
		PW_WorldMap_Toggle = targetMat.IsKeywordEnabled("_PW_SF_WORLDMAP_ON");
		PW_SSS_Toggle = targetMat.IsKeywordEnabled("_PW_SF_SSS_ON");
		AlphaTest_Toggle = targetMat.IsKeywordEnabled("_ALPHATEST_ON");

		if (targetMat.IsKeywordEnabled("_PW_SF_BILLBOARD_ON"))
		{
			pwShaderMode.floatValue = (int) PW_SHADER_MODE.VEGETATION_BILLBOARD;
			PW_Billboard_Toggle = true;
		}
		else
		{
			PW_Billboard_Toggle = false;
		}
	}
	 // -----------------------------------------------------------------------
	 void SetShaderModeProperties( PW_SHADER_MODE i_shadermode)
	 {
		 switch (i_shadermode)
		 {
			 case PW_SHADER_MODE.OBJECT_SOLID:
				 targetMat.renderQueue 	= (int)UnityEngine.Rendering.RenderQueue.Geometry;
				 cullMode.floatValue 	= (float)UnityEngine.Rendering.CullMode.Back;
				 PW_Billboard_Toggle = false;
				 AlphaTest_Toggle = false;
				 break;
			 
			 case PW_SHADER_MODE.VEGETATION_CUTOUT:
				 targetMat.renderQueue 	= (int)UnityEngine.Rendering.RenderQueue.AlphaTest + 10;
				 cullMode.floatValue		= (float)UnityEngine.Rendering.CullMode.Off;
				 PW_Billboard_Toggle = false;
				 AlphaTest_Toggle = true;
				 break;
				 
			 case PW_SHADER_MODE.VEGETATION_BILLBOARD:
				 targetMat.renderQueue 	= (int)UnityEngine.Rendering.RenderQueue.AlphaTest + 10;
				 cullMode.floatValue		= (float)UnityEngine.Rendering.CullMode.Back;
				 PW_Billboard_Toggle = true;
				 AlphaTest_Toggle = true;
				 break;
			 
			 default:
				 break;
		 }
	 }

	 private void SetShaderKeywords()
	{
		SetShaderFeature ( targetMat, AlphaTest_Toggle, "_ALPHATEST_ON" );
		SetShaderFeature ( targetMat, PW_Wind_Toggle , "_PW_SF_WIND_ON" );
		SetShaderFeature ( targetMat, PW_Cover_Toggle, "_PW_SF_COVER_ON" );
		SetShaderFeature ( targetMat, PW_WorldMap_Toggle, "_PW_SF_WORLDMAP_ON" );
		SetShaderFeature ( targetMat, PW_SSS_Toggle, "_PW_SF_SSS_ON" );
		SetShaderFeature ( targetMat, PW_Billboard_Toggle, "_PW_SF_BILLBOARD_ON");
		
	}

	 private void SetShaderKeywordsToggles()
	 {
		 _ALPHATEST.floatValue = Convert.ToInt16(AlphaTest_Toggle);
		 _PW_SF_WIND.floatValue = Convert.ToInt16(PW_Wind_Toggle);
		 _PW_SF_COVER.floatValue = Convert.ToInt16(PW_Cover_Toggle);
		 _PW_SF_WORLDMAP.floatValue = Convert.ToInt16(PW_WorldMap_Toggle);
		 _PW_SF_SSS.floatValue = Convert.ToInt16(PW_SSS_Toggle);
		 _PW_SF_BILLBOARD.floatValue = Convert.ToInt16(PW_Billboard_Toggle);
	 }

	//-------------------------------------------------------------------------
	private void FindProperties(MaterialProperty[] props)
	{
		pwShaderMode = FindProperty("_PW_ShaderMode", props);

		cullMode = FindProperty("_CullMode", props);
		color = FindProperty("_Color", props);
		mainTex = FindProperty("_MainTex", props);
		cutoff = FindProperty("_Cutoff", props);
		bumpMap = FindProperty("_BumpMap", props);
		bumpMapScale = FindProperty("_BumpMapScale", props);
		metallicGlossMap = FindProperty("_MetallicGlossMap", props);
		glossiness = FindProperty("_Glossiness", props);
		metallic = FindProperty("_Metallic", props);
		wrapLighting = FindProperty("_WrapLighting", props);

		aoPower = FindProperty("_AOPower", props);
		aoPowerExp = FindProperty("_AOPowerExp", props);
		aoVertexMask = FindProperty("_AOVertexMask", props);


		coverLayer0Image = FindProperty("_PW_CoverLayer0", props);
		coverLayer0Color = FindProperty("_PW_CoverLayer0Color", props);
		coverLayer0Edge = FindProperty("_PW_CoverLayer0Edge", props);
		coverLayer0Tiling = FindProperty("_PW_CoverLayer0Tiling", props);
		coverLayer0AlphaClamp = FindProperty("_PW_CoverLayer0AlphaClamp", props);
		coverLayer0Normal = FindProperty("_PW_CoverLayer0Normal", props);
		coverLayer0NormalScale = FindProperty("_PW_CoverLayer0NormalScale", props);
		coverLayer0Smoothness = FindProperty("_PW_CoverLayer0Smoothness", props);
		coverLayer0Metallic = FindProperty("_PW_CoverLayer0Metallic", props);
		coverLayer0Wrap = FindProperty("_PW_CoverLayer0Wrap", props);
		coverLayer0Progress = FindProperty("_PW_CoverLayer0Progress", props);
		coverLayer0FadeStart = FindProperty("_PW_CoverLayer0FadeStart", props);
		coverLayer0FadeDist = FindProperty("_PW_CoverLayer0FadeDist", props);

		coverLayer1Image = FindProperty("_PW_CoverLayer1", props);
		coverLayer1Color = FindProperty("_PW_CoverLayer1Color", props);
		coverLayer1Edge = FindProperty("_PW_CoverLayer1Edge", props);
		coverLayer1Tiling = FindProperty("_PW_CoverLayer1Tiling", props);
		coverLayer1AlphaClamp = FindProperty("_PW_CoverLayer1AlphaClamp", props);
		coverLayer1Normal = FindProperty("_PW_CoverLayer1Normal", props);
		coverLayer1NormalScale = FindProperty("_PW_CoverLayer1NormalScale", props);
		coverLayer1Smoothness = FindProperty("_PW_CoverLayer1Smoothness", props);
		coverLayer1Metallic = FindProperty("_PW_CoverLayer1Metallic", props);
		coverLayer1Wrap = FindProperty("_PW_CoverLayer1Wrap", props);
		coverLayer1Progress = FindProperty("_PW_CoverLayer1Progress", props);
		coverLayer1FadeStart = FindProperty("_PW_CoverLayer1FadeStart", props);
		coverLayer1FadeDist = FindProperty("_PW_CoverLayer1FadeDist", props);

		worldMap = FindProperty("_PW_WorldMap", props);
		worldMapScale = FindProperty("_PW_WorldMapUVScale", props);
		worldMapColorObject = FindProperty("_PW_WorldMapColorObject", props);
		worldMapColorCover0 = FindProperty("_PW_WorldMapColorCover0", props);
		worldMapColorCover1 = FindProperty("_PW_WorldMapColorCover1", props);

		windTreeWidthHeight = FindProperty("_PW_WindTreeWidthHeight", props);
		windTreeFlex = FindProperty("_PW_WindTreeFlex", props);
		windTreeFrequency = FindProperty("_PW_WindTreeFrequency", props);

		seasonalTintAmount = FindProperty("_PW_Global_SeasonalTintAmount", props);

		sssPower = FindProperty("_PW_SSSPower", props);
		sssDistortion = FindProperty("_PW_SSSDistortion", props);
		sssTint = FindProperty("_PW_SSSTint", props);

		// Keyword Toggles
		_PW_SF_BILLBOARD = FindProperty("_PW_SF_BILLBOARD", props);
		_PW_SF_COVER = FindProperty("_PW_SF_COVER", props);
		_PW_SF_SSS = FindProperty("_PW_SF_SSS", props);
		_PW_SF_WIND = FindProperty("_PW_SF_WIND", props);
		_PW_SF_WORLDMAP = FindProperty("_PW_SF_WORLDMAP", props);
		_ALPHATEST = FindProperty("_ALPHATEST", props);
	}


	//-------------------------------------------------------------------------
	private void CoverGUI()
	{
		Header ( "Cover options:" );
		Toggle ( ref PW_Cover_Toggle,  "enabled" );

		if ( PW_Cover_Toggle )
		{

			EditorGUILayout.LabelField (" ");
			EditorGUILayout.LabelField ("Layer 0:");
			EditorGUI.indentLevel++;

			materialEditor.TexturePropertySingleLine ( new GUIContent( "Texture", "Cover Texture (A) Dissolve"), coverLayer0Image, coverLayer0Color );
			materialEditor.TexturePropertySingleLine ( new GUIContent( "Normal", ""), coverLayer0Normal, coverLayer0NormalScale );
			materialEditor.DefaultShaderProperty ( coverLayer0Progress, "Progress" );
			materialEditor.ShaderProperty ( coverLayer0Edge, new GUIContent("Edge", "Smooth Edge of dissolve" ));
			materialEditor.ShaderProperty ( coverLayer0Tiling, new GUIContent("Tiling" ,"How many times texture repeats across Terrain"));
			materialEditor.DefaultShaderProperty ( coverLayer0Wrap, "Wrap" );
			materialEditor.DefaultShaderProperty ( coverLayer0AlphaClamp, "Alpha Mod" );
			materialEditor.DefaultShaderProperty ( coverLayer0Metallic, "Metallic" );
			materialEditor.DefaultShaderProperty ( coverLayer0Smoothness, "Smoothness" );
			materialEditor.DefaultShaderProperty ( coverLayer0FadeStart, "Fade Start" );
			materialEditor.DefaultShaderProperty ( coverLayer0FadeDist, "Fade Distance" );
			EditorGUI.indentLevel--;

			EditorGUILayout.LabelField (" ");
			EditorGUILayout.LabelField ("Layer 1:");
			EditorGUI.indentLevel++;

			materialEditor.TexturePropertySingleLine ( new GUIContent( "Texture", "Cover Texture (A) Dissolve"), coverLayer1Image, coverLayer1Color );
			materialEditor.TexturePropertySingleLine ( new GUIContent( "Normal", ""), coverLayer1Normal, coverLayer1NormalScale );
			materialEditor.DefaultShaderProperty ( coverLayer1Progress, "Progress" );
			materialEditor.ShaderProperty ( coverLayer1Edge, new GUIContent("Edge", "Smooth Edge of dissolve" ));
			materialEditor.ShaderProperty ( coverLayer1Tiling, new GUIContent("Tiling" ,"How many times texture repeats across Terrain"));
			materialEditor.DefaultShaderProperty ( coverLayer1Wrap, "Wrap" );
			materialEditor.DefaultShaderProperty ( coverLayer1AlphaClamp, "Alpha Mod" );
			materialEditor.DefaultShaderProperty ( coverLayer1Metallic, "Metallic" );
			materialEditor.DefaultShaderProperty ( coverLayer1Smoothness, "Smoothness" );
			materialEditor.DefaultShaderProperty ( coverLayer1FadeStart, "Fade Start" );
			materialEditor.DefaultShaderProperty ( coverLayer1FadeDist, "Fade Distance" );
			EditorGUI.indentLevel--;
		}
	}

	//-------------------------------------------------------------------------
	private void SSSGUI()
	{
		Header ( "Translucent (SSS) options:" );
		Toggle ( ref PW_SSS_Toggle, "enabled" );

		if (PW_SSS_Toggle )
		{
			materialEditor.DefaultShaderProperty ( sssTint, "Tint" );
			materialEditor.DefaultShaderProperty ( sssPower, "Power" );
			materialEditor.DefaultShaderProperty ( sssDistortion, "Distortion" );
		}
	}

	//-------------------------------------------------------------------------
	private void WindGUI()
	{
		Header ( "Wind options:" );
		Toggle ( ref PW_Wind_Toggle, "enabled" );

		if ( PW_Wind_Toggle)
		{
			var widthHeight = new Vector2(windTreeWidthHeight.vectorValue.x, windTreeWidthHeight.vectorValue.y);
			widthHeight = EditorGUILayout.Vector2Field("Scale: Width, Height", widthHeight);
			windTreeWidthHeight.vectorValue = new Vector4(widthHeight.x,widthHeight.y,0,0);
			
			var flex = new Vector3(windTreeFlex.vectorValue.x,windTreeFlex.vectorValue.y,windTreeFlex.vectorValue.z);
			flex = EditorGUILayout.Vector3Field(" Flex: Stem, Branch, Leaf", flex);
			windTreeFlex.vectorValue = new Vector4(flex.x,flex.y,flex.z,0);
			
			var frequency = new Vector3(windTreeFrequency.vectorValue.x,windTreeFrequency.vectorValue.y,windTreeFrequency.vectorValue.z);
			frequency = EditorGUILayout.Vector3Field(" Frequency: Stem, Branch, Leaf", frequency);
			windTreeFrequency.vectorValue = new Vector4(frequency.x,frequency.y,frequency.z,0);
		}
	}

	//-------------------------------------------------------------------------
	private void WorldMapGUI()
	{
		Header ( "World Mapping options:" );
		Toggle ( ref PW_WorldMap_Toggle, "enabled" );

		if (PW_WorldMap_Toggle )
		{
			materialEditor.TexturePropertySingleLine ( new GUIContent( "World Map", "VariationMap (R) Object Amount (G) Cover Amount (B) unused (A) unused\n No World Map makes it use vertex colors as map"), worldMap );
			materialEditor.DefaultShaderProperty ( worldMapColorObject, "Object" );
			materialEditor.DefaultShaderProperty ( worldMapColorCover0, "Cover Layer 0" );
			materialEditor.DefaultShaderProperty ( worldMapColorCover1, "Cover Layer 1" );
			materialEditor.DefaultShaderProperty ( worldMapScale, "Tiling" );
		}
	}

	//-------------------------------------------------------------------------
	private void MainGUI()
	{
		string addScale = "";

		Header ( "Main options:", true );

		materialEditor.EnableInstancingField();

		pwShaderMode.floatValue = (float)(PW_SHADER_MODE)EditorGUILayout.EnumPopup("Mode", (PW_SHADER_MODE)pwShaderMode.floatValue );
		
		if (AlphaTest_Toggle)
		{
			materialEditor.DefaultShaderProperty ( cutoff, "Alpha Cutoff" );
		}
		
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Albedo", "Main Albedo Texture & Tint"), mainTex, color );
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Normal", "Normal Map & Normal Map Power"), bumpMap, bumpMapScale );
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Mask", "(R)Metallic (G)AO (B)Thickness (A)Smoothness"), metallicGlossMap );

		if (metallicGlossMap.textureValue != null)
			addScale = "(S)";

        materialEditor.ShaderProperty(metallic, new GUIContent("Metallic " + addScale, "Metallic - influenced by Mask if present"));
        materialEditor.ShaderProperty(glossiness, new GUIContent("Smoothness " + addScale, "Smoothness - influenced by Mask if present"));

		materialEditor.ShaderProperty(wrapLighting, new GUIContent("Side Lighting", "Lights sides perpendicular to Light source"));

        materialEditor.ShaderProperty(aoPower, new GUIContent("Occlusion", "Occlusion - Data Mask(G) * Vertex(R)"));
		materialEditor.ShaderProperty(aoPowerExp, new GUIContent("Occlusion Power", "Occlusion - Data Mask(G) * Vertex(R)"));
		materialEditor.ShaderProperty(aoVertexMask, new GUIContent("Vertex To Mask", "0=Vertex Color (R) 1=DataMask(G)"));


		materialEditor.ShaderProperty(seasonalTintAmount, new GUIContent("Seasonal Tint", "Seasonal tint influence at runtime"));

        materialEditor.TextureScaleOffsetProperty(mainTex);
        
        //materialEditor.ShaderProperty(cullMode, new GUIContent("Cull Mode", "Cull by by facing"));
        
        SetShaderModeProperties((PW_SHADER_MODE)(int)pwShaderMode.floatValue);
	}

	//=====================================================================
	public override void OnGUI ( MaterialEditor i_materialEditor, MaterialProperty[] i_properties )
	{
		materialEditor 	= i_materialEditor;
		targetMat 		= i_materialEditor.target as Material;
		
		GetShaderType ( targetMat.shader );
		FindProperties ( i_properties );
		
		GetShaderKeywords();
		
		EditorGUI.BeginChangeCheck();

		MainGUI();
		CoverGUI();

		if (shaderType == GAIA_SHADER_TYPE.URP || shaderType == GAIA_SHADER_TYPE.FORWARD)
		{
			SSSGUI();
		}
		
		WindGUI();
		WorldMapGUI();

		SetShaderKeywordsToggles();

		SetShaderId(i_properties);
		
		if (EditorGUI.EndChangeCheck())
		{
			SetShaderKeywords();
		}
		
	}
}

