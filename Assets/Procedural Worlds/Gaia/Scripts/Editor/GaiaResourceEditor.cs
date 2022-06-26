using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using PWCommon4;
using Gaia.Internal;
using UnityEditorInternal;
using UnityEngine.Rendering;
using System.Linq;

namespace Gaia
{

    /// <summary>
    /// Editor for resource manager
    /// </summary>
    [CustomEditor(typeof(GaiaResource))]
    public class GaiaResourceEditor : PWEditor, IPWEditor
    {

        GUIStyle m_boxStyle = null;
        GUIStyle m_wrapStyle;
        GaiaResource m_resource = new GaiaResource();
        private DateTime m_lastSaveDT = DateTime.Now;
        private EditorUtils m_editorUtils = null;
        private bool[] m_resourceProtoFoldOutStatus;
        private bool[] m_resourceProtoMasksExpanded;
        private ReorderableList[] m_resourceProtoReorderableLists;
        private ImageMask[] m_maskListBeingDrawn;
        private CollisionMask[] m_collisionMaskListBeingDrawn;
        private int m_resourceIndexBeingDrawn;
        private int m_resourceMaskIndexBeingDrawn;

        private int GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType resourceType, int prototypeIndex)
        {
            //We have the following Resource types in this order: Textures, Terrain Details, Trees, GameObjects
            //To get the resource index we need to add the amount of other resources on top of the prototype index
            switch (resourceType)
            {
                case GaiaConstants.SpawnerResourceType.TerrainTexture:
                    return prototypeIndex;
                case GaiaConstants.SpawnerResourceType.TerrainDetail:
                    return m_resource.m_texturePrototypes.Length + prototypeIndex;
                case GaiaConstants.SpawnerResourceType.TerrainTree:
                    return m_resource.m_detailPrototypes.Length + m_resource.m_texturePrototypes.Length + prototypeIndex;
                case GaiaConstants.SpawnerResourceType.GameObject:
                    return m_resource.m_gameObjectPrototypes.Length + m_resource.m_detailPrototypes.Length + m_resource.m_texturePrototypes.Length + prototypeIndex;
                default:
                    return prototypeIndex;
            }


        }

        private void DrawTextures(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int textureProtoIndex = 0; textureProtoIndex < m_resource.m_texturePrototypes.Length; textureProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainTexture, textureProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_texturePrototypes[textureProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawTexturePrototype(m_resource.m_texturePrototypes[textureProtoIndex], m_editorUtils, showHelp);
                    if (m_editorUtils.Button("DeleteTexture"))
                    {
                        m_resource.m_texturePrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoTexture>(m_resource.m_texturePrototypes, textureProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        textureProtoIndex--;
                    }
                }
            }
            EditorGUI.indentLevel--;
            if (m_editorUtils.Button("AddTexture"))
            {
                m_resource.m_texturePrototypes = GaiaUtils.AddElementToArray<ResourceProtoTexture>(m_resource.m_texturePrototypes, new ResourceProtoTexture());
                m_resource.m_texturePrototypes[m_resource.m_texturePrototypes.Length - 1].m_name = "New Texture Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }


        public static void DrawTexturePrototype(ResourceProtoTexture resourceProtoTexture, EditorUtils editorUtils, bool showHelp, bool CTSProfileConnected = false)
        {
            editorUtils.LabelField("TextureProtoHeadingLayerPrototype", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            resourceProtoTexture.m_name = editorUtils.TextField("TextureProtoName", resourceProtoTexture.m_name, showHelp);
#if SUBSTANCE_PLUGIN_ENABLED
            if (resourceProtoTexture.m_substanceMaterial == null)
            {
                resourceProtoTexture.m_texture = (Texture2D)editorUtils.ObjectField("TextureProtoTexture", resourceProtoTexture.m_texture, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
                resourceProtoTexture.m_normal = (Texture2D)editorUtils.ObjectField("TextureProtoNormal", resourceProtoTexture.m_normal, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
                resourceProtoTexture.m_maskmap = (Texture2D)editorUtils.ObjectField("TextureProtoMaskMap", resourceProtoTexture.m_maskmap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            }
            else
            {
                EditorGUILayout.HelpBox(editorUtils.GetTextValue("SubstanceActiveHelp"), MessageType.Info);
                if (resourceProtoTexture.m_substanceMaterial.graphs.Count > 1)
                {
                    resourceProtoTexture.substanceSourceIndex = editorUtils.IntSlider("SubstanceGraphSelection", resourceProtoTexture.substanceSourceIndex, 1, resourceProtoTexture.m_substanceMaterial.graphs.Count, showHelp);
                }
                else
                {
                    resourceProtoTexture.substanceSourceIndex = 1;
                }
            }

            resourceProtoTexture.m_substanceMaterial = (Substance.Game.Substance)editorUtils.ObjectField("TextureProtoSubstance", resourceProtoTexture.m_substanceMaterial, typeof(Substance.Game.Substance), false, showHelp, GUILayout.MaxHeight(16));
#else
            resourceProtoTexture.m_texture = (Texture2D)editorUtils.ObjectField("TextureProtoTexture", resourceProtoTexture.m_texture, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            resourceProtoTexture.m_normal = (Texture2D)editorUtils.ObjectField("TextureProtoNormal", resourceProtoTexture.m_normal, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            resourceProtoTexture.m_maskmap = (Texture2D)editorUtils.ObjectField("TextureProtoMaskMap", resourceProtoTexture.m_maskmap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
#if CTS_PRESENT
            GUILayout.Space(5);
            EditorGUILayout.LabelField("CTS Profile Maps");
            EditorGUI.indentLevel++;
            resourceProtoTexture.m_CTSSmoothnessMap = (Texture2D)editorUtils.ObjectField("TextureProtoCTSSmoothness", resourceProtoTexture.m_CTSSmoothnessMap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            resourceProtoTexture.m_CTSRoughnessMap = (Texture2D)editorUtils.ObjectField("TextureProtoCTSRoughness", resourceProtoTexture.m_CTSRoughnessMap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            resourceProtoTexture.m_CTSHeightMap = (Texture2D)editorUtils.ObjectField("TextureProtoCTSHeight", resourceProtoTexture.m_CTSHeightMap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            resourceProtoTexture.m_CTSAmbientOcclusionMap = (Texture2D)editorUtils.ObjectField("TextureProtoCTSAmbientOcclusion", resourceProtoTexture.m_CTSAmbientOcclusionMap, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            EditorGUI.indentLevel--;
            GUILayout.Space(5);
#endif
#endif
            if (CTSProfileConnected)
            {
                resourceProtoTexture.m_sizeX = editorUtils.FloatField("TextureProtoTileSize", resourceProtoTexture.m_sizeX, showHelp);
                resourceProtoTexture.m_sizeY = resourceProtoTexture.m_sizeX;
                resourceProtoTexture.m_normalScale = editorUtils.Slider("TextureProtoNormalScale", resourceProtoTexture.m_normalScale, 0f, 10f, showHelp);
                resourceProtoTexture.m_smoothness = editorUtils.Slider("TextureProtoOffsetSmoothness", resourceProtoTexture.m_smoothness, 0f, 5f, showHelp);
            }
            else
            {
                resourceProtoTexture.m_sizeX = editorUtils.FloatField("TextureProtoSizeX", resourceProtoTexture.m_sizeX, showHelp);
                resourceProtoTexture.m_sizeY = editorUtils.FloatField("TextureProtoSizeY", resourceProtoTexture.m_sizeY, showHelp);
                resourceProtoTexture.m_offsetX = editorUtils.FloatField("TextureProtoOffsetX", resourceProtoTexture.m_offsetX, showHelp);
                resourceProtoTexture.m_offsetY = editorUtils.FloatField("TextureProtoOffsetY", resourceProtoTexture.m_offsetY, showHelp);
                resourceProtoTexture.m_normalScale = editorUtils.Slider("TextureProtoNormalScale", resourceProtoTexture.m_normalScale, 0f, 10f, showHelp);

#if HDPipeline || UPPipeline
                //The color tint is encoded in the diffuse Remap Max RGB / XYZ
                resourceProtoTexture.m_diffuseRemapMax = editorUtils.ColorField("TextureProtoColorTint", resourceProtoTexture.m_diffuseRemapMax, showHelp);
                //The toggle for "Opacity as Density" is encoded in the alpha channel / w-axis for the diffuse Remap min Vector 4
                bool OaDIsChecked = resourceProtoTexture.m_diffuseRemapMin.w == 1.0f;
                OaDIsChecked = editorUtils.Toggle("TextureProtoOpacityAsDensity", OaDIsChecked, showHelp);
                if (OaDIsChecked)
                {
                    resourceProtoTexture.m_diffuseRemapMin.w = 1.0f;
                }
                else
                {
                    resourceProtoTexture.m_diffuseRemapMin.w = 0.0f;
                }

                resourceProtoTexture.m_channelRemapFoldedOut = editorUtils.Foldout(resourceProtoTexture.m_channelRemapFoldedOut, "TextureProtoChannelRemapFoldout", showHelp);
                if (resourceProtoTexture.m_channelRemapFoldedOut)
                {
                    EditorGUI.indentLevel++;
                    //For HDRP, we have 
                    //R:Metallic
                    editorUtils.MinMaxSliderWithFields("TextureProtoRemapMetallic", ref resourceProtoTexture.m_maskMapRemapMin.x, ref resourceProtoTexture.m_maskMapRemapMax.x, 0f, 1f, showHelp);
                    //G:AO
                    editorUtils.MinMaxSliderWithFields("TextureProtoRemapAO", ref resourceProtoTexture.m_maskMapRemapMin.y, ref resourceProtoTexture.m_maskMapRemapMax.y, 0f, 1f, showHelp);
                    //A:Smoothness
                    editorUtils.MinMaxSliderWithFields("TextureProtoRemapSmoothness", ref resourceProtoTexture.m_maskMapRemapMin.w, ref resourceProtoTexture.m_maskMapRemapMax.w, 0f, 1f, showHelp);
                    EditorGUI.indentLevel--;
                }
#else
                 resourceProtoTexture.m_channelRemapFoldedOut = editorUtils.Foldout(resourceProtoTexture.m_channelRemapFoldedOut, "TextureProtoChannelRemapFoldout", showHelp);
                if (resourceProtoTexture.m_channelRemapFoldedOut)
                {
                    EditorGUI.indentLevel++;
                    //For Built-in, we have just the RGBA channels without any further description
                    //R:
                    editorUtils.MinMaxSliderWithFields("TextureProtoBuiltInRemapR", ref resourceProtoTexture.m_maskMapRemapMin.x, ref resourceProtoTexture.m_maskMapRemapMax.x, 0f, 1f, showHelp);
                    //G:
                    editorUtils.MinMaxSliderWithFields("TextureProtoBuiltInRemapG", ref resourceProtoTexture.m_maskMapRemapMin.y, ref resourceProtoTexture.m_maskMapRemapMax.y, 0f, 1f, showHelp);
                    //B:
                    editorUtils.MinMaxSliderWithFields("TextureProtoBuiltInRemapB", ref resourceProtoTexture.m_maskMapRemapMin.z, ref resourceProtoTexture.m_maskMapRemapMax.z, 0f, 1f, showHelp);
                    //A:
                    editorUtils.MinMaxSliderWithFields("TextureProtoBuiltInRemapA", ref resourceProtoTexture.m_maskMapRemapMin.w, ref resourceProtoTexture.m_maskMapRemapMax.w, 0f, 1f, showHelp);
                    EditorGUI.indentLevel--;
                }
                resourceProtoTexture.m_specularColor = editorUtils.ColorField("TextureProtoSpecularColor", resourceProtoTexture.m_specularColor, showHelp);
                resourceProtoTexture.m_metallic = editorUtils.Slider("TextureProtoOffsetMetallic", resourceProtoTexture.m_metallic, 0f, 1f, showHelp);
                resourceProtoTexture.m_smoothness = editorUtils.Slider("TextureProtoOffsetSmoothness", resourceProtoTexture.m_smoothness, 0f, 1f, showHelp);
#endif
            }
            EditorGUI.indentLevel--;
        }


        private void DrawGameObjects(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int gameObjectProtoIndex = 0; gameObjectProtoIndex < m_resource.m_gameObjectPrototypes.Length; gameObjectProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainTexture, gameObjectProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_gameObjectPrototypes[gameObjectProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawGameObjectPrototype(m_resource.m_gameObjectPrototypes[gameObjectProtoIndex], m_editorUtils, showHelp);

                    Rect buttonRect = EditorGUILayout.GetControlRect();
                    buttonRect.x += 15 * EditorGUI.indentLevel;
                    buttonRect.width -= 15 * EditorGUI.indentLevel;
                    if (GUI.Button(buttonRect, m_editorUtils.GetContent("DeleteGameObject")))
                    {
                        m_resource.m_gameObjectPrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoGameObject>(m_resource.m_gameObjectPrototypes, gameObjectProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        gameObjectProtoIndex--;
                    }

                    //Rect maskRect;
                    //m_resourceIndexBeingDrawn = gameObjectProtoIndex;
                    //if (m_resourceProtoMasksExpanded[resourceIndex])
                    //{
                    //    m_maskListBeingDrawn = m_resource.m_gameObjectPrototypes[gameObjectProtoIndex].m_imageMasks;
                    //    maskRect = EditorGUILayout.GetControlRect(true, m_resourceProtoReorderableLists[resourceIndex].GetHeight());
                    //    m_resourceProtoReorderableLists[resourceIndex].DoList(maskRect);
                    //}
                    //else
                    //{
                    //    int oldIndent = EditorGUI.indentLevel;
                    //    EditorGUI.indentLevel = 1;
                    //    m_resourceProtoMasksExpanded[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoMasksExpanded[resourceIndex], ImageMaskListEditor.PropertyCount("MaskSettings", m_resource.m_gameObjectPrototypes[gameObjectProtoIndex].m_imageMasks, m_editorUtils), true);
                    //    maskRect = GUILayoutUtility.GetLastRect();
                    //    EditorGUI.indentLevel = oldIndent;
                    //}
                }
            }
            EditorGUI.indentLevel--;
            if (m_editorUtils.Button("AddGameObject"))
            {
                m_resource.m_gameObjectPrototypes = GaiaUtils.AddElementToArray<ResourceProtoGameObject>(m_resource.m_gameObjectPrototypes, new ResourceProtoGameObject());
                m_resource.m_gameObjectPrototypes[m_resource.m_gameObjectPrototypes.Length - 1].m_name = "New Game Object Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }

        public static void DrawGameObjectPrototype(ResourceProtoGameObject resourceProtoGameObject, EditorUtils editorUtils, bool showHelp)
        {
            resourceProtoGameObject.m_name = editorUtils.TextField("GameObjectProtoName", resourceProtoGameObject.m_name, showHelp);
            EditorGUI.indentLevel++;
            resourceProtoGameObject.m_instancesFoldOut = editorUtils.Foldout("GameObjectInstances", resourceProtoGameObject.m_instancesFoldOut, showHelp);
            //Iterate through instances
            if (resourceProtoGameObject.m_instancesFoldOut)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < resourceProtoGameObject.m_instances.Length; i++)
                {
                    var instance = resourceProtoGameObject.m_instances[i];
                    instance.m_foldedOut = editorUtils.Foldout(instance.m_foldedOut, new GUIContent(instance.m_name), showHelp);
                    if (instance.m_foldedOut)
                    {
                        editorUtils.LabelField("GameObjectProtoHeadingPrefab", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        instance.m_name = editorUtils.TextField("GameObjectProtoName", instance.m_name, showHelp);
                        instance.m_desktopPrefab = (GameObject)editorUtils.ObjectField("GameObjectProtoInstanceDesktop", instance.m_desktopPrefab, typeof(GameObject), false, showHelp);
                        //instance.m_mobilePrefab = (GameObject)editorUtils.ObjectField("GameObjectProtoInstanceMobile", instance.m_mobilePrefab, typeof(GameObject), false, showHelp);
                        instance.m_minInstances = editorUtils.IntField("GameObjectProtoInstanceMinInstances", instance.m_minInstances, showHelp);
                        instance.m_maxInstances = editorUtils.IntField("GameObjectProtoInstanceMaxInstances", instance.m_maxInstances, showHelp);
                        //Display failure rate as Probability and with %
                        instance.m_failureRate = 1f-editorUtils.Slider("GameObjectProtoInstanceProbabilityRate", (1f-instance.m_failureRate) * 100, 0, 100f, showHelp) / 100f;
                        EditorGUI.indentLevel--;
                        editorUtils.LabelField("GameObjectProtoHeadingOffset", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetX", ref instance.m_minSpawnOffsetX, ref instance.m_maxSpawnOffsetX, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetY", ref instance.m_minSpawnOffsetY, ref instance.m_maxSpawnOffsetY, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetZ", ref instance.m_minSpawnOffsetZ, ref instance.m_maxSpawnOffsetZ, -100, 100, showHelp);
                        instance.m_yOffsetToSlope = editorUtils.Toggle("GameObjectProtoInstanceYOffsetToSlope", instance.m_yOffsetToSlope, showHelp);
                        EditorGUI.indentLevel--;
                        editorUtils.LabelField("GameObjectProtoHeadingRotation", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetX", ref instance.m_minRotationOffsetX, ref instance.m_maxRotationOffsetX, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetY", ref instance.m_minRotationOffsetY, ref instance.m_maxRotationOffsetY, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetZ", ref instance.m_minRotationOffsetZ, ref instance.m_maxRotationOffsetZ, 0, 360, showHelp);
                        instance.m_rotateToSlope = editorUtils.Toggle("GameObjectProtoInstanceRotateToSlope", instance.m_rotateToSlope, showHelp);
                        EditorGUI.indentLevel--;
                        editorUtils.LabelField("GameObjectProtoHeadingScale", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        instance.m_spawnScale = (SpawnScale)editorUtils.EnumPopup("ProtoSpawnScale", instance.m_spawnScale, showHelp);
                        EditorGUI.indentLevel++;
                        switch (instance.m_spawnScale)
                        {
                            case SpawnScale.Fixed:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceScale", instance.m_minScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceScale", instance.m_minXYZScale);
                                }
                                break;
                            case SpawnScale.Random:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.Fitness:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.FitnessRandomized:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                    instance.m_scaleRandomPercentage = editorUtils.Slider("GameObjectProtoInstanceRandomScalePercentage", instance.m_scaleRandomPercentage * 100f, 0, 100, showHelp) /100f;
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                    instance.m_XYZScaleRandomPercentage = editorUtils.Vector3Field("GameObjectProtoInstanceRandomScalePercentage", instance.m_XYZScaleRandomPercentage * 100f, showHelp) /100f;
                                }
                                break;
                                
                        }
                        EditorGUI.indentLevel--;
                        
                        instance.m_scaleByDistance = editorUtils.CurveField("GameObjectProtoInstanceScaleByDistance", instance.m_scaleByDistance);

                        //instance.m_localBounds = editorUtils.FloatField("GameObjectProtoInstanceLocalBounds", instance.m_localBounds);
                        Rect removeButtonRect = EditorGUILayout.GetControlRect();
                        removeButtonRect.x += 15 * EditorGUI.indentLevel;
                        removeButtonRect.width -= 15 * EditorGUI.indentLevel;
                        if (GUI.Button(removeButtonRect, editorUtils.GetContent("GameObjectRemoveInstance")))
                        {
                            resourceProtoGameObject.m_instances = GaiaUtils.RemoveArrayIndexAt<ResourceProtoGameObjectInstance>(resourceProtoGameObject.m_instances, i);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
                Rect buttonRect = EditorGUILayout.GetControlRect();
                buttonRect.x += 15 * EditorGUI.indentLevel;
                buttonRect.width -= 15 * EditorGUI.indentLevel;
                if (GUI.Button(buttonRect, editorUtils.GetContent("GameObjectAddInstance")))
                {
                    resourceProtoGameObject.m_instances = GaiaUtils.AddElementToArray<ResourceProtoGameObjectInstance>(resourceProtoGameObject.m_instances, new ResourceProtoGameObjectInstance() { m_name = "New Instance" });
                }
            }
            //resourceProtoGameObject.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoGameObject.m_dnaFoldedOut, showHelp);
            //if (resourceProtoGameObject.m_dnaFoldedOut)
            //{
            //    DrawDNA(resourceProtoGameObject.m_dna, editorUtils, showHelp);
            //}
            EditorGUI.indentLevel--;
        }

        public static void DrawSpawnExtensionPrototype(ResourceProtoSpawnExtension resourceProtoSpawnExtension, EditorUtils editorUtils, bool showHelp)
        {
            resourceProtoSpawnExtension.m_name = editorUtils.TextField("SpawnExtensionProtoName", resourceProtoSpawnExtension.m_name, showHelp);
            EditorGUI.indentLevel++;
            resourceProtoSpawnExtension.m_instancesFoldOut = editorUtils.Foldout("SpawnExtensionProtoInstances", resourceProtoSpawnExtension.m_instancesFoldOut, showHelp);
            //Iterate through instances
            if (resourceProtoSpawnExtension.m_instancesFoldOut)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < resourceProtoSpawnExtension.m_instances.Length; i++)
                {
                    var instance = resourceProtoSpawnExtension.m_instances[i];
                    instance.m_foldedOut = editorUtils.Foldout(instance.m_foldedOut, new GUIContent(instance.m_name), showHelp);
                    if (instance.m_foldedOut)
                    {
                        EditorGUI.indentLevel++;
                        instance.m_name = editorUtils.TextField("SpawnExtensionProtoName", instance.m_name, showHelp);

                        GameObject oldPrefab = instance.m_spawnerPrefab;

                        if (instance.m_invalidPrefabSupplied)
                        {
                            EditorGUILayout.HelpBox(editorUtils.GetTextValue("SpawnExtensionNoSpawnExtension"), MessageType.Error);
                        }

                        instance.m_spawnerPrefab = (GameObject)editorUtils.ObjectField("SpawnExtensionProtoPrefab", instance.m_spawnerPrefab, typeof(GameObject), false, showHelp);

                        //New Prefab submitted - check if it actually contains a Spawn Extension
                        if (oldPrefab != instance.m_spawnerPrefab)
                        {
                            if (instance.m_spawnerPrefab.GetComponent<ISpawnExtension>() != null)
                            {
                                instance.m_name = instance.m_spawnerPrefab.name;
                                instance.m_invalidPrefabSupplied = false;
                            }
                            else
                            {
                                instance.m_spawnerPrefab = null;
                                instance.m_invalidPrefabSupplied = true;
                            }
                        }
                        //instance.m_mobilePrefab = (GameObject)editorUtils.ObjectField("GameObjectProtoInstanceMobile", instance.m_mobilePrefab, typeof(GameObject), false, showHelp);
                        instance.m_minSpawnerRuns = editorUtils.IntField("SpawnExtensionProtoMinSpawns", instance.m_minSpawnerRuns, showHelp);
                        instance.m_maxSpawnerRuns = editorUtils.IntField("SpawnExtensionProtoMaxSpawns", instance.m_maxSpawnerRuns, showHelp);
                        instance.m_failureRate = editorUtils.Slider("SpawnExtensionProtoFailureRate", instance.m_failureRate, 0, 1, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetX", ref instance.m_minSpawnOffsetX, ref instance.m_maxSpawnOffsetX, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetY", ref instance.m_minSpawnOffsetY, ref instance.m_maxSpawnOffsetY, -100, 100, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceSpawnOffsetZ", ref instance.m_minSpawnOffsetZ, ref instance.m_maxSpawnOffsetZ, -100, 100, showHelp);
                        //instance.m_rotateToSlope = editorUtils.Toggle("GameObjectProtoInstanceRotateToSlope", instance.m_rotateToSlope, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetX", ref instance.m_minRotationOffsetX, ref instance.m_maxRotationOffsetX, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetY", ref instance.m_minRotationOffsetY, ref instance.m_maxRotationOffsetY, 0, 360, showHelp);
                        editorUtils.SliderRange("GameObjectProtoInstanceRotationOffsetZ", ref instance.m_minRotationOffsetZ, ref instance.m_maxRotationOffsetZ, 0, 360, showHelp);
                        //instance.m_useParentScale = editorUtils.Toggle("GameObjectProtoInstanceRotateToSlope", instance.m_useParentScale, showHelp);
                        instance.m_spawnScale = (SpawnScale)editorUtils.EnumPopup("ProtoSpawnScale", instance.m_spawnScale, showHelp);
                        EditorGUI.indentLevel++;
                        switch (instance.m_spawnScale)
                        {
                            case SpawnScale.Fixed:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceScale", instance.m_minScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceScale", instance.m_minXYZScale);
                                }
                                break;
                            case SpawnScale.Random:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.Fitness:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                }
                                break;
                            case SpawnScale.FitnessRandomized:
                                instance.m_commonScale = editorUtils.Toggle("ProtoCommonScale", instance.m_commonScale);
                                if (instance.m_commonScale)
                                {
                                    instance.m_minScale = editorUtils.Slider("GameObjectProtoInstanceMinScale", instance.m_minScale, 0, 100, showHelp);
                                    instance.m_maxScale = editorUtils.Slider("GameObjectProtoInstanceMaxScale", instance.m_maxScale, 0, 100, showHelp);
                                    instance.m_scaleRandomPercentage = editorUtils.Slider("GameObjectProtoInstanceRandomScalePercentage", instance.m_scaleRandomPercentage * 100f, 0, 100f, showHelp) /100f;
                                }
                                else
                                {
                                    instance.m_minXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMinScale", instance.m_minXYZScale, showHelp);
                                    instance.m_maxXYZScale = editorUtils.Vector3Field("GameObjectProtoInstanceMaxScale", instance.m_maxXYZScale, showHelp);
                                    instance.m_XYZScaleRandomPercentage = editorUtils.Vector3Field("GameObjectProtoInstanceRandomScalePercentage", instance.m_XYZScaleRandomPercentage*100f, showHelp) /100f;
                                }
                                break;
                        }
                        EditorGUI.indentLevel--;

                        instance.m_scaleByDistance = editorUtils.CurveField("GameObjectProtoInstanceScaleByDistance", instance.m_scaleByDistance);

                        //instance.m_localBounds = editorUtils.FloatField("GameObjectProtoInstanceLocalBounds", instance.m_localBounds);
                        Rect removeButtonRect = EditorGUILayout.GetControlRect();
                        removeButtonRect.x += 15 * EditorGUI.indentLevel;
                        removeButtonRect.width -= 15 * EditorGUI.indentLevel;
                        if (GUI.Button(removeButtonRect, editorUtils.GetContent("SpawnExtensionProtoRemoveInstance")))
                        {
                            resourceProtoSpawnExtension.m_instances = GaiaUtils.RemoveArrayIndexAt<ResourceProtoSpawnExtensionInstance>(resourceProtoSpawnExtension.m_instances, i);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
                Rect buttonRect = EditorGUILayout.GetControlRect();
                buttonRect.x += 15 * EditorGUI.indentLevel;
                buttonRect.width -= 15 * EditorGUI.indentLevel;
                if (GUI.Button(buttonRect, editorUtils.GetContent("SpawnExtensionProtoAddInstance")))
                {
                    resourceProtoSpawnExtension.m_instances = GaiaUtils.AddElementToArray<ResourceProtoSpawnExtensionInstance>(resourceProtoSpawnExtension.m_instances, new ResourceProtoSpawnExtensionInstance() { m_name = "New Spawn Extension" });
                }
            }
            resourceProtoSpawnExtension.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoSpawnExtension.m_dnaFoldedOut, showHelp);
            if (resourceProtoSpawnExtension.m_dnaFoldedOut)
            {
                resourceProtoSpawnExtension.m_dna.m_boundsRadius = editorUtils.FloatField("GameObjectProtoDNABoundsRadius", resourceProtoSpawnExtension.m_dna.m_boundsRadius, showHelp);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawStampDistributionPrototype(ResourceProtoStampDistribution resourceProtoStampDistribution, EditorUtils editorUtils, List<string> stampCategoryNames, int[]stampCategoryIDs, bool showHelp)
        {
            resourceProtoStampDistribution.m_name = editorUtils.TextField("SpawnExtensionProtoName", resourceProtoStampDistribution.m_name, showHelp);
            int deletionIndex = -99;
            for (int i=0;i<resourceProtoStampDistribution.m_featureSettings.Count;i++)
            {
                StampFeatureSettings featureChance = resourceProtoStampDistribution.m_featureSettings[i];
                EditorGUI.indentLevel++;
                featureChance.m_isFoldedOut = editorUtils.Foldout(featureChance.m_isFoldedOut, new GUIContent(featureChance.m_featureType), showHelp);
                if (featureChance.m_isFoldedOut)
                {
                    //we need to set the id initially according to the stored string for the category
                    //we can't rely on the IDs being the same every time, since the user might have added additional category folders in the meantime
                    int selectedStampCategoryID = -99;
                    selectedStampCategoryID = stampCategoryNames.IndexOf(featureChance.m_featureType);
                    selectedStampCategoryID = EditorGUILayout.IntPopup("Feature Type:", selectedStampCategoryID, stampCategoryNames.ToArray(), stampCategoryIDs);

                    featureChance.m_borderMaskStyle = (BorderMaskStyle)editorUtils.EnumPopup("FeatureTypeBorderMaskType", featureChance.m_borderMaskStyle, showHelp);
                    int selectedBorderMaskCategoryID = 0;
                    if (featureChance.m_borderMaskStyle == BorderMaskStyle.ImageMask)
                    {
                        
                        selectedBorderMaskCategoryID = Math.Max(0,stampCategoryNames.IndexOf(featureChance.m_borderMaskType));
                        selectedBorderMaskCategoryID = EditorGUILayout.IntPopup("Border Mask:", selectedBorderMaskCategoryID, stampCategoryNames.ToArray(), stampCategoryIDs);
                    }

                    int selectedIndex  = EditorGUILayout.Popup("Operation Type:", GaiaConstants.FeatureOperationNames.Select((x, j) => new { item = x, index = j }).First(x => x.item.Value == (int)featureChance.m_operation).index, GaiaConstants.TerrainGeneratorFeatureOperationNames);

                    //The "FeatureOperationNames" are just an array of strings to get a multi-level popup going. To get the actual operation enum
                    //we need to select the enum element at the same index as the name array. 
                    int selectedValue = -99;
                    GaiaConstants.FeatureOperationNames.TryGetValue(GaiaConstants.TerrainGeneratorFeatureOperationNames.Select(x => x).ToArray()[selectedIndex], out selectedValue);
                    featureChance.m_operation = (GaiaConstants.TerrainGeneratorFeatureOperation)selectedValue;

                    if (selectedStampCategoryID >= 0)
                    {
                        featureChance.m_featureType = stampCategoryNames[selectedStampCategoryID];
                        featureChance.m_stampInfluence = (ImageMaskInfluence)editorUtils.EnumPopup("MaskInfluence", featureChance.m_stampInfluence, showHelp);
                        featureChance.m_borderMaskType = stampCategoryNames[selectedBorderMaskCategoryID];
                        featureChance.m_chanceStrengthMapping = editorUtils.CurveField("FeatureTypeStrengthRemap", featureChance.m_chanceStrengthMapping, showHelp);
                        featureChance.m_invertChance =  editorUtils.Slider("FeatureTypeInvertChance", featureChance.m_invertChance, 0, 100, showHelp);
                        editorUtils.MinMaxSliderWithFields("FeatureTypeWidth", ref featureChance.m_minWidth, ref featureChance.m_maxWidth, 0, 100, showHelp);
                        featureChance.m_tieWidthToStrength = editorUtils.Toggle("FeatureTypeTieWidth", featureChance.m_tieWidthToStrength, showHelp);
                        if (featureChance.m_operation != GaiaConstants.TerrainGeneratorFeatureOperation.MixHeight)
                        {
                            editorUtils.MinMaxSliderWithFields("FeatureTypeHeight", ref featureChance.m_minHeight, ref featureChance.m_maxHeight, 0, 20, showHelp);
                            featureChance.m_tieHeightToStrength = editorUtils.Toggle("FeatureTypeTieHeight", featureChance.m_tieHeightToStrength, showHelp);
                            editorUtils.MinMaxSliderWithFields("FeatureTypeYOffset", ref featureChance.m_minYOffset, ref featureChance.m_maxYOffset, -150, 150, showHelp);
                        }
                        else
                        {
                            featureChance.m_minMixStrength *= 100f;
                            featureChance.m_maxMixStrength *= 100f;
                            editorUtils.MinMaxSliderWithFields("FeatureTypeMixHeightStrength", ref featureChance.m_minMixStrength, ref featureChance.m_maxMixStrength, 0, 200, showHelp);
                            featureChance.m_minMixStrength /= 100f;
                            featureChance.m_maxMixStrength /= 100f;
                            featureChance.m_tieHeightToStrength = editorUtils.Toggle("FeatureTypeTieStrength", featureChance.m_tieHeightToStrength, showHelp);
                            editorUtils.MinMaxSliderWithFields("FeatureTypeMixMidpoint", ref featureChance.m_minMixMidPoint, ref featureChance.m_maxMixMidPoint, 0, 1, showHelp);
                        }

                    }
                    if (editorUtils.ButtonRight("RemoveFeatureType"))
                    {
                        deletionIndex = i;
                    }
                }

                EditorGUI.indentLevel--;
            }
            if (deletionIndex != -99)
            {
                resourceProtoStampDistribution.m_featureSettings.RemoveAt(deletionIndex);
            }

            if (editorUtils.ButtonAutoIndent("AddFeatureType"))
            {
                resourceProtoStampDistribution.m_featureSettings.Add(new StampFeatureSettings() { m_featureType = stampCategoryNames[0]});
            }

        }

        public static void DrawWorldBiomeMaskPrototype(ResourceProtoWorldBiomeMask worldBiomeMaskPrototype, EditorUtils editorUtils, bool showHelp)
        {
            worldBiomeMaskPrototype.m_name = editorUtils.TextField("SpawnExtensionProtoName", worldBiomeMaskPrototype.m_name, showHelp);
            worldBiomeMaskPrototype.m_biomePreset = (BiomePreset)editorUtils.ObjectField("WorldBiomeMaskBiomePreset", worldBiomeMaskPrototype.m_biomePreset, typeof(BiomePreset), false, showHelp);

        }

        public static void DrawProbePrototype(ResourceProtoProbe probePrototype, EditorUtils editorUtils, GaiaConstants.EnvironmentRenderer currentPipeline, bool showHelp)
        {
            probePrototype.m_name = editorUtils.TextField("ProbeProtoName", probePrototype.m_name, showHelp);
            probePrototype.m_probeType = (ProbeType)editorUtils.EnumPopup("ProbeType", probePrototype.m_probeType, showHelp);
            if (probePrototype.m_probeType == ProbeType.ReflectionProbe)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                editorUtils.Heading("Probe Rendering Settings");
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel++;
                probePrototype.m_reflectionProbeData.reflectionProbeMode = (ReflectionProbeMode)editorUtils.EnumPopup("ReflectionProbeMode", probePrototype.m_reflectionProbeData.reflectionProbeMode, showHelp);
                probePrototype.m_reflectionProbeData.reflectionProbeRefresh = (GaiaConstants.ReflectionProbeRefreshModePW)editorUtils.EnumPopup("ReflectionProbeRefresh", probePrototype.m_reflectionProbeData.reflectionProbeRefresh, showHelp);
                EditorGUI.indentLevel--;
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                editorUtils.Heading("Probe Optimization Settings");
                GUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                if (probePrototype.m_reflectionProbeData.reflectionProbeRefresh == GaiaConstants.ReflectionProbeRefreshModePW.ViaScripting)
                {
                    probePrototype.m_reflectionProbeData.reflectionProbeTimeSlicingMode = (ReflectionProbeTimeSlicingMode)editorUtils.EnumPopup("ReflectionProbeTimeSlicing", probePrototype.m_reflectionProbeData.reflectionProbeTimeSlicingMode, showHelp);
                }
                if (currentPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    probePrototype.m_reflectionProbeData.reflectionProbeResolution = (GaiaConstants.ReflectionProbeResolution)editorUtils.EnumPopup("ReflectionProbeResolution", probePrototype.m_reflectionProbeData.reflectionProbeResolution, showHelp);
                    probePrototype.m_reflectionProbeData.reflectionCubemapCompression = (ReflectionCubemapCompression)editorUtils.EnumPopup("ReflectionProbeCompression", probePrototype.m_reflectionProbeData.reflectionCubemapCompression, showHelp);
                }
                probePrototype.m_reflectionProbeData.reflectionProbeClipPlaneDistance = editorUtils.Slider("ReflectionProbeRenderDistance", probePrototype.m_reflectionProbeData.reflectionProbeClipPlaneDistance, 0.1f, 10000f, showHelp);
                probePrototype.m_reflectionProbeData.reflectionProbeShadowDistance = editorUtils.Slider("ReflectionProbeShadowDistance", probePrototype.m_reflectionProbeData.reflectionProbeShadowDistance, 0.1f, 3000f, showHelp);
                probePrototype.m_reflectionProbeData.reflectionprobeCullingMask = GaiaEditorUtils.LayerMaskField(new GUIContent(editorUtils.GetTextValue("ReflectionProbeCullingMask"), editorUtils.GetTooltip("ReflectionProbeCullingMask")), probePrototype.m_reflectionProbeData.reflectionprobeCullingMask);
                editorUtils.InlineHelp("ReflectionProbeCullingMask", showHelp);
                EditorGUI.indentLevel--;
            }

        }

        private void DrawTrees(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int treeProtoIndex = 0; treeProtoIndex < m_resource.m_treePrototypes.Length; treeProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainTree, treeProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_treePrototypes[treeProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawTreePrototype(m_resource.m_treePrototypes[treeProtoIndex], m_editorUtils, showHelp);

                    if (m_editorUtils.Button("DeleteTree"))
                    {
                        m_resource.m_treePrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoTree>(m_resource.m_treePrototypes, treeProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        treeProtoIndex--;
                    }

                    //Rect maskRect;
                    //m_resourceIndexBeingDrawn = treeProtoIndex;
                    //if (m_resourceProtoMasksExpanded[resourceIndex])
                    //{
                    //    m_maskListBeingDrawn = m_resource.m_treePrototypes[treeProtoIndex].m_imageMasks;
                    //    maskRect = EditorGUILayout.GetControlRect(true, m_resourceProtoReorderableLists[resourceIndex].GetHeight());
                    //    m_resourceProtoReorderableLists[resourceIndex].DoList(maskRect);
                    //}
                    //else
                    //{
                    //    int oldIndent = EditorGUI.indentLevel;
                    //    EditorGUI.indentLevel = 1;
                    //    m_resourceProtoMasksExpanded[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoMasksExpanded[resourceIndex], ImageMaskListEditor.PropertyCount("MaskSettings", m_resource.m_treePrototypes[treeProtoIndex].m_imageMasks, m_editorUtils), true);
                    //    maskRect = GUILayoutUtility.GetLastRect();
                    //    EditorGUI.indentLevel = oldIndent;
                    //}
                }
            }
            EditorGUI.indentLevel--;
            if (m_editorUtils.Button("AddTree"))
            {
                m_resource.m_treePrototypes = GaiaUtils.AddElementToArray<ResourceProtoTree>(m_resource.m_treePrototypes, new ResourceProtoTree());
                m_resource.m_treePrototypes[m_resource.m_treePrototypes.Length - 1].m_name = "New Tree Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }

        public static void DrawTreePrototype(ResourceProtoTree resourceProtoTree, EditorUtils editorUtils, bool showHelp)
        {
            editorUtils.LabelField("TreeProtoHeadingPrefab", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            resourceProtoTree.m_name = editorUtils.TextField("GameObjectProtoName", resourceProtoTree.m_name, showHelp);
            resourceProtoTree.m_desktopPrefab = (GameObject)editorUtils.ObjectField("TreeProtoDesktopPrefab", resourceProtoTree.m_desktopPrefab, typeof(GameObject), false, showHelp);
            //resourceProtoTree.m_mobilePrefab = (GameObject)editorUtils.ObjectField("TreeProtoMobilePrefab", resourceProtoTree.m_mobilePrefab, typeof(GameObject), false, showHelp);
            resourceProtoTree.m_bendFactor = editorUtils.Slider("TreeProtoBendFactor", resourceProtoTree.m_bendFactor, 0, 100, showHelp);
            resourceProtoTree.m_healthyColour = editorUtils.ColorField("TreeProtoHealthyColour", resourceProtoTree.m_healthyColour, showHelp);
            resourceProtoTree.m_dryColour = editorUtils.ColorField("TreeProtoDryColour", resourceProtoTree.m_dryColour, showHelp);
            EditorGUI.indentLevel--;
            editorUtils.LabelField("TreeProtoHeadingScale", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            resourceProtoTree.m_spawnScale = (SpawnScale)editorUtils.EnumPopup("ProtoSpawnScale", resourceProtoTree.m_spawnScale,showHelp);
            EditorGUI.indentLevel++;
            switch (resourceProtoTree.m_spawnScale)
            {
                case SpawnScale.Fixed:
                    resourceProtoTree.m_minWidth = editorUtils.FloatField("TreeProtoWidth", resourceProtoTree.m_minWidth, showHelp);
                    resourceProtoTree.m_minHeight = editorUtils.FloatField("TreeProtoHeight", resourceProtoTree.m_minHeight, showHelp);
                    break;
                case SpawnScale.Random:
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxWidth", ref resourceProtoTree.m_minWidth, ref resourceProtoTree.m_maxWidth, 0f, 10f, showHelp);
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxHeight", ref resourceProtoTree.m_minHeight, ref resourceProtoTree.m_maxHeight, 0f, 10f, showHelp);
                    break;
                case SpawnScale.Fitness:
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxWidth", ref resourceProtoTree.m_minWidth, ref resourceProtoTree.m_maxWidth, 0f, 10f, showHelp);
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxHeight", ref resourceProtoTree.m_minHeight, ref resourceProtoTree.m_maxHeight, 0f, 10f, showHelp);
                    break;
                case SpawnScale.FitnessRandomized:
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxWidth", ref resourceProtoTree.m_minWidth, ref resourceProtoTree.m_maxWidth, 0f, 10f, showHelp);
                    resourceProtoTree.m_widthRandomPercentage = editorUtils.Slider("TreeProtoWidthRandomPercentage", resourceProtoTree.m_widthRandomPercentage *100f, 0f, 100f) /100f;
                    editorUtils.MinMaxSliderWithFields("TreeProtoMinMaxHeight", ref resourceProtoTree.m_minHeight, ref resourceProtoTree.m_maxHeight, 0f, 10f, showHelp);
                    resourceProtoTree.m_heightRandomPercentage = editorUtils.Slider("TreeProtoHeightRandomPercentage", resourceProtoTree.m_heightRandomPercentage *100f, 0f, 100f) /100f;
                    break;
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
             //resourceProtoTree.m_dna.m_boundsRadius = editorUtils.FloatField("GameObjectProtoDNABoundsRadius", resourceProtoTree.m_dna.m_boundsRadius, showHelp);

            //resourceProtoTree.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoTree.m_dnaFoldedOut, showHelp);
            //if (resourceProtoTree.m_dnaFoldedOut)
            //{
            //    DrawDNA(resourceProtoTree.m_dna, editorUtils, showHelp);
            //}
        }

        private void DrawTerrainDetails(bool showHelp)
        {
            EditorGUI.indentLevel++;
            for (int terrainDetailProtoIndex = 0; terrainDetailProtoIndex < m_resource.m_detailPrototypes.Length; terrainDetailProtoIndex++)
            {
                int resourceIndex = GetResourceIndexFromPrototypeIndex(GaiaConstants.SpawnerResourceType.TerrainDetail, terrainDetailProtoIndex);

                m_resourceProtoFoldOutStatus[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoFoldOutStatus[resourceIndex], m_resource.m_detailPrototypes[terrainDetailProtoIndex].m_name);
                if (m_resourceProtoFoldOutStatus[resourceIndex])
                {
                    DrawTerrainDetailPrototype(m_resource.m_detailPrototypes[terrainDetailProtoIndex], m_editorUtils, showHelp);
                    if (m_editorUtils.Button("DeleteTerrainDetail"))
                    {
                        m_resource.m_detailPrototypes = GaiaUtils.RemoveArrayIndexAt<ResourceProtoDetail>(m_resource.m_detailPrototypes, terrainDetailProtoIndex);
                        m_resourceProtoFoldOutStatus = GaiaUtils.RemoveArrayIndexAt<bool>(m_resourceProtoFoldOutStatus, resourceIndex);
                        //Correct the index since we just removed one texture
                        terrainDetailProtoIndex--;
                    }

                    //Rect maskRect;
                    //m_resourceIndexBeingDrawn = terrainDetailProtoIndex;
                    //if (m_resourceProtoMasksExpanded[resourceIndex])
                    //{
                    //    m_maskListBeingDrawn = m_resource.m_detailPrototypes[terrainDetailProtoIndex].m_imageMasks;
                    //    maskRect = EditorGUILayout.GetControlRect(true, m_resourceProtoReorderableLists[resourceIndex].GetHeight());
                    //    m_resourceProtoReorderableLists[resourceIndex].DoList(maskRect);
                    //}
                    //else
                    //{
                    //    int oldIndent = EditorGUI.indentLevel;
                    //    EditorGUI.indentLevel = 1;
                    //    m_resourceProtoMasksExpanded[resourceIndex] = EditorGUILayout.Foldout(m_resourceProtoMasksExpanded[resourceIndex], ImageMaskListEditor.PropertyCount("MaskSettings", m_resource.m_detailPrototypes[terrainDetailProtoIndex].m_imageMasks, m_editorUtils), true);
                    //    maskRect = GUILayoutUtility.GetLastRect();
                    //    EditorGUI.indentLevel = oldIndent;
                    //}
                }
            }
            EditorGUI.indentLevel--;

            if (m_editorUtils.Button("AddTerrainDetail"))
            {
                m_resource.m_detailPrototypes = GaiaUtils.AddElementToArray<ResourceProtoDetail>(m_resource.m_detailPrototypes, new ResourceProtoDetail());
                m_resource.m_detailPrototypes[m_resource.m_detailPrototypes.Length - 1].m_name = "New Terrain Detail Prototype";
                m_resourceProtoFoldOutStatus = GaiaUtils.AddElementToArray<bool>(m_resourceProtoFoldOutStatus, false);
            }
        }

        public static void DrawTerrainDetailPrototype(ResourceProtoDetail resourceProtoDetail, EditorUtils editorUtils, bool showHelp)
        {
            editorUtils.LabelField("TreeProtoHeadingPrefab", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            resourceProtoDetail.m_name = editorUtils.TextField("GameObjectProtoName", resourceProtoDetail.m_name, showHelp);
            resourceProtoDetail.m_renderMode = (DetailRenderMode)editorUtils.EnumPopup("DetailProtoRenderMode", resourceProtoDetail.m_renderMode, showHelp);
            if (resourceProtoDetail.m_renderMode == DetailRenderMode.VertexLit || resourceProtoDetail.m_renderMode == DetailRenderMode.Grass)
            {
                resourceProtoDetail.m_detailProtoype = (GameObject)editorUtils.ObjectField("DetailProtoModel", resourceProtoDetail.m_detailProtoype, typeof(GameObject), false, showHelp);
            }
            if (resourceProtoDetail.m_renderMode != DetailRenderMode.VertexLit)
            {
                resourceProtoDetail.m_detailTexture = (Texture2D)editorUtils.ObjectField("DetailProtoTexture", resourceProtoDetail.m_detailTexture, typeof(Texture2D), false, showHelp, GUILayout.MaxHeight(16));
            }
            editorUtils.LabelField("ProtoSpawnScale", "ProtoSpawnScaleRandom", showHelp);
            editorUtils.MinMaxSliderWithFields("DetailProtoMinMaxWidth", ref resourceProtoDetail.m_minWidth, ref resourceProtoDetail.m_maxWidth, 0, 20, showHelp);
            editorUtils.MinMaxSliderWithFields("DetailProtoMinMaxHeight", ref resourceProtoDetail.m_minHeight, ref resourceProtoDetail.m_maxHeight, 0, 20, showHelp);
            resourceProtoDetail.m_noiseSpread = editorUtils.FloatField("DetailProtoNoiseSpread", resourceProtoDetail.m_noiseSpread, showHelp);
            //resourceProtoDetail.m_bendFactor = editorUtils.FloatField("DetailProtoBendFactor", resourceProtoDetail.m_bendFactor, showHelp);
            resourceProtoDetail.m_healthyColour = editorUtils.ColorField("DetailProtoHealthyColour", resourceProtoDetail.m_healthyColour, showHelp);
            resourceProtoDetail.m_dryColour = editorUtils.ColorField("DetailProtoDryColour", resourceProtoDetail.m_dryColour, showHelp);
            //resourceProtoDetail.m_dnaFoldedOut = editorUtils.Foldout("GameObjectProtoDNA", resourceProtoDetail.m_dnaFoldedOut, showHelp);
            //if (resourceProtoDetail.m_dnaFoldedOut)
            //{
            //    DrawDNA(resourceProtoDetail.m_dna, editorUtils, showHelp);
            //}
            EditorGUI.indentLevel--;
        }


        public void DropAreaGUI()
        {
            //Drop out if no resource selected
            if (m_resource == null)
            {
                return;
            }

            //Ok - set up for drag and drop
            Event evt = Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, "Drop Game Objects / Prefabs Here", m_boxStyle);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

#if UNITY_2018_3_OR_NEWER
                        //Work out if we have prefab instances or prefab objects
                        bool havePrefabInstances = false;
                        foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                        {
                            PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(dragged_object);

                            if (pt == PrefabAssetType.Regular || pt == PrefabAssetType.Model)
                            {
                                havePrefabInstances = true;
                                break;
                            }
                        }

                        if (havePrefabInstances)
                        {
                            List<GameObject> prototypes = new List<GameObject>();

                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(dragged_object);

                                if (pt == PrefabAssetType.Regular || pt == PrefabAssetType.Model)
                                {
                                    prototypes.Add(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefab instances!");
                                }
                            }

                            //Same them as a single entity
                            if (prototypes.Count > 0)
                            {
                                m_resource.AddGameObject(prototypes);
                            }
                        }
                        else
                        {
                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                if (PrefabUtility.GetPrefabAssetType(dragged_object) == PrefabAssetType.Regular)
                                {
                                    m_resource.AddGameObject(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefabs or game objects attached to prefabs!");
                                }
                            }
                        }
#else

                        //Work out if we have prefab instances or prefab objects
                        bool havePrefabInstances = false;
                        foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                        {
                            PrefabType pt = PrefabUtility.GetPrefabType(dragged_object);

                            if (pt == PrefabType.PrefabInstance || pt == PrefabType.ModelPrefabInstance)
                            {
                                havePrefabInstances = true;
                                break;
                            }
                        }

                        if (havePrefabInstances)
                        {
                            List<GameObject> prototypes = new List<GameObject>();

                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                PrefabType pt = PrefabUtility.GetPrefabType(dragged_object);

                                if (pt == PrefabType.PrefabInstance || pt == PrefabType.ModelPrefabInstance)
                                {
                                    prototypes.Add(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefab instances!");
                                }
                            }

                            //Same them as a single entity
                            if (prototypes.Count > 0)
                            {
                                m_resource.AddGameObject(prototypes);
                            }
                        }
                        else
                        {
                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                if (PrefabUtility.GetPrefabType(dragged_object) == PrefabType.Prefab)
                                {
                                    m_resource.AddGameObject(dragged_object as GameObject);
                                }
                                else
                                {
                                    Debug.LogWarning("You may only add prefabs or game objects attached to prefabs!");
                                }
                            }
                        }
#endif
                    }
                    break;
            }
        }


        /// <summary>
        /// Get the range from the terrain
        /// </summary>
        /// <returns>Range from currently active terrain or 1024f</returns>
        private float GetRangeFromTerrain()
        {
            float range = 1024f;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                range = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / 2f;
            }
            return range;
        }

        /// <summary>
        /// Get texture increment from terrain
        /// </summary>
        /// <returns></returns>
        private float GetTextureIncrementFromTerrain()
        {
            float increment = 1f;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                if (t.terrainData != null)
                {
                    increment = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / (float)t.terrainData.alphamapResolution;
                }
            }
            return increment;
        }

        /// <summary>
        /// Get detail increment from terrain
        /// </summary>
        /// <returns></returns>
        private float GetDetailIncrementFromTerrain()
        {
            float increment = 1f;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                if (t.terrainData != null)
                {
                    increment = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / (float)t.terrainData.detailResolution;
                }
            }
            return increment;
        }


        /// <summary>
        /// Get a content label - look the tooltip up if possible
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        GUIContent GetLabel(string name)
        {
            string tooltip = "";
            if (m_tooltips.TryGetValue(name, out tooltip))
            {
                return new GUIContent(name, tooltip);
            }
            else
            {
                return new GUIContent(name);
            }
        }

        /// <summary>
        /// The tooltips
        /// </summary>
        static Dictionary<string, string> m_tooltips = new Dictionary<string, string>
        {
            { "Get From Terrain", "Get or update the resource prototypes from the current terrain." },
            { "Apply To Terrains", "Apply the resource prototypes into all existing terrains." },
            { "Visualise", "Visualise the fitness of resource prototypes." },
        };


    }
}
