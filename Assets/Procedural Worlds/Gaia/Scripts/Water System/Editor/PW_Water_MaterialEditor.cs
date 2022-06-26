using System;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;


[CanEditMultipleObjects]
public class PW_Water_MaterialEditor : ShaderGUI
{
//	bool propsLoaded = false;
	MaterialEditor 				materialEditor;
	Material 					targetMat;

	MaterialProperty  ambientColor;
	MaterialProperty  mainLightDir;
	MaterialProperty  mainLightColor;
	MaterialProperty  mainLightSpecular;
	MaterialProperty  metallic;
	MaterialProperty  smoothness;
	MaterialProperty  reflectionTex;
	MaterialProperty  reflectionColor;
//	MaterialProperty  reflectionDistortion;
//	MaterialProperty  reflectionStrength;

	MaterialProperty  normalLayer0;
	MaterialProperty  normalLayer0Scale;
	MaterialProperty  normalLayer1;
	MaterialProperty  normalLayer1Scale;
	MaterialProperty  normalLayer2;
	MaterialProperty  normalLayer2Scale;
	MaterialProperty  normalTile;
	MaterialProperty  normalFadeMap;
	MaterialProperty  normalFadeStart;
	MaterialProperty  normalFadeDist;

	MaterialProperty  foamTex;
	MaterialProperty  foamTexTile;
	MaterialProperty  foamDepth;
	MaterialProperty  foamStrength;
	MaterialProperty  waterDepthRamp;
	MaterialProperty  transparentDepth;
	MaterialProperty  transparentMin;

	MaterialProperty  waveLength;
	MaterialProperty  waveSteepness;
	MaterialProperty  waveSpeed;
	MaterialProperty  waveDirection;
	MaterialProperty  waveShoreClamp;
	MaterialProperty  waveDirGlobal;
	MaterialProperty  waveBackwashToggle;
	MaterialProperty  wavePeakToggle;

	MaterialProperty  sssToggle;
	MaterialProperty  sssPower;
	MaterialProperty  sssDistortion;
	MaterialProperty  sssTint;

	MaterialProperty  edgeWaterColor;
	MaterialProperty  edgeWaterDist;

	string _toolTip ="";

	//-------------------------------------------------------------------------
	private void FindProperties ( MaterialProperty[] props )
	{
		ambientColor			= FindProperty ( "_AmbientColor", props );
		mainLightDir			= FindProperty ( "_PW_MainLightDir", props );
		mainLightColor			= FindProperty ( "_PW_MainLightColor", props );
		mainLightSpecular 		= FindProperty ( "_PW_MainLightSpecular", props );

		metallic 				= FindProperty ( "_Metallic", props );
		smoothness				= FindProperty ( "_Smoothness", props );
		//reflectionTex      		= FindProperty ( "_ReflectionTex", props );
		reflectionColor      	= FindProperty ( "_ReflectionColor", props );
	   // reflectionDistortion 	= FindProperty ( "_ReflectionDistortion", props );
	   // reflectionStrength   	= FindProperty ( "_ReflectionStrength", props );

		normalLayer0			= FindProperty ( "_NormalLayer0", props );
		normalLayer0Scale		= FindProperty ( "_NormalLayer0Scale", props );
		normalLayer1			= FindProperty ( "_NormalLayer1", props );
		normalLayer1Scale		= FindProperty ( "_NormalLayer1Scale", props );
		normalLayer2			= FindProperty ( "_NormalLayer2", props );
		normalLayer2Scale		= FindProperty ( "_NormalLayer2Scale", props );
		normalTile				= FindProperty ( "_NormalTile", props );
		normalFadeMap			= FindProperty ( "_NormalFadeMap", props );
		normalFadeStart			= FindProperty ( "_NormalFadeStart", props );
		normalFadeDist			= FindProperty ( "_NormalFadeDistance", props );
//		normalMoveSpeed			= FindProperty ( "_NormalMoveSpeed", props );

		foamTex					= FindProperty ( "_FoamTex", props );
		foamTexTile				= FindProperty ( "_FoamTexTile", props );
		foamDepth				= FindProperty ( "_FoamDepth", props );
		foamStrength			= FindProperty ( "_FoamStrength", props );
		waterDepthRamp          = FindProperty ( "_WaterDepthRamp", props );
		transparentDepth        = FindProperty ( "_TransparentDepth", props );
		transparentMin          = FindProperty ( "_TransparentMin", props );

		waveDirGlobal        	= FindProperty ( "_WaveDirGlobal", props );
		waveBackwashToggle     	= FindProperty ( "_WaveBackwashToggle", props );
		wavePeakToggle     		= FindProperty ( "_WavePeakToggle", props );
		waveLength        		= FindProperty ( "_WaveLength", props );
		waveSteepness      		= FindProperty ( "_WaveSteepness", props );
		waveSpeed				= FindProperty ( "_WaveSpeed", props );
		waveDirection			= FindProperty ( "_WaveDirection", props );
		waveShoreClamp			= FindProperty ( "_WaveShoreClamp", props );

		sssToggle 				= FindProperty ( "_PW_SSS" , props );
		sssPower 				= FindProperty ( "_PW_SSSPower" , props );
		sssDistortion 			= FindProperty ( "_PW_SSSDistortion" , props );
		sssTint 				= FindProperty ( "_PW_SSSTint" , props );

		edgeWaterColor			= FindProperty ( "_EdgeWaterColor" , props );
		edgeWaterDist			= FindProperty ( "_EdgeWaterDist" , props );
	}

	//-------------------------------------------------------------------------
	private void Toggle ( MaterialProperty i_matProp, string i_name )
	{
		bool flag = false;

		if ( i_matProp.floatValue > 0.0f )
			flag = true;

		flag = EditorGUILayout.Toggle ( i_name, flag );

		if ( flag )
			i_matProp.floatValue = 1;
		else
			i_matProp.floatValue = 0;
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
	private static void SetShaderFeature ( Material i_mat, bool i_toggle, string i_keyword )
	{
		if ( i_toggle )
		  i_mat.EnableKeyword ( i_keyword );
	  else
		  i_mat.DisableKeyword ( i_keyword );
	}

	//-------------------------------------------------------------------------
	private void SetShaderKeywords()
	{
		SetShaderFeature ( targetMat, sssToggle.floatValue > 0.01f, "_PW_SF_SSS_ON" );
		SetShaderFeature ( targetMat, waveDirGlobal.floatValue > 0.01f, "_PW_SF_WAVEDIR_GLOBAL_ON" );
		SetShaderFeature ( targetMat, waveBackwashToggle.floatValue > 0.01f, "_PW_SF_WAVE_BACKWASH_ON" );
		SetShaderFeature ( targetMat, wavePeakToggle.floatValue > 0.01f, "_PW_SF_WAVE_PEAK_ON" );
	}

	//-------------------------------------------------------------------------
	private void LightingGUI()
	{
		Header ( "Main Directional Light:", true );

		EditorGUI.indentLevel++;

		_toolTip = "Main light directional vector";
		materialEditor.ShaderProperty ( mainLightDir, new GUIContent( "Direction", _toolTip ) );
		EditorGUILayout.LabelField (" ");

		_toolTip = "Light Color and intesity ";
		materialEditor.ShaderProperty ( mainLightColor, new GUIContent( "Color", _toolTip ) );

		_toolTip = "Light Specular color";
		materialEditor.ShaderProperty ( mainLightSpecular, new GUIContent( "Specular", _toolTip ) );

		_toolTip = "Ambient Light";
		materialEditor.ShaderProperty ( ambientColor, new GUIContent( "Ambient Light", _toolTip ) );

		EditorGUI.indentLevel--;
	}

	//-------------------------------------------------------------------------
	private void WaterSurfaceGUI()
	{
		Header ( "Water Surface:" );

		EditorGUI.indentLevel++;

		_toolTip = "Tooltip desc";
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Layer 0", _toolTip ), normalLayer0, normalLayer0Scale );
		_toolTip = "Tooltip desc";
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Layer 1", _toolTip ), normalLayer1, normalLayer1Scale );
		_toolTip = "Tooltip desc";
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Layer Fade", _toolTip ), normalLayer2, normalLayer2Scale );

		_toolTip = "Tooltip desc";
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Scale Map", _toolTip ), normalFadeMap );

//		_toolTip = "Tooltip desc";
//		materialEditor.ShaderProperty ( normalMoveSpeed, new GUIContent( "Move Speed", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( normalTile, new GUIContent( "Tiling", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( normalFadeStart, new GUIContent( "Fade Start", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( normalFadeDist, new GUIContent( "Fade Distance", _toolTip ) );

		EditorGUI.indentLevel--;
	}

	//-------------------------------------------------------------------------
	private void WaterEdgeGUI()
	{
		Header ( "Water Edge:" );

		_toolTip = "color of water edge near camera";
		materialEditor.ShaderProperty ( edgeWaterColor, new GUIContent( "Color", _toolTip ) );

		_toolTip = "Distance From Camera";
		materialEditor.ShaderProperty ( edgeWaterDist, new GUIContent( "Distance", _toolTip ) );
	}

	//-------------------------------------------------------------------------
	private void SSSGUI()
	{
		Header ( "Translucent:" );
		Toggle ( sssToggle, "enabled" );

		if ( sssToggle.floatValue > 0.0f )
		{
			materialEditor.DefaultShaderProperty ( sssTint, "Tint" );
			materialEditor.DefaultShaderProperty ( sssPower, "Power" );
			materialEditor.DefaultShaderProperty ( sssDistortion, "Distortion" );
		}
	}


	//-------------------------------------------------------------------------
	private void PBRGUI()
	{
		Header ( "PBR:" );

		EditorGUI.indentLevel++;

		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( metallic, new GUIContent( "Metallic", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( smoothness, new GUIContent( "Smoothness", _toolTip ) );

		EditorGUI.indentLevel--;
	}

	//-------------------------------------------------------------------------
	private void ReflectionGUI()
	{
		Header ( "Reflection:" );

		EditorGUI.indentLevel++;

		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( reflectionColor, new GUIContent( "Color", _toolTip ) );
//		_toolTip = "Tooltip desc";
//		materialEditor.ShaderProperty ( reflectionStrength, new GUIContent( "Strength", _toolTip ) );
//		_toolTip = "Tooltip desc";
//		materialEditor.ShaderProperty ( reflectionDistortion, new GUIContent( "Distortion", _toolTip ) );

		EditorGUI.indentLevel--;
	}

	//-------------------------------------------------------------------------
	private void WavesGUI()
	{
		Header ( "Waves:" );

		EditorGUI.indentLevel++;

		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( waveLength, new GUIContent( "Wavelength", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( waveSteepness, new GUIContent( "Steepness", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( waveSpeed, new GUIContent( "Speed", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( waveShoreClamp, new GUIContent( "Shore Clamp", _toolTip ) );
		_toolTip = "Tooltip desc";

//		Toggle ( waveBackwashToggle, "Backwash Foam" );
//		Toggle ( wavePeakToggle, "Peak Foam" );

		EditorGUILayout.LabelField (" ");
		Toggle ( waveDirGlobal, "System Direction" );

		if (waveDirGlobal.floatValue < 0.9) 
		{
			_toolTip = "Tooltip desc";
			materialEditor.ShaderProperty ( waveDirection, new GUIContent( "Direction", _toolTip ) );
		}


		EditorGUI.indentLevel--;
	}

	//-------------------------------------------------------------------------
	private void FoamGUI()
	{
		Header ( "Foam:" );

		EditorGUI.indentLevel++;
		_toolTip = "Tooltip desc";
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Foam", _toolTip ), foamTex );

		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( foamTexTile, new GUIContent( "Tiling", _toolTip ) );

		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( foamDepth, new GUIContent( "Depth", _toolTip ) );
		EditorGUI.indentLevel--;

		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( foamStrength, new GUIContent( "Strength", _toolTip ) );
		EditorGUI.indentLevel--;
	}

	//-------------------------------------------------------------------------
	private void DepthGUI()
	{
		Header ( "Water Depth:" );

		EditorGUI.indentLevel++;

		_toolTip = "Tooltip desc";
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Depth Ramp", _toolTip ), waterDepthRamp );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( transparentDepth, new GUIContent( "Depth", _toolTip ) );
		materialEditor.ShaderProperty ( transparentMin, new GUIContent( "Minimum Alpha", _toolTip ) );

		EditorGUI.indentLevel--;
	}

	//-------------------------------------------------------------------------
	/*
	private void DataMapGUI()
	{
		Header ( "DataMap:" );

		EditorGUI.indentLevel++;

		_toolTip = "Tooltip desc";
		materialEditor.TexturePropertySingleLine ( new GUIContent( "Data Map", _toolTip ), dataMap );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( dataMapOffsetX, new GUIContent( "Offset X", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( dataMapOffsetZ, new GUIContent( "Offset Z", _toolTip ) );
		_toolTip = "Tooltip desc";
		materialEditor.ShaderProperty ( dataMapPointScale, new GUIContent( "Point Scale", _toolTip ) );

		EditorGUI.indentLevel--;
	}
	*/

	//=====================================================================
	public override void OnGUI ( MaterialEditor i_materialEditor, MaterialProperty[] i_properties )
	{
		materialEditor 	= i_materialEditor;
		targetMat 		= i_materialEditor.target as Material;

		FindProperties ( i_properties );

		EditorGUI.BeginChangeCheck();

		LightingGUI();
		PBRGUI();
		ReflectionGUI();
		SSSGUI();
		WaterSurfaceGUI();
		//WaterEdgeGUI();
		WavesGUI();
		FoamGUI();
		DepthGUI();
		//DataMapGUI();

		if ( EditorGUI.EndChangeCheck() )
		{
			SetShaderKeywords();
		}
	}
}

