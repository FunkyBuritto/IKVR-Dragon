using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    public enum ScannerObjectType { Raw, Texture, Mesh, Terrain, Unkown }
    /// <summary>
    /// Scanning system - creates stamps
    /// </summary>
    public class Scanner : MonoBehaviour
    {
        //Public
        public string m_exportFolder;
        public string m_exportFileName = "/Scan " + string.Format("{0:YYYYMMDD-hhmmss}", DateTime.Now);

        //public string m_featureName = string.Format("{0}",DateTime.Now);
        public float m_baseLevel = 0f;
        public float m_scanResolution = 0.1f; //Every 10 cm
        public float m_lastScanResolution = 0.1f; //the scan resolution that was used last

        public Material m_previewMaterial;
        public bool m_exportTextureAlso = false;
        public bool m_exportBytesData = false;
        public HeightMap m_scanMap;
        public GaiaConstants.GaiaProWaterReflectionsQuality m_textureExportResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024;
        public bool m_boundsSet = false;
        public bool m_normalize = true;
        public bool m_objectScanned = false;
        public ScannerObjectType m_scannerObjectType = ScannerObjectType.Unkown;
        public GameObject m_lastScannedMesh;
        //Private
        private Bounds m_scanBounds;
        private int m_scanWidth = 1;
        private int m_scanDepth = 1;
        private int m_scanHeight = 500;
        
        private Vector3 m_groundOffset = Vector3.zero;
        private Vector3 m_groundSize = Vector3.zero;
        [SerializeField] private MeshFilter m_meshFilter;
        [SerializeField] private MeshRenderer m_meshRenderer;

        #region Unity Functions

        /// <summary>
        /// Draw gizmos, and make updates / overrides
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            //Housekeep
            UpdateScanner();

            if (m_scanMap == null)
            {
                return;
            }

            //Draw a border to show what we are working on
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(m_scanBounds.center, m_scanBounds.size);

            //Draw the ground plane
            if (m_baseLevel > 0)
            {
                m_groundOffset = m_scanBounds.center;
                m_groundOffset.y = m_scanBounds.min.y + (m_scanBounds.max.y - m_scanBounds.min.y) * m_baseLevel;
                m_groundSize = m_scanBounds.size;
                m_groundSize.y = 0.01f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(m_groundOffset, m_groundSize);
            }
        }
        /// <summary>
        /// Loads on enable
        /// </summary>
        private void OnEnable()
        {
            if (String.IsNullOrEmpty(m_exportFolder))
            {
                m_exportFolder = GaiaDirectories.GetScannerExportDirectory();
            }
            SetOrCreateMeshComponents();
        }

        /// <summary>
        /// Knock ourselves out if we happen to be left on in play mode
        /// </summary>
        private void Awake()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Clean Up/Save Functions

        /// <summary>
        /// Reset the scanner
        /// </summary>
        public void ResetData()
        {
            m_exportFolder = GaiaDirectories.GetScannerExportDirectory();
            m_exportFileName = "/Scan " + string.Format("{0:YYYYMMDD-hhmmss}", DateTime.Now);
            m_boundsSet = false;
            m_scanBounds = new Bounds(GetPosition(gameObject, null), Vector3.one * 10f);
            m_scanWidth = m_scanDepth = m_scanHeight = 0;
            m_baseLevel = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        /// <summary>
        /// Clears all the data and generated mesh
        /// </summary>
        public void Clear()
        {
            ResetData();
            m_scanMap = null;
            if (m_meshFilter != null)
            {
                m_meshFilter.sharedMesh = null;
            }
        }
        /// <summary>
        /// Save the stamp
        /// </summary>
        /// <returns>Path of saved stamp</returns>
        public string SaveScan()
        {
            if (m_scanMap == null || !m_scanMap.HasData())
            {
                Debug.LogWarning("Cant save scan as none has been loaded");
                return null;
            }

            #if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Generating Texture", "Generating texture", 0.25f);
#endif

            //work with a copy for the export - don't want to normalize etc. source data
            HeightMap heightmapCopy = new HeightMap(m_scanMap);

            heightmapCopy.AddClamped(0f, m_baseLevel, 1f);

            if (m_normalize)
            {
                heightmapCopy.Normalise();
            }

            //Save preview
            string fullpath = m_exportFolder + "/" + m_exportFileName;
            GaiaUtils.CompressToMultiChannelFileImage(fullpath, heightmapCopy, heightmapCopy, heightmapCopy, null, TextureFormat.RGBAFloat, GaiaConstants.ImageFileType.Exr, m_baseLevel);

            GaiaUtils.SetDefaultStampImportSettings(fullpath + ".exr");


#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Generating Texture", "Compressing texture", 0.5f);
#endif
            GaiaUtils.CompressToSingleChannelFileImage(heightmapCopy.Heights(), m_exportFolder, GaiaConstants.fmtHmTextureFormat, m_exportTextureAlso, false);

            //Save stamp
            if (m_exportBytesData)
            {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Generating Texture", "Generating bytes data", 0.75f);
#endif
                m_exportFolder += ".bytes";
                float [] metaData = new float[5];
                metaData[0] = m_scanWidth;
                metaData[1] = m_scanDepth;
                metaData[2] = m_scanHeight;
                metaData[3] = m_scanResolution;
                metaData[4] = m_baseLevel;
                byte[] byteData = new byte[metaData.Length * 4];
                Buffer.BlockCopy(metaData, 0, byteData, 0, byteData.Length);
                heightmapCopy.SetMetaData(byteData);
                heightmapCopy.SaveToBinaryFile(m_exportFolder);
            }

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif

            return fullpath;
        }

        #endregion

        #region Load Data Functions

        /// <summary>
        /// Load the raw file at the path given
        /// </summary>
        /// <param name="path">Full path of the raw file</param>
        public void LoadRawFile(string path, GaiaConstants.RawByteOrder byteOrder, ref GaiaConstants.RawBitDepth bitDepth, ref int resolution)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Must supply a valid path. Raw load Aborted!");
            }

            //Clear out the old
            ResetData();

            //Load up the new
            m_scanMap = new HeightMap();
            m_scanMap.LoadFromRawFile(path, byteOrder, ref bitDepth, ref resolution);
            if (m_scanMap.HasData() == false)
            {
                Debug.LogError("Unable to load raw file. Raw load aborted.");
                return;
            }

            m_scanWidth = m_scanMap.Width();
            m_scanDepth = m_scanMap.Depth();
            m_scanHeight = m_scanWidth / 2;
            m_scanResolution = 0.1f;
            m_scanBounds = new Bounds(GetPosition(gameObject, null), new Vector3(m_scanWidth * m_scanResolution, m_scanWidth * m_scanResolution, m_scanDepth * m_scanResolution));
            //m_baseLevel = m_scanMap.GetBaseLevel();

            SetOrCreateMeshComponents();
            m_meshFilter.sharedMesh = GaiaUtils.CreateMesh(m_scanMap.Heights(), m_scanBounds.size);
            if (m_previewMaterial != null)
            {
                m_previewMaterial.hideFlags = HideFlags.HideInInspector;
                m_meshRenderer.sharedMaterial = m_previewMaterial;
            }

            gameObject.transform.position = m_scanBounds.center;
            m_exportFileName = path.Substring(path.LastIndexOf('/'));
            m_boundsSet = true;
        }

        /// <summary>
        /// Load the texture file provided
        /// </summary>
        /// <param name="texture">Texture file to load</param>
        public void LoadTextureFile(Texture2D texture)
        {
            //Check not null
            if (texture == null)
            {
                Debug.LogError("Must supply a valid texture! Texture load aborted.");
                return;
            }

            //Clear out the old
            ResetData();

            m_scanMap = new UnityHeightMap(texture);
            if (m_scanMap.HasData() == false)
            {
                Debug.LogError("Unable to load Texture file. Texture load aborted.");
                return;
            }

            m_scanWidth = m_scanMap.Width();
            m_scanDepth = m_scanMap.Depth();
            m_scanHeight = m_scanWidth / 2;
            m_scanResolution = 0.1f;
            m_scanBounds = new Bounds(GetPosition(gameObject, null), new Vector3(texture.width / 2, m_scanWidth * m_scanResolution, texture.height / 2));
            //m_baseLevel = m_scanMap.GetBaseLevel();

            SetOrCreateMeshComponents();
            m_meshFilter.sharedMesh = GaiaUtils.CreateMesh(m_scanMap.Heights(), m_scanBounds.size);
            if (m_previewMaterial != null)
            {
                m_previewMaterial.hideFlags = HideFlags.HideInInspector;
                m_meshRenderer.sharedMaterial = m_previewMaterial;
            }

            gameObject.transform.position = m_scanBounds.center;
            m_exportFileName = texture.name;
            m_boundsSet = true;
        }

        /// <summary>
        /// Load the terrain provided
        /// </summary>
        /// <param name="texture">Terrain to load</param>
        public void LoadTerain(Terrain terrain)
        {
            //Check not null
            if (terrain == null)
            {
                Debug.LogError("Must supply a valid terrain! Terrain load aborted.");
                return;
            }

            //Clear out the old
            ResetData();

            m_scanMap = new UnityHeightMap(terrain);
            if (m_scanMap.HasData() == false)
            {
                Debug.LogError("Unable to load terrain file. Terrain load aborted.");
                return;
            }

            m_scanMap.Flip(); //Undo unity terrain shenannigans

            m_scanWidth = m_scanMap.Width();
            m_scanDepth = m_scanMap.Depth();
            m_scanHeight = (int)terrain.terrainData.size.y;
            m_scanResolution = 0.1f;
            //m_scanBounds = new Bounds(GetPosition(gameObject), new Vector3(m_scanWidth * m_scanResolution, m_scanWidth * m_scanResolution, m_scanDepth * m_scanResolution));
            m_scanBounds = new Bounds(GetPosition(gameObject, terrain, true), new Vector3(terrain.terrainData.size.x, terrain.terrainData.size.y, terrain.terrainData.size.z));
            //m_baseLevel = m_scanMap.GetBaseLevel();

            SetOrCreateMeshComponents();
            m_meshFilter.sharedMesh = GaiaUtils.CreateMesh(m_scanMap.Heights(), m_scanBounds.size);
            if (m_previewMaterial != null)
            {
                m_previewMaterial.hideFlags = HideFlags.HideInInspector;
                m_meshRenderer.sharedMaterial = m_previewMaterial;
            }

            gameObject.transform.position = m_scanBounds.center;
            m_exportFileName = terrain.name;
            m_boundsSet = true;
        }

        /// <summary>
        /// Load the object provided
        /// </summary>
        /// <param name="go">Terrain to load</param>
        public void LoadGameObject(GameObject go)
        {
            //Check not null
            if (go == null)
            {
                Debug.LogError("Must supply a valid game object! GameObject load aborted.");
                return;
            }

            //Clear out the old
            ResetData();

            //Duplicate the object
            GameObject workingGo = GameObject.Instantiate(go);

            workingGo.transform.position = transform.position;
            workingGo.transform.localRotation = Quaternion.identity;
            workingGo.transform.localScale = Vector3.one;

            //Delete any old colliders
            Collider[] colliders = workingGo.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                DestroyImmediate(c);
            }

            //Now add mesh colliders to all active game objects for the most accurate possible scanning
            Transform[] transforms = workingGo.GetComponentsInChildren<Transform>();
            foreach (Transform child in transforms)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.AddComponent<MeshCollider>();
                }
            }

            //Calculate bounds
            m_scanBounds.center = workingGo.transform.position;
            m_scanBounds.size = Vector3.zero;
            foreach (MeshCollider c in workingGo.GetComponentsInChildren<MeshCollider>())
            {
                m_scanBounds.Encapsulate(c.bounds);
            }

            //Update scan array details - dont need to allocate mem until we scan
            m_scanWidth = (int)(Mathf.Ceil(m_scanBounds.size.x * (1f / m_scanResolution)));
            m_scanHeight = (int)(Mathf.Ceil(m_scanBounds.size.y * (1f / m_scanResolution)));
            m_scanDepth = (int)(Mathf.Ceil(m_scanBounds.size.z * (1f / m_scanResolution)));

            //Now scan the object
            m_scanMap = new HeightMap(m_scanWidth, m_scanDepth);
            Vector3 scanMin = m_scanBounds.min;
            Vector3 scanPos = scanMin;
            scanPos.y = m_scanBounds.max.y + 1;
            RaycastHit scanHit;

#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Processing Mesh", "Processing mesh data", 0f);
#endif

            int count = 0;
            int totalNumberOfRays = m_scanWidth * m_scanDepth;
            //Perform the scan - only need to store hits as float arrays inherently zero
            for (int x = 0; x < m_scanWidth; x++)
            {
                scanPos.x = scanMin.x + (m_scanResolution * (float)x);
                for (int z = 0; z < m_scanDepth; z++)
                {
                    scanPos.z = scanMin.z + (m_scanResolution * (float)z);
                    if (Physics.Raycast(scanPos, Vector3.down, out scanHit, m_scanBounds.size.y + 1))
                    {
                        m_scanMap[x, z] = 1f - ((scanHit.distance -1f) / m_scanBounds.size.y);
                    }
                    count++;
                }
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Processing Mesh", "Processing mesh data", (float)count / (float)totalNumberOfRays);
#endif
            }

            //Now delete the scanned clone
            DestroyImmediate(workingGo);

            //Nad make sure we had some data
            if (m_scanMap.HasData() == false)
            {
                Debug.LogError("Unable to scan GameObject. GameObject load aborted.");
                return;
            }

            m_scanBounds = new Bounds(GetPosition(gameObject, null), new Vector3(m_scanWidth * m_scanResolution, m_scanBounds.size.y, m_scanDepth * m_scanResolution));//m_scanWidth * m_scanResolution * 0.4f, m_scanDepth * m_scanResolution));
            //m_baseLevel = m_scanMap.GetBaseLevel();

            SetOrCreateMeshComponents();
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Processing Mesh", "Creating mesh from data", 0.95f);
#endif
            m_meshFilter.sharedMesh = GaiaUtils.CreateMesh(m_scanMap.Heights(), m_scanBounds.size);
            if (m_previewMaterial != null)
            {
                m_previewMaterial.hideFlags = HideFlags.HideInInspector;
                m_meshRenderer.sharedMaterial = m_previewMaterial;
            }

            gameObject.transform.position = m_scanBounds.center;
            m_boundsSet = true;
            m_lastScanResolution = m_scanResolution;
            m_exportFileName = go.name;
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }

        #endregion

        #region Utils

        /// <summary>
        /// Creates or gets the mesh filter and renderer
        /// </summary>
        private void SetOrCreateMeshComponents()
        {
            //Mesh filter
            if (m_meshFilter == null)
            {
                m_meshFilter = GetComponent<MeshFilter>();
                if (m_meshFilter == null)
                {
                    m_meshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }
            m_meshFilter.hideFlags = HideFlags.HideInInspector;

            //Mesh renderer
            if (m_meshRenderer == null)
            {
                m_meshRenderer = GetComponent<MeshRenderer>();
                if (m_meshRenderer == null)
                {
                    m_meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
            }
            m_meshRenderer.hideFlags = HideFlags.HideInInspector;
        }
        private Vector3 GetPosition(GameObject scannerObj, Terrain terrain, bool terrainMode = false)
        {
            Vector3 position = Vector3.zero;
            if (terrainMode)
            {
                if (terrain != null)
                {
                    position.y = terrain.terrainData.size.x / 2f;
                }
            }
            else
            {
                GaiaSceneInfo sceneInfo = null;
                if (Terrain.activeTerrains.Length > 0)
                {
                    sceneInfo = GaiaSceneInfo.GetSceneInfo();
                } 
                if (sceneInfo != null)
                {
                    position.y = sceneInfo.m_seaLevel;
                    scannerObj.transform.position = position;
                }
                else
                {
                    scannerObj.transform.position = new Vector3(0f, 50f, 0f);
                }
            }


            return position;
        }

        /// <summary>
        /// Update the scanner settings, fix any location and rotation, and perform any other housekeeping
        /// </summary>
        private void UpdateScanner()
        {
            //Reset rotation and scaling on scanner 
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (m_boundsSet)
            {
                m_scanBounds.center = transform.position;
            }
        }
        /// <summary>
        /// Select or create a scanner
        /// </summary>
        public static GameObject CreateScanner()
        {
            GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
            GameObject scannerObj = GameObject.Find("Scanner");
            if (scannerObj == null)
            {
                scannerObj = new GameObject("Scanner");
                scannerObj.transform.parent = gaiaObj.transform;
                GaiaSceneInfo sceneInfo = null;
                if (Terrain.activeTerrains.Length > 0)
                {
                    sceneInfo = GaiaSceneInfo.GetSceneInfo();
                } 
                if (sceneInfo != null)
                {
                    Vector3 position = scannerObj.transform.position;
                    position.y = sceneInfo.m_seaLevel;
                    scannerObj.transform.position = position;
                }
                else
                {
                    scannerObj.transform.position = new Vector3(0f, 50f, 0f);
                }
                Scanner scanner = scannerObj.AddComponent<Scanner>();

                #if UNITY_EDITOR
                //Load the material to draw it
                string matPath = GaiaUtils.GetAssetPath("GaiaScannerMaterial.mat");
                if (!string.IsNullOrEmpty(matPath))
                {
                    scanner.m_previewMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                }
                #endif
            }
            return scannerObj;
        }

        #endregion
    }
}