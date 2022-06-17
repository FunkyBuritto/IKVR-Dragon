using Gaia.Internal;
using PWCommon4;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(GaiaMaintenanceProfile))]
    public class GaiaMaintenanceProfileEditor : PWEditor
    {
        private GaiaMaintenanceProfile m_profile;
        private object m_editorUtils;
        private bool m_enableEditMode;

        private void OnEnable()
        {
            //Get Gaia Lighting Profile object
            m_profile = (GaiaMaintenanceProfile)target;
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            m_enableEditMode = System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities"));
        }

        public override void OnInspectorGUI()
        {
            if (m_enableEditMode)
            {
                DrawDefaultInspector();
            }
        }

#if UNITY_EDITOR
        //[MenuItem("Assets/Create/Procedural Worlds/Gaia/Maintenance Profile")]
        //public static void CreateMaintenanceProfiles()
        //{
        //    GaiaMaintenanceProfile asset = ScriptableObject.CreateInstance<GaiaMaintenanceProfile>();
        //    AssetDatabase.CreateAsset(asset, "Assets/Gaia Maintenance Profile.asset");
        //    AssetDatabase.SaveAssets();
        //    EditorUtility.FocusProjectWindow();
        //    Selection.activeObject = asset;
        //}
#endif
    }
}
