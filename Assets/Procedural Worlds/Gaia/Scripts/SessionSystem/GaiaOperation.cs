using UnityEngine;
using System.Collections;
using System;
using UnityEditor;

namespace Gaia
{

    public enum SessionPlaybackState {Queued, Started }

    /// <summary>
    /// A gaia operation - serialises and deserialises and executes a gaia operation
    /// </summary>
    [System.Serializable]
    public class GaiaOperation
    {

        /// <summary>
        /// Settings for a world creation operation
        /// </summary>
        private WorldCreationSettings m_worldCreationSettings = null;


        public WorldCreationSettings WorldCreationSettings
        { 
            get {
                if (m_worldCreationSettings == null)
                {
#if UNITY_EDITOR
                    m_worldCreationSettings = (WorldCreationSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(WorldCreationSettings));
#endif
                }
                return m_worldCreationSettings;
            } 
        }

        private StamperSettings m_stamperSettings = null;
        public StamperSettings StamperSettings
        {
            get
            {
                if (m_stamperSettings == null)
                {
#if UNITY_EDITOR
                    m_stamperSettings = (StamperSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(StamperSettings));
#endif
                }
                return m_stamperSettings;
            }
        }

        private SpawnOperationSettings m_spawnOperationSettings = null;
        public SpawnOperationSettings SpawnOperationSettings
        {
            get
            {
                if (m_spawnOperationSettings == null)
                {
#if UNITY_EDITOR
                    m_spawnOperationSettings = (SpawnOperationSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(SpawnOperationSettings));
#endif
                }
                return m_spawnOperationSettings;
            }
        }

        private FlattenOperationSettings m_flattenOperationSettings = null;
        public FlattenOperationSettings FlattenOperationSettings
        {
            get
            {
                if (m_flattenOperationSettings == null)
                {
#if UNITY_EDITOR
                    m_flattenOperationSettings = (FlattenOperationSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(FlattenOperationSettings));
#endif
                }
                return m_flattenOperationSettings;
            }
        }
        private UndoRedoOperationSettings m_undoRedoOperationSettings = null;
        public UndoRedoOperationSettings UndoRedoOperationSettings      {
            get
            {
                if (m_undoRedoOperationSettings == null)
                {
#if UNITY_EDITOR
                    m_undoRedoOperationSettings = (UndoRedoOperationSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(UndoRedoOperationSettings));
#endif
                }
                return m_undoRedoOperationSettings;
            }
        }

        private ClearOperationSettings m_clearOperationSettings = null;
        public ClearOperationSettings ClearOperationSettings
        {
            get
            {
                if (m_clearOperationSettings == null)
                {
#if UNITY_EDITOR
                    m_clearOperationSettings = (ClearOperationSettings) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(ClearOperationSettings));
#endif
                }
                return m_clearOperationSettings;
            }
        }

        private RemoveNonBiomeResourcesSettings m_removeNonBiomeResourcesSettings = null;
        public RemoveNonBiomeResourcesSettings RemoveNonBiomeResourcesSettings
        {
            get
            {
                if (m_removeNonBiomeResourcesSettings == null)
                {
#if UNITY_EDITOR
                    m_removeNonBiomeResourcesSettings = (RemoveNonBiomeResourcesSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(RemoveNonBiomeResourcesSettings));
#endif
                }
                return m_removeNonBiomeResourcesSettings;
            }
        }

        private ExportMaskMapOperationSettings m_exportMaskMapOperationSettings = null;
        public ExportMaskMapOperationSettings ExportMaskMapOperationSettings
        {
            get
            {
                if (m_exportMaskMapOperationSettings == null)
                {
#if UNITY_EDITOR
                    m_exportMaskMapOperationSettings = (ExportMaskMapOperationSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(ExportMaskMapOperationSettings));
#endif
                }
                return m_exportMaskMapOperationSettings;
            }
        }

        private ScriptableObject m_externalScriptableObject = null;
        public ScriptableObject ExternalOperationScriptableObject
        {
            get
            {
                if (m_externalScriptableObject == null)
                {
#if UNITY_EDITOR
                    m_externalScriptableObject = (ScriptableObject)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(scriptableObjectAssetGUID), typeof(ScriptableObject));
#endif
                }
                return m_externalScriptableObject;
            }
        }

        /// <summary>
        /// An optional description
        /// </summary>
        public string m_description;

        /// <summary>
        /// The types of operations we can record
        /// </summary>
        public enum OperationType { CreateWorld, FlattenTerrain, SmoothTerrain, ClearSpawns, Stamp, StampUndo, StampRedo, Spawn, RemoveNonBiomeResources,
            MaskMapExport,
            ClearWorld,
            ExportWorldMapToLocalMap,
            External
        }

        /// <summary>
        /// The operation type
        /// </summary>
        public OperationType m_operationType;

        /// <summary>
        /// Whether or not the operation is active
        /// </summary>
        public bool m_isActive = true;

        ///// <summary>
        ///// The name of the object that generated this operation
        ///// </summary>
        //public string m_generatedByName;

        ///// <summary>
        ///// The ID of the onject that generated this operation
        ///// </summary>
        //public string m_generatedByID;

        ///// <summary>
        ///// The type of object that generated this operation
        ///// </summary>
        //public string m_generatedByType;

        /// <summary>
        /// The list of terrains affected by this operation.
        /// </summary>
        public string[] m_affectedTerrainNames = new string[0];

        /// <summary>
        /// When the operation was recorded
        /// </summary>
        public string m_operationDateTime = DateTime.Now.ToString();


        /// <summary>
        /// GUID for the scriptable object that holds the actual settings data for the operation
        /// </summary>
        public string scriptableObjectAssetGUID;

        /// <summary>
        /// Whether or not we are folded out in the editor
        /// </summary>
        public bool m_isFoldedOut = false;


        public SessionPlaybackState sessionPlaybackState = SessionPlaybackState.Started;

        /// <summary>
        /// Whether the affected terrains section on the GUI is folded out or not
        /// </summary>
        public bool m_terrainsFoldedOut;

        /// <summary>
        /// Holds data from a serialized external action that was saved in the session.
        /// </summary>
        public byte[] m_serializedExternalAction;
    }
}