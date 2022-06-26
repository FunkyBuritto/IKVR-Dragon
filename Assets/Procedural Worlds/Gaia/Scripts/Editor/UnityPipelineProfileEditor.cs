using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PWCommon4;
using Gaia.Internal;
using System;
using System.Linq;
using System.IO;
using ProceduralWorlds.Gaia.PackageSystem;

namespace Gaia.Pipeline
{
    [CustomEditor(typeof(UnityPipelineProfile))]
    public class UnityPipelineProfileEditor : PWEditor
    {
        private GUIStyle m_boxStyle;
        private EditorUtils m_editorUtils;
        private UnityPipelineProfile m_profile;
        private string m_version;
        private bool[] m_shaderMappingLibraryFoldouts;
        private GUIStyle m_matlibButtonStyle;

        private void OnEnable()
        {
            //Initialization
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            //Get Gaia Lighting Profile object
            m_profile = (UnityPipelineProfile)target;

            m_version = PWApp.CONF.Version;

            m_shaderMappingLibraryFoldouts = new bool[m_profile.m_ShaderMappingLibrary.Length];
        }

        public override void OnInspectorGUI()
        {           
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = {textColor = GUI.skin.label.normal.textColor},
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
            }

            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Profile Version: " + m_version);

            bool enableEditMode = System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities"));
            if (enableEditMode)
            {
                m_profile.m_editorUpdates = EditorGUILayout.ToggleLeft("Use Procedural Worlds Editor Settings", m_profile.m_editorUpdates);
            }
            else
            {
                m_profile.m_editorUpdates = false;
            }

            if (m_profile.m_editorUpdates)
            {
                m_editorUtils.Panel("ProfileSettings", ProfileSettingsEnabled, false);
            }

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);
            }
        }

        private void ProfileSettingsEnabled(bool helpEnabled)
        {
            GUILayout.BeginVertical("Pipeline Profile Settings", m_boxStyle);
            GUILayout.Space(20);
            EditorGUI.indentLevel++;
            DrawDefaultInspector();
            EditorGUI.indentLevel--;

            if (m_matlibButtonStyle == null)
            {
                m_matlibButtonStyle = new GUIStyle(GUI.skin.button);
                m_matlibButtonStyle.margin = new RectOffset(40, m_matlibButtonStyle.margin.right, m_matlibButtonStyle.margin.top, m_matlibButtonStyle.margin.bottom);
            }

            //Draw the Material library settings
            EditorGUILayout.LabelField("Shader Mapping");
            EditorGUI.indentLevel++;
            for (int i=0; i < m_profile.m_ShaderMappingLibrary.Length; i++)
            {
                ShaderMappingEntry entry = m_profile.m_ShaderMappingLibrary[i];
                //int materialCount = entry.m_materials == null ? 0 : entry.m_materials.Count;
                m_shaderMappingLibraryFoldouts[i] = EditorGUILayout.Foldout(m_shaderMappingLibraryFoldouts[i], entry.m_builtInShaderName);
                if (m_shaderMappingLibraryFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    string oldbuiltInShader = entry.m_builtInShaderName;
                    //entry.m_name = EditorGUILayout.TextField("Name", entry.m_name);
                    entry.m_builtInShaderName = EditorGUILayout.TextField("Builtin Shader", entry.m_builtInShaderName);
                    //if (oldbuiltInShader != entry.m_builtInShaderName)
                    //{
                    //    entry.m_name = entry.m_builtInShaderName;
                    //}
                    entry.m_URPReplacementShaderName = EditorGUILayout.TextField("URP Shader", entry.m_URPReplacementShaderName);
                    entry.m_HDRPReplacementShaderName = EditorGUILayout.TextField("HDRP Shader", entry.m_HDRPReplacementShaderName);
                    //EditorGUI.indentLevel++;
                    //entry.m_supportUnitypackages = EditorGUILayout.Toggle("Support Unity Packages", entry.m_supportUnitypackages);
                    //if (entry.m_supportUnitypackages)
                    //{
                    //    EditorGUI.indentLevel++;
                    //    entry.m_builtInMaterialUnitypackage = EditorGUILayout.TextField("Builtin Material Package", entry.m_builtInMaterialUnitypackage);
                    //    entry.m_URPMaterialUnitypackage = EditorGUILayout.TextField("URP Material Package", entry.m_URPMaterialUnitypackage);
                    //    entry.m_HDRPMaterialUnitypackage = EditorGUILayout.TextField("HDRP Material Package", entry.m_HDRPMaterialUnitypackage);
                    //    EditorGUI.indentLevel--;
                    //}
                    //entry.m_floatChecksFoldedOut = EditorGUILayout.Foldout(entry.m_floatChecksFoldedOut, "Float Checks [" + entry.m_floatChecks.Count.ToString() + "]");
                    //int floatCheckDeleteIndex = -99;
                    //if (entry.m_floatChecksFoldedOut)
                    //{
                    //    EditorGUI.indentLevel++;
                    //    for (int j = 0; j < entry.m_floatChecks.Count; j++)
                    //    {
                    //        Color regularColor = GUI.color;
                    //        if (String.IsNullOrEmpty(entry.m_floatChecks[j].m_floatValue) || String.IsNullOrEmpty(entry.m_floatChecks[j].m_shaderKeyWord))
                    //        {
                    //            GUI.color = Color.red;
                    //        }
                    //        Rect rect = EditorGUILayout.GetControlRect();
                    //        EditorGUILayout.BeginHorizontal();
                    //        EditorGUILayout.LabelField("Float", GUILayout.MinWidth(rect.width/5f));
                    //        entry.m_floatChecks[j].m_floatValue = EditorGUILayout.TextField(entry.m_floatChecks[j].m_floatValue, GUILayout.MinWidth(rect.width / 5f));
                    //        EditorGUILayout.LabelField("KeyWord", GUILayout.MinWidth(rect.width / 5f));
                    //        entry.m_floatChecks[j].m_shaderKeyWord = EditorGUILayout.TextField(entry.m_floatChecks[j].m_shaderKeyWord, GUILayout.MinWidth(rect.width / 5f));
                    //        if (GUILayout.Button("Delete", m_matlibButtonStyle, GUILayout.MaxWidth(50)))
                    //        {
                    //            floatCheckDeleteIndex = j;
                    //        }
                    //        EditorGUILayout.EndHorizontal();
                    //        GUI.color = regularColor;
                            
                    //    }
                    //    if (floatCheckDeleteIndex != -99)
                    //    {
                    //        entry.m_floatChecks.RemoveAt(floatCheckDeleteIndex);
                    //    }
                    //    if (GUILayout.Button("Add new Float Check", m_matlibButtonStyle))
                    //    {
                    //        entry.m_floatChecks.Add(new ShaderFloatCheck());
                    //    }
                        
                    //EditorGUI.indentLevel--;
                }
                //EditorGUI.indentLevel--;

                    //EditorGUI.indentLevel++;
                    //entry.m_materialsFoldedOut = EditorGUILayout.Foldout(entry.m_materialsFoldedOut, "Materials [" + materialCount.ToString() +"]");
                    //if (entry.m_materialsFoldedOut)
                    //{
                    //    EditorGUI.indentLevel++;
                    //    for (int j = 0; j < entry.m_materials.Count; j++)
                    //    {
                    //        EditorGUILayout.BeginHorizontal();
                    //        Color regularColor = GUI.color;
                    //        if (entry.m_materials[j] == null)
                    //        {
                    //            GUI.color = Color.red;
                    //        }
                    //        entry.m_materials[j] = (Material)EditorGUILayout.ObjectField("Material " + j.ToString(), entry.m_materials[j], typeof(Material),false);
                    //        if (GUILayout.Button("-", GUILayout.MaxWidth(30f)))
                    //        {
                    //            entry.m_materials.RemoveAt(j);
                    //        }
                    //        GUI.color = regularColor;
                    //        EditorGUILayout.EndHorizontal();
                    //    }
                    //    EditorGUI.indentLevel--;
                    //}
                    //if (GUILayout.Button("Load Materials for " + entry.m_name, m_matlibButtonStyle))
                    //{
                    //    LoadMaterials(entry);
                    //}
                if (GUILayout.Button("Remove " + entry.m_builtInShaderName, m_matlibButtonStyle))
                {
                    if (EditorUtility.DisplayDialog("Delete Shader Mapping Entry", "Are you sure you want to delete the entire entry for '" + entry.m_builtInShaderName + "' ?", "OK", "Cancel"))
                    {
                        m_profile.m_ShaderMappingLibrary = GaiaUtils.RemoveArrayIndexAt(m_profile.m_ShaderMappingLibrary, i);
                        m_shaderMappingLibraryFoldouts = GaiaUtils.RemoveArrayIndexAt(m_shaderMappingLibraryFoldouts, i);
                    }

                }
                if (GUILayout.Button("Insert entry below", m_matlibButtonStyle))
                {
                    m_profile.m_ShaderMappingLibrary = GaiaUtils.InsertElementInArray(m_profile.m_ShaderMappingLibrary, new ShaderMappingEntry() { m_builtInShaderName = "New Entry" } ,i+1);
                    m_shaderMappingLibraryFoldouts = GaiaUtils.InsertElementInArray(m_shaderMappingLibraryFoldouts, true, i+1);

                }
                //EditorGUI.indentLevel--;
                //EditorGUI.indentLevel--;
                
            }
            if (GUILayout.Button("Add new Material Library entry"))
            {
                m_profile.m_ShaderMappingLibrary = GaiaUtils.AddElementToArray(m_profile.m_ShaderMappingLibrary, new ShaderMappingEntry() { m_builtInShaderName = "New Entry" });
                m_shaderMappingLibraryFoldouts = GaiaUtils.AddElementToArray(m_shaderMappingLibraryFoldouts, true);
            }
            EditorGUI.indentLevel--;

            GUILayout.EndVertical();
        }
    }
}