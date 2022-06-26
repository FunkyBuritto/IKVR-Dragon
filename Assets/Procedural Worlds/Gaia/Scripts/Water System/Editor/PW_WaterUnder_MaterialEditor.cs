using System;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;

[CanEditMultipleObjects]
public class PW_WaterUnderMaterialEditor : ShaderGUI
{
	MaterialProperty  underWaterColor;

	//-------------------------------------------------------------------------
	private void FindProperties ( MaterialProperty[] props )
	{
		underWaterColor			= FindProperty ( "_UnderWaterColor", props );
	}

	//-------------------------------------------------------------------------
	public override void OnGUI ( MaterialEditor i_materialEditor, MaterialProperty[] i_properties )
	{
		FindProperties ( i_properties );

		i_materialEditor.ShaderProperty( underWaterColor, new GUIContent("Tint", "Tint of underwater side"));
	}



}

