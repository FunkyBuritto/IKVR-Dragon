using System;
using System.Collections;
using System.Collections.Generic;
using PWCommon4;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Linq;

namespace Gaia
{
    public static class GaiaEditorUtils
    {
        /// <summary>
        /// Marks the active scene dirty.
        /// </summary>
        public static void MarkSceneDirty()
        {
            if (!Application.isPlaying)
            {
                if (!EditorSceneManager.GetActiveScene().isDirty)
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }

        /// <summary>
        /// Helper function to create a GUIContent for button icons - decided whether to use Unity Standard or Pro Icons and adds the tooltip text
        /// </summary>
        /// <param name="key">Localization key to get the tooltip from</param>
        /// <param name="standardIcon">Icon to use for Unity Standard Skin</param>
        /// <param name="proIcon">Icon to use for Unity Pro Skin</param>
        /// <param name="editorUtils">Current Editor Utils class to pass in to look up the localized tooltip</param>
        /// <returns>GUIContent with correct texture and tooltip.</returns>
        public static GUIContent GetIconGUIContent(string key, Texture2D standardIcon, Texture2D proIcon, EditorUtils editorUtils)
        {
            Texture icon = null;
            if (EditorGUIUtility.isProSkin)
            {
                icon = proIcon;
            }
            else
            {
                icon = standardIcon;
            }

            return new GUIContent(icon, editorUtils.GetTooltip(key));

        }

        /// <summary>
        /// Displays the resource helper window with the given operation
        /// </summary>
        /// <param name="operation">The operation mode to open the window with.</param>
        public static void ShowResourceHelperWindow(GaiaResourceHelperOperation operation, Vector2 position)
        {
            var resourceHelper = EditorWindow.GetWindow<GaiaResourceHelper>(false, "Resource Helper");
            resourceHelper.Show();
            resourceHelper.position = new Rect(position, new Vector2(300, 200));
            resourceHelper.minSize = new Vector2(300, 200);
            resourceHelper.m_operation = operation;
        }

        /// <summary>
        /// Draws a line that can be used on curves and gradients to show where the current time of day is.
        /// </summary>
        /// <param name="currentRect"></param>
        /// <param name="currentTime"></param>
        /// <param name="lineHeight"></param>
        public static void DrawTimeOfDayLine(float currentTime, float lineHeight = 0f)
        {
            Rect currentRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(currentRect.x + EditorGUIUtility.labelWidth + Mathf.Lerp(0f, currentRect.width - EditorGUIUtility.labelWidth, currentTime), currentRect.y - EditorGUIUtility.singleLineHeight * lineHeight, 2f, EditorGUIUtility.singleLineHeight * (lineHeight + 1)), Color.red);
        }

        /// <summary>
        /// Used to draw the line on the kelvin gradient image.
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="heightValue"></param>
        /// <param name="lineHeight"></param>
        public static void DrawKelvinLine(float currentTime, float heightValue = 0f, float lineHeight = 0f)
        {
            Rect currentRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(currentRect.x + EditorGUIUtility.labelWidth + Mathf.Lerp(3.5f, currentRect.width - EditorGUIUtility.labelWidth - 3.5f, currentTime), currentRect.y - EditorGUIUtility.singleLineHeight * lineHeight, 2f, EditorGUIUtility.singleLineHeight * (lineHeight + 1) + heightValue), Color.green);
        }

        /// <summary>
        /// Allows to loads an asset from unitys hidden builtin-extra resources
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="aAssetPath"></param>
        /// <returns></returns>
        public static T LoadAssetFromUniqueAssetPath<T>(string aAssetPath) where T : UnityEngine.Object
        {
            if (aAssetPath.Contains("::"))
            {
                string[] parts = aAssetPath.Split(new string[] { "::" }, System.StringSplitOptions.RemoveEmptyEntries);
                aAssetPath = parts[0];
                if (parts.Length > 1)
                {
                    string assetName = parts[1];
                    System.Type t = typeof(T);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(aAssetPath)
                        .Where(i => t.IsAssignableFrom(i.GetType())).Cast<T>();
                    var obj = assets.Where(i => i.name == assetName).FirstOrDefault();
                    if (obj == null)
                    {
                        int id;
                        if (int.TryParse(parts[1], out id))
                            obj = assets.Where(i => i.GetInstanceID() == id).FirstOrDefault();
                    }
                    if (obj != null)
                        return obj;
                }
            }
            return AssetDatabase.LoadAssetAtPath<T>(aAssetPath);
        }
        public static string GetUniqueAssetPath(UnityEngine.Object aObj)
        {
            string path = AssetDatabase.GetAssetPath(aObj);
            if (!string.IsNullOrEmpty(aObj.name))
                path += "::" + aObj.name;
            else
                path += "::" + aObj.GetInstanceID();
            return path;
        }

        /// <summary>
        /// Handy layer mask interface - creates a EditorGUILayout Mask field
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static LayerMask LayerMaskField(GUIContent label, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }
            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }

        /// <summary>
        /// Handy layer mask interface - creates a EditorGUI Mask field
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static LayerMask LayerMaskFieldRect(Rect rect, GUIContent label, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }
            maskWithoutEmpty = EditorGUI.MaskField(rect, label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }
    }
}
