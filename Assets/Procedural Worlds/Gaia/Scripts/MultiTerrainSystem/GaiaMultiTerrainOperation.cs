using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEngine.SocialPlatforms;
using static Gaia.GaiaConstants;

namespace Gaia
{
    /// <summary>
    /// Enum to signal the type of operation that is supposed to be performed across multiple terrrains.
    /// </summary>
    public enum MultiTerrainOperationType { Heightmap, Texture, Tree, TerrainDetail, GameObject, BakedMask, AffectedTerrains }

    /// <summary>
    /// Dictionary Key to bind a terrain that is affected by a specific operation type. 
    /// </summary>
    public struct TerrainOperation
    {
        public Terrain terrain;
        public MultiTerrainOperationType operationType;
    }

    /// <summary>
    /// The affected pixels on the respective map (heightmap, splatmap, etc.) when a multiterrain operation is performed.
    /// </summary>
    public class AffectedPixels
    {
        /// <summary>
        /// The coordinates in the total operation for these affected pixels
        /// </summary>
        public Vector2Int pixelCoordinate;

        /// <summary>
        /// The affected pixels on the local map on the terrain.
        /// </summary>
        /// 
        public RectInt affectedLocalPixels;

        /// <summary>
        /// The affected pixels in the total operation.
        /// </summary>
        public RectInt affectedOperationPixels;


        /// <summary>
        /// Splatmap ID / Layer ID for texture operations.
        /// </summary>
        public int splatMapID = -1;

        /// <summary>
        /// Channel ID (color channel of the splatmap image) for texture oprations.
        /// </summary>
        public int channelID = -1;

        /// <summary>
        /// Flag to set when performing compute shader simulations of an operation, the simulation can tell if this operation will actually produce a result in the stored pixels.
        /// </summary>
        public bool simulationPositive = true;

        /// <summary>
        /// Returns true if this area actually contains any affected pixels
        /// </summary>
        /// <returns></returns>
        public bool IsAffected()
        {
            return !(affectedLocalPixels.width == 0 || affectedLocalPixels.height == 0) && simulationPositive;
        }
    }

    /// <summary>
    /// Handles getting and setting height, textures, trees, etc. in a multi-terrain context
    /// </summary>
    public class GaiaMultiTerrainOperation
    {
        /// <summary>
        /// The terrain from which the multi-terrain operation originates (where the stamper / spawner is placed over atm)
        /// </summary>
        public Terrain m_originTerrain;

        /// <summary>
        /// The transform from which the multi-terrain operation originates (the stamper / spawner transform usually)
        /// </summary>
        public Transform m_originTransform;

        /// <summary>
        /// The range of the operation.
        /// </summary>
        public float m_range;


        /// <summary>
        /// Whether this operation is intended to take place on the world map. WM ops will only affect the world map terrains, and vice versa.
        /// </summary>
        public bool m_isWorldMapOperation = false;

        /// <summary>
        /// List of all terrain data objects affected by a splatmap change. This list can then be processed for splatmap syncing on closing the operation.
        /// </summary>
        private List<TerrainData> affectedSplatmapData = new List<TerrainData>();

        /// <summary>
        /// List of all terrain objects affected by a heightmpa change. This list can then be processed for heightmap syncing on closing the operation.
        /// </summary>
        public List<Terrain> affectedHeightmapData = new List<Terrain>();

        /// <summary>
        /// List of all terrains that are marked as "valid to modify" for this operation. If this list has no entries, all terrains in the operation area are assumed to be valid.
        /// </summary>
        public List<string> m_validTerrainNames = new List<string>();

        /// <summary>
        /// List of terrains with spawn rules that are missing resources on that terrain - used during spawning to add those resources "on demand" when a spawn happens.
        /// </summary>
        public List<TerrainMissingSpawnRules> m_terrainsMissingSpawnRules = new List<TerrainMissingSpawnRules>();


        //Render Textures to hold queried existing data from this multi-terrain operation

        public RenderTexture RTheightmap;
        public RenderTexture RTnormalmap;
        public RenderTexture RTterrainTree;
        public RenderTexture RTgameObject;
        public RenderTexture RTdetailmap;
        public RenderTexture RTtextureSplatmap;
        public RenderTexture RTbakedMask; 


        public RectInt m_heightmapPixels;
        private RectInt m_texturePixels;
        private RectInt m_terrainTreePixels;
        private RectInt m_bakedMaskPixels;
        private RectInt m_gameObjectPixels;
        private RectInt m_terrainDetailPixels;

        private Vector2 m_heightmapPixelSize;
        
        private Vector2 m_texturePixelSize;
        private Vector2 m_terrainDetailPixelSize;
        private Vector2 m_terrainTreePixelSize;
        private Vector2 m_gameObjectPixelSize;
        private Vector2 m_collisionPixelSize;

        private BrushTransform m_heightmapBrushTransform;
        private BrushTransform m_textureBrushTransform;
        private BrushTransform m_terrainTreeBrushTransform;
        private BrushTransform m_bakedMaskBrushTransform;
        private BrushTransform m_gameObjectBrushTransform;
        public BrushTransform m_terrainDetailBrushTransform;

        /// <summary>
        /// Holds the affected pixels per terrain per operation.
        /// </summary>
        public Dictionary<TerrainOperation, AffectedPixels> affectedTerrainPixels = new Dictionary<TerrainOperation, AffectedPixels>();

        private static Material m_GaiaStamperPreviewMaterial;
        private static Material m_GaiaSpawnerPreviewMaterial;

        private int m_originalMasterTextureLimit = 0;
        


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="originTerrain">The origin terrain that serves as a reference point for the whole operation.</param>
        /// <param name="toolTransform">The transform of the tool (spawner / stamper) that calls the whole operation.</param>
        /// <param name="range">The range of the operation</param>
        /// <param name="fullTextureQuality">Whether this operation requires the full texture quality or not - this is to control a workaround in case the user runs the editor in low-res texture settings which affects render textures as well.</param>
        /// <param name="validTerrainNames">A list of terrain names this operation is allowed to modify - if no list is supplied, all terrains in the operation area are automatically declared as valid.</param>
        public GaiaMultiTerrainOperation(Terrain originTerrain, Transform toolTransform, float range, bool fullTextureQuality = false, List<string> validTerrainNames = null)
        {
            //All operations need to be executed with masterTextureLimit = 0, else there are quality / functional issues in the render texture processing!
            m_originalMasterTextureLimit = QualitySettings.masterTextureLimit;
            if (fullTextureQuality)
            {
                QualitySettings.masterTextureLimit = 0;
            }

            m_originTerrain = originTerrain;
            m_originTransform = toolTransform;
            m_range = range;
            if (validTerrainNames != null)
            {
                m_validTerrainNames = validTerrainNames;
            }
        }

        #region OPERATIONS

        #region HEIGHTMAP

        /// <summary>
        /// Lightweight Operation without overhead that just fetches the terrains touched by the current operation
        /// Can be used to determine which terrains will be affected by stamping/spawning before executing an operation
        /// </summary>
        public void GetAffectedTerrainsOnly()
        {
            int heightmapResolution = m_originTerrain.terrainData.heightmapResolution;

            m_heightmapPixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / (heightmapResolution),
            m_originTerrain.terrainData.size.z / (heightmapResolution));

            m_heightmapBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_heightmapPixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_heightmapBrushTransform.GetBrushXYBounds(), heightmapResolution, heightmapResolution, 0);
            AddAffectedTerrainPixels(m_heightmapPixels, MultiTerrainOperationType.AffectedTerrains, heightmapResolution, heightmapResolution);
        }


        /// <summary>
        /// Gets the heightmap data for this operation and stores it in the RTheightmap render texture.
        /// </summary>
        public void GetHeightmap()
        {
            RenderTexture previousRT = RenderTexture.active;
            if (m_originTerrain == null)
            {
                Debug.LogError("No origin terrain for this operation!");
                return;
            }
            int heightmapResolution = m_originTerrain.terrainData.heightmapResolution;

            m_heightmapPixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / (heightmapResolution),
            m_originTerrain.terrainData.size.z / (heightmapResolution));

            m_heightmapBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_heightmapPixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_heightmapBrushTransform.GetBrushXYBounds(), heightmapResolution, heightmapResolution, 0);


            CreateDefaultRenderTexture(ref RTheightmap, m_heightmapPixels.width, m_heightmapPixels.height, Terrain.heightmapRenderTextureFormat);

          

            AddAffectedTerrainPixels(m_heightmapPixels, MultiTerrainOperationType.Heightmap, heightmapResolution, heightmapResolution);
            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();
            RenderTexture.active = RTheightmap;
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, m_heightmapPixels.width, 0, m_heightmapPixels.height);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Heightmap);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                Texture originTexture = entry.Key.terrain.terrainData.heightmapTexture;
                if ((originTexture.width != heightmapResolution) || (originTexture.height != heightmapResolution))
                {
                    Debug.LogWarning(String.Format("Mismatched heightmap resolutions between terrains: The terrain {0} does use a different heightmap resolution than the origin terrain, this terrain will be ignored.", entry.Key.terrain.name));
                    continue;
                }

                FilterMode oldFilterMode = originTexture.filterMode;

                originTexture.filterMode = FilterMode.Point;

                blitMaterial.SetTexture("_MainTex", originTexture);
                blitMaterial.SetPass(0);
                CopyIntoPixels(entry.Value.affectedOperationPixels, entry.Value.affectedLocalPixels, originTexture);

                originTexture.filterMode = oldFilterMode;
            }

            GL.PopMatrix();

            RenderTexture.active = previousRT;

        }

        public static void CreateDefaultRenderTexture(ref RenderTexture renderTex, int width, int height, RenderTextureFormat renderTextureFormat)
        {
            if (renderTex != null)
            {
                RenderTexture.ReleaseTemporary(renderTex);
            }
            renderTex = RenderTexture.GetTemporary(width, height, 0, renderTextureFormat, RenderTextureReadWrite.Linear);
            renderTex.wrapMode = TextureWrapMode.Clamp;
            renderTex.filterMode = FilterMode.Bilinear;
        }

        /// <summary>
        /// Sets the heightmap across the operation area according to the information in the supplied Render Texture.
        /// </summary>
        /// <param name="newHeightmapRT">The render texture containing the new target heightmap.</param>
        public void SetHeightmap(RenderTexture newHeightmapRT)
        {
            var previousRT = RenderTexture.active;
            RenderTexture.active = newHeightmapRT;
            int heightmapResolution = m_originTerrain.terrainData.heightmapResolution;
            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Heightmap);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                //can't stamp a negative width / height
                if (entry.Value.affectedOperationPixels.width < 0 || entry.Value.affectedOperationPixels.height <0)
                {
                    continue;
                }

                var terrainData = entry.Key.terrain.terrainData;
                if (terrainData.heightmapResolution != heightmapResolution)
                {
                    Debug.LogWarning(String.Format("Mismatched heightmap resolutions between terrains: The terrain {0} does use a different heightmap resolution than the origin terrain, this terrain will be ignored.", entry.Key.terrain.name));
                    continue;
                }
                terrainData.CopyActiveRenderTextureToHeightmap(entry.Value.affectedOperationPixels, entry.Value.affectedLocalPixels.min, entry.Key.terrain.drawInstanced ? TerrainHeightmapSyncControl.None : TerrainHeightmapSyncControl.HeightOnly);
                if (!affectedHeightmapData.Contains(entry.Key.terrain))
                {
                    affectedHeightmapData.Add(entry.Key.terrain);
                }
            }

            RenderTexture.active = previousRT;

            RenderTexture.ReleaseTemporary(RTheightmap);
            RTheightmap = null;
        }

        #endregion

        #region NORMALMAP

        /// <summary>
        /// Gets the normal map data for this operation and stores it in the RTnormalmap render texture.
        /// </summary>
        public void GetNormalmap()
        {

            RenderTexture previousRT = RenderTexture.active;
            int heightmapResolution = m_originTerrain.terrainData.heightmapResolution;

            m_heightmapPixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / (heightmapResolution),
            m_originTerrain.terrainData.size.z / (heightmapResolution));

            m_terrainDetailBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_heightmapPixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_terrainDetailBrushTransform.GetBrushXYBounds(), heightmapResolution, heightmapResolution, 0);

            if (RTnormalmap != null)
            {
                RenderTexture.ReleaseTemporary(RTnormalmap);
            }
            CreateDefaultRenderTexture(ref RTnormalmap, m_heightmapPixels.width, m_heightmapPixels.height, Terrain.normalmapRenderTextureFormat);

            Material mat = TerrainPaintUtility.GetBlitMaterial();

            RenderTexture.active = RTnormalmap;
            GL.Clear(false, true, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, m_heightmapPixels.width, 0, m_heightmapPixels.height);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Heightmap);

            foreach (var entry in relevantEntries)
            {
                Texture originTexture = entry.Key.terrain.normalmapTexture;
                if (originTexture == null)
                {
                    Debug.LogWarning("Normal maps missing on terrain '" + entry.Key.terrain.name + "'. Please enable the 'Draw Instanced' setting in the terrain options while using the stamper / spawner, otherwise not all masks will function properly.");
                    continue;
                }

                if (!entry.Value.IsAffected())
                    continue;

                if ((originTexture.width != heightmapResolution) || (originTexture.height != heightmapResolution))
                {
                    Debug.LogWarning(String.Format("Mismatched heightmap resolutions between terrains: The terrain {0} does use a different heightmap resolution than the origin terrain, this terrain will be ignored.", entry.Key.terrain.name));
                    continue;
                }

                FilterMode oldFilterMode = originTexture.filterMode;

                originTexture.filterMode = FilterMode.Point;

                mat.SetTexture("_MainTex", originTexture);
                mat.SetPass(0);

                CopyIntoPixels(entry.Value.affectedOperationPixels, entry.Value.affectedLocalPixels, originTexture);

                originTexture.filterMode = oldFilterMode;
            }

            GL.PopMatrix();

            RenderTexture.active = previousRT;

        }

        #endregion

        #region SPLATMAP

        /// <summary>
        /// Gets the splatmap data for this operation and the passed terrain layer and stores it in the RTtexturesplatmap render texture.
        /// </summary>
        public void GetSplatmap(TerrainLayer layer)
        {
            RenderTexture currentRT = RenderTexture.active;

           

            int controlTextureResolution = m_originTerrain.terrainData.alphamapResolution;

            m_texturePixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / (controlTextureResolution - 1.0f),
            m_originTerrain.terrainData.size.z / (controlTextureResolution - 1.0f));

            m_textureBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_texturePixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_textureBrushTransform.GetBrushXYBounds(), controlTextureResolution, controlTextureResolution,0);

            if (RTtextureSplatmap != null)
            {
                RenderTexture.ReleaseTemporary(RTtextureSplatmap);
            }

            CreateDefaultRenderTexture(ref RTtextureSplatmap, m_texturePixels.width, m_texturePixels.height, RenderTextureFormat.R8);

            AddAffectedTerrainPixels(m_texturePixels, MultiTerrainOperationType.Texture, controlTextureResolution, controlTextureResolution, layer);

            //Without any layer we are done at this point, makes no sense to copy non-existent data around.
            if (layer == null)
                return;

            RenderTexture.active = RTtextureSplatmap;
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, m_texturePixels.width, 0, m_texturePixels.height);



            Material mat = TerrainPaintUtility.GetCopyTerrainLayerMaterial();

            Vector4[] lmArray = {   new Vector4(1, 0, 0, 0),
                                    new Vector4(0, 1, 0, 0),
                                    new Vector4(0, 0, 1, 0),
                                    new Vector4(0, 0, 0, 1) };

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Texture);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                //Entry does not have the prototype on the terrain yet
                if (entry.Value.splatMapID == -1 || entry.Value.channelID == -1)
                {
                    continue;
                }

                Texture originTexture = TerrainPaintUtility.GetTerrainAlphaMapChecked(entry.Key.terrain, entry.Value.splatMapID);
                if ((originTexture.width != controlTextureResolution) || (originTexture.height != controlTextureResolution))
                {
                    Debug.LogWarning("Mismatched control texture resolution on one of the terrains. Expected:( " +
                        originTexture.width + " x " + originTexture.height + ") Found: (" + controlTextureResolution + " x " + controlTextureResolution + ")",
                        entry.Key.terrain);
                    continue;
                }

                FilterMode oldFilterMode = originTexture.filterMode;
                originTexture.filterMode = FilterMode.Point;


                mat.SetVector("_LayerMask", lmArray[entry.Value.channelID]);
                mat.SetTexture("_MainTex", originTexture);
                mat.SetPass(0);

                CopyIntoPixels(entry.Value.affectedOperationPixels, entry.Value.affectedLocalPixels, originTexture);

                originTexture.filterMode = oldFilterMode;
            }

            GL.PopMatrix();
            
            RenderTexture.active = currentRT;

        }

        /// <summary>
        /// Sets the splatmap across the operation area according to the information in the supplied Render Texture. Note that this will always affect the texture layer that was fetched last via GetSplatmap().
        /// </summary>
        /// <param name="paintRenderTexture">The render texture containing the new target splatmap.</param>
        /// <param name="centerTerrainOnly">Should the splatmap be applied to the center terrain of the operation only?</param>
        /// <returns>True if canceled</returns>
        public bool SetSplatmap(RenderTexture paintRenderTexture, Spawner spawner, SpawnRule spawnRule, bool centerTerrainOnly)
        {
            bool cancel = false;

            Vector4[] layerMasks = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

            Material mat = TerrainPaintUtility.GetCopyTerrainLayerMaterial();

            int controlTextureResolution = m_originTerrain.terrainData.alphamapResolution;

            var rtd = new RenderTextureDescriptor(paintRenderTexture.width, paintRenderTexture.height, RenderTextureFormat.ARGB32);
            rtd.useMipMap = false;
            rtd.autoGenerateMips = false;
            rtd.sRGB = false;

            RenderTexture paintTarget = RenderTexture.GetTemporary(rtd);
            RenderTexture.active = paintTarget;

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Texture && (x.Key.terrain == m_originTerrain || !centerTerrainOnly));

            //int completedEntries = 0;

            foreach (var entry in relevantEntries)
            {
                //cancel = SpawnProgressBar.UpdateSpawnRuleProgress((float)relevantEntries.Count() / (float)completedEntries);
                //if (!cancel)
                //{


                    if (!entry.Value.IsAffected())
                        continue;

                    if (entry.Value.splatMapID == -1 || entry.Value.channelID == -1)
                        continue;

                    RectInt paintRect = entry.Value.affectedOperationPixels;

                    Rect sourceRect = new Rect(
                        paintRect.x / (float)m_texturePixels.width,
                        paintRect.y / (float)m_texturePixels.height,
                        paintRect.width / (float)m_texturePixels.width,
                        paintRect.height / (float)m_texturePixels.height);

                    paintRenderTexture.filterMode = FilterMode.Point;

                    int splatMapID = entry.Value.splatMapID;
                    int channelID = entry.Value.channelID;
                    var terrainData = entry.Key.terrain.terrainData;
                    var splats = terrainData.alphamapTextures;
                    for (int j = 0; j < splats.Length; j++)
                    {
                        Texture2D sourceTex = splats[j];
                        if ((sourceTex.width != controlTextureResolution) || (sourceTex.height != controlTextureResolution))
                        {
                            Debug.LogWarning("Mismatched control texture resolution on one of the terrains. Expected:( " +
                            sourceTex.width + " x " + sourceTex.height + ") Found: (" + controlTextureResolution + " x " + controlTextureResolution + ")",
                            entry.Key.terrain);
                            continue;
                        }

                        Rect completeRect = new Rect(
                             entry.Value.affectedLocalPixels.x / (float)sourceTex.width,
                             entry.Value.affectedLocalPixels.y / (float)sourceTex.height,
                             entry.Value.affectedLocalPixels.width / (float)sourceTex.width,
                             entry.Value.affectedLocalPixels.height / (float)sourceTex.height);

                        mat.SetTexture("_MainTex", paintRenderTexture);
                        mat.SetTexture("_OldAlphaMapTexture", RTtextureSplatmap);
                        mat.SetTexture("_OriginalTargetAlphaMap", splats[splatMapID]);

                        mat.SetTexture("_AlphaMapTexture", sourceTex);
                        mat.SetVector("_LayerMask", j == splatMapID ? layerMasks[channelID] : Vector4.zero);
                        mat.SetVector("_OriginalTargetAlphaMask", layerMasks[channelID]);
                        mat.SetPass(1);

                        GL.PushMatrix();
                        GL.LoadPixelMatrix(0, paintTarget.width, 0, paintTarget.height);

                        GL.Begin(GL.QUADS);
                        GL.Color(new Color(1.0f, 1.0f, 1.0f, 1.0f));

                        GL.MultiTexCoord2(0, sourceRect.x, sourceRect.y);
                        GL.MultiTexCoord2(1, completeRect.x, completeRect.y);
                        GL.Vertex3(paintRect.x, paintRect.y, 0.0f);
                        GL.MultiTexCoord2(0, sourceRect.x, sourceRect.yMax);
                        GL.MultiTexCoord2(1, completeRect.x, completeRect.yMax);
                        GL.Vertex3(paintRect.x, paintRect.yMax, 0.0f);
                        GL.MultiTexCoord2(0, sourceRect.xMax, sourceRect.yMax);
                        GL.MultiTexCoord2(1, completeRect.xMax, completeRect.yMax);
                        GL.Vertex3(paintRect.xMax, paintRect.yMax, 0.0f);
                        GL.MultiTexCoord2(0, sourceRect.xMax, sourceRect.y);
                        GL.MultiTexCoord2(1, completeRect.xMax, completeRect.y);
                        GL.Vertex3(paintRect.xMax, paintRect.y, 0.0f);

                        GL.End();
                        GL.PopMatrix();
                        terrainData.CopyActiveRenderTextureToTexture(TerrainData.AlphamapTextureName, j, paintRect, entry.Value.affectedLocalPixels.min, true);

                        if (!affectedSplatmapData.Contains(terrainData))
                        {
                            affectedSplatmapData.Add(terrainData);
                        }
                    }

                    //completedEntries++;
               // }
            }

            //remove the entries from dictionary, as it will negatively affect other texture spawns
            foreach (var entry in affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Texture).ToList())
            {
                affectedTerrainPixels.Remove(entry.Key);
            }

 

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(paintRenderTexture);
            paintRenderTexture = null;

            RenderTexture.ReleaseTemporary(paintTarget);
            paintTarget = null;
            return cancel;
        }

        #endregion

        #region TERRAIN DETAILS

        /// <summary>
        /// Collects the affected terrains for a terrain detail operation. 
        /// </summary>
        public void CollectTerrainDetails()
        {
            RenderTexture previousRT = RenderTexture.active;
            int terrainDetailResolution = m_originTerrain.terrainData.detailResolution;



            m_terrainDetailPixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / (terrainDetailResolution - 1.0f),
            m_originTerrain.terrainData.size.z / (terrainDetailResolution - 1.0f));

            m_terrainDetailBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_terrainDetailPixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_terrainDetailBrushTransform.GetBrushXYBounds(), terrainDetailResolution, terrainDetailResolution, 0);

            CreateDefaultRenderTexture(ref RTdetailmap, m_terrainDetailPixels.width, m_terrainDetailPixels.height, RenderTextureFormat.R16);
            
            AddAffectedTerrainPixels(m_terrainDetailPixels, MultiTerrainOperationType.TerrainDetail, terrainDetailResolution, terrainDetailResolution);

        }

        /// <summary>
        /// Sets the terrain details across the operation area according to the information in the supplied Render Texture. 
        /// </summary>
        /// <param name="targetDetailTexture">A render texture containing the desired new detail distribution.</param>
        /// <param name="terrainLayerID">The terrain detail prototype ID.</param>
        /// <param name="spawnMode">The used spawn mode (replace, add, etc.)</param>
        /// <param name="centerTerrainOnly">Should the details be applied to the center terrain of the operation only?</param>
        /// <returns>True if canceled.</returns>
        public bool SetTerrainDetails(RenderTexture targetDetailTexture, SpawnerSettings spawnerSettings, Spawner spawner, SpawnRule spawnRule, int randomSeed, bool centerTerrainOnly = false)
        {
            XorshiftPlus randomGenerator = new XorshiftPlus(randomSeed);

            Color[] colors = GetRTColorArray(targetDetailTexture);
            int terrainDetailResolution = m_originTerrain.terrainData.detailResolution;

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.TerrainDetail && (x.Key.terrain == m_originTerrain || !centerTerrainOnly));

            //Calculate the actual target density according to the global spawn density
            float targetDensity = spawnRule.m_terrainDetailDensity * spawnerSettings.m_spawnDensity;

            //int completedEntries = 0;
            //int totalEntries = relevantEntries.Count();
            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                bool missingResourceHandled = false;

                //need to evaluate the terrain layer ID fresh for each terrain - can be a different ID on different terrains!
                int terrainLayerID = spawner.m_settings.m_resources.PrototypeIdxInTerrain(spawnRule.m_resourceType, spawnRule.m_resourceIdx, entry.Key.terrain);

                var terrainData = entry.Key.terrain.terrainData;
                if ((terrainData.detailResolution != terrainDetailResolution) || (terrainData.detailResolution != terrainDetailResolution))
                {
                    Debug.LogWarning("Mismatched terrain detail resolution on terrain: " + entry.Key.terrain.name);
                    continue;
                }

                int colorIndex = 0;
                //build up a int map
                int[,] map = new int[terrainData.detailResolution, terrainData.detailResolution];

                // this will be set to -1 if the spawn mode is set to remove to invert the spawn.
                int invert = 1;
                if (spawnerSettings.m_spawnMode == SpawnMode.Remove)
                {
                    invert = -1;
                }

                //If we don't replace the terrain detail data we have to read in the existing data first
                if (spawnerSettings.m_spawnMode != SpawnMode.Replace && terrainLayerID!=-1)
                {
                    map = terrainData.GetDetailLayer(0, 0, terrainData.detailResolution, terrainData.detailResolution, terrainLayerID);
                }
                else
                {
                    spawnRule.m_spawnedInstances = 0;
                }

                int maxX = entry.Value.affectedLocalPixels.x + entry.Value.affectedLocalPixels.width;
                int maxY = entry.Value.affectedLocalPixels.y + entry.Value.affectedLocalPixels.height;

                for (int x = entry.Value.affectedLocalPixels.x; x < maxX; x++)
                {
                    //if (x % 100 == 0)
                    //{
                    //    float progress = (float)completedEntries + Mathf.InverseLerp(0, maxX, x) / (float)totalEntries;
                    //    cancel = SpawnProgressBar.UpdateSpawnRuleProgress(progress);
                    //    if (cancel)
                    //    {
                    //        break;
                    //    }
                    //}

                    for (int y = entry.Value.affectedLocalPixels.y; y < maxY; y++)
                    {
                        //We have to calculate the correct color index on the original Paint context
                        //this must work for all cases, partial intersection  of the Paint Context with Local Context, LC fully encapsulated by PC, etc.
                        colorIndex = Mathf.Clamp(((y - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetDetailTexture.descriptor.width) + x - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x), 0, colors.Length - 1);

                        //At higher strength values we put a grass amount according to strength, at lower values, we start to "thin out" the grass
                        //by putting more and more 0s in our map array. Note that the map array assignment is inverted in the next line, else the result will be flipped!
                        int oldValue = map[y, x];
                        if (colors[colorIndex].r > spawnRule.m_terrainDetailMinFitness)
                        {
                            if (!missingResourceHandled)
                            {
                                if (HandleMissingResources(spawner, spawnRule, entry.Key.terrain))
                                {
                                    //get the new layer id since the terrain detail resource was just added
                                    terrainLayerID = spawner.m_settings.m_resources.PrototypeIdxInTerrain(spawnRule.m_resourceType, spawnRule.m_resourceIdx, entry.Key.terrain);
                                }
                                missingResourceHandled = true;
                            }

                            if (colors[colorIndex].r < spawnRule.m_terrainDetailFitnessBeginFadeOut)
                            {
                                if (randomGenerator.Next() > Mathf.InverseLerp(spawnRule.m_terrainDetailMinFitness, spawnRule.m_terrainDetailFitnessBeginFadeOut, colors[colorIndex].r))
                                {
                                    //only when replacing we must actually remove the grass, for add and remove mode it can just stay the same as it was
                                    if (spawnerSettings.m_spawnMode == SpawnMode.Replace)
                                    {
                                        map[y, x] = 0;
                                        spawnRule.m_spawnedInstances += map[y, x] - oldValue;
                                    }
                                    continue;
                                }
                            }
                            map[y, x] = Mathf.RoundToInt(Mathf.Clamp(map[y, x] + invert * Mathf.InverseLerp(spawnRule.m_terrainDetailMinFitness, 1f, colors[colorIndex].r) * targetDensity, 0, targetDensity));
                            spawnRule.m_spawnedInstances += map[y, x] - oldValue;
                        }
                    }
                    //if (cancel)
                    //{
                    //    break;
                    //}
                }
                if (terrainLayerID > -1)
                {
                    terrainData.SetDetailLayer(0, 0, terrainLayerID, map);
                }
                //completedEntries++;
                //if (cancel)
                //{
                //    break;
                //}
            }

            

            RenderTexture.ReleaseTemporary(targetDetailTexture);
            targetDetailTexture = null;

            //return cancel;
            return false;
        }


        /// <summary>
        /// Removes all terrain details in the masked area from the render texture which are not in the "domestic detail list". This is used to clear biomes from foreign terrain details that are not part of this biome (e.g. removing grass spawns from a snow biome) 
        /// </summary>
        /// <param name="targetDetailTexture">The render texture containing the desired tree distribution info.</param>
        ///<param name="domesticDetailPrototypes">The "allowed" domestic terrain details which should not be affected by this operation</param>
        ///<param name="removalStrength">The minimum required strength on the target Detail Texture required for a removal to occur.</param>
        public void RemoveForeignTerrainDetails(RenderTexture targetDetailTexture, List<ResourceProtoDetail> domesticDetailPrototypes, float removalStrength)
        {
            Color[] colors = GetRTColorArray(targetDetailTexture);
            int terrainDetailResolution = m_originTerrain.terrainData.detailResolution;

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.TerrainDetail);

            //int completedEntries = 0;
            //int totalEntries = relevantEntries.Count();
            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                TerrainData terrainData = entry.Key.terrain.terrainData;

                for (int protoIndex = 0; protoIndex < terrainData.detailPrototypes.Length; protoIndex++)
                {
                    DetailPrototype dp = terrainData.detailPrototypes[protoIndex];
                    if (!domesticDetailPrototypes.Exists(x=> x.m_detailTexture == dp.prototypeTexture && x.m_detailProtoype == dp.prototype))
                    {
                        int colorIndex = 0;
                        //build up a int map
                        int[,] map = new int[terrainData.detailResolution, terrainData.detailResolution];

                        map = terrainData.GetDetailLayer(0, 0, terrainData.detailResolution, terrainData.detailResolution, protoIndex);
                       
                        int maxX = entry.Value.affectedLocalPixels.x + entry.Value.affectedLocalPixels.width;
                        int maxY = entry.Value.affectedLocalPixels.y + entry.Value.affectedLocalPixels.height;

                        for (int x = entry.Value.affectedLocalPixels.x; x < maxX; x++)
                        {

                            for (int y = entry.Value.affectedLocalPixels.y; y < maxY; y++)
                            {
                                colorIndex = Mathf.Clamp(((y - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetDetailTexture.descriptor.width) + x - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x), 0, colors.Length - 1);

                                int oldValue = map[y, x];
                                if (colors[colorIndex].r > removalStrength)
                                {
                                    map[y, x] -= Mathf.RoundToInt(colors[colorIndex].r * 20 - removalStrength);
                                    map[y, x] = Math.Max(0, map[y, x]);
                                }
                            }
                        }
                        if (protoIndex > -1)
                        {
                            terrainData.SetDetailLayer(0, 0, protoIndex, map);
                        }
                    }
                }

            }
        }

        #endregion

        #region TERRAIN TREES

        /// <summary>
        /// Collects the affected terrains for a terrain tree operation. 
        /// </summary>
        public void CollectTerrainTrees()
        {
            RenderTexture previousRT = RenderTexture.active;
            int terrainTreeResolution = (int)m_originTerrain.terrainData.size.x;

            m_terrainTreePixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / (terrainTreeResolution - 1.0f),
            m_originTerrain.terrainData.size.z / (terrainTreeResolution - 1.0f));

            m_terrainTreeBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_terrainTreePixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_terrainTreeBrushTransform.GetBrushXYBounds(), terrainTreeResolution, terrainTreeResolution, 0);

            CreateDefaultRenderTexture(ref RTterrainTree, m_terrainTreePixels.width, m_terrainTreePixels.height, RenderTextureFormat.R16);

            AddAffectedTerrainPixels(m_terrainTreePixels, MultiTerrainOperationType.Tree, terrainTreeResolution, terrainTreeResolution);
        }


        /// <summary>
        /// Sets the terrain trees across the operation area according to the distribution information in the supplied Render Texture. 
        /// </summary>
        /// <param name="targetTreeTexture">The render texture containing the desired tree distribution info.</param>
        /// <param name="protoTypeIndex">The tree prototype index on the terrain.</param>
        /// <param name="protoTree">The Gaia tree prototype data.</param>
        /// <param name="spawnerSettings">The used spawnerSettings</param>
        /// <param name="centerTerrainOnly">Should the trees be applied to the center terrain of the operation only?</param>
        public bool SetTerrainTrees(RenderTexture targetTreeTexture, SpawnerSettings spawnerSettings, Spawner spawner, SpawnRule spawnRule, int seed, bool centerTerrainOnly = false)
        {
            //initialize RNG
            XorshiftPlus randomGenerator = new XorshiftPlus(seed);

            Color[] colors = GetRTColorArray(targetTreeTexture);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Tree && (x.Key.terrain == m_originTerrain || !centerTerrainOnly));

            //int completedEntries = 0;
            //int totalEntries = relevantEntries.Count();

            //Sanity check for location increments
            spawnRule.m_locationIncrementMin = Mathf.Max(GaiaConstants.minlocationIncrement, spawnRule.m_locationIncrementMin);
            spawnRule.m_locationIncrementMax = Mathf.Max(spawnRule.m_locationIncrementMin, spawnRule.m_locationIncrementMax);

            //Calculate the actual location increment, taking the global spawn density into account
            float locationIncrementMin = spawnRule.m_locationIncrementMin * (1 / spawnerSettings.m_spawnDensity);
            float locationIncrementMax = spawnRule.m_locationIncrementMax * (1 / spawnerSettings.m_spawnDensity);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                bool missingResourceHandled = false;

                var terrainData = entry.Key.terrain.terrainData;

                int colorIndex = 0;
                //build up a tree instance List
                List<TreeInstance> spawnedTreeInstances = new List<TreeInstance>();

                int protoTypeIndex = spawner.m_settings.m_resources.PrototypeIdxInTerrain(spawnRule.m_resourceType, spawnRule.m_resourceIdx, entry.Key.terrain);

                //Add the existing trees to our list
                spawnedTreeInstances.AddRange(terrainData.treeInstances);

                //int localResolution = Mathf.Max(terrainData.alphamapResolution, terrainData.heightmapResolution);
                RenderTextureDescriptor rtDesc = targetTreeTexture.descriptor;
                rtDesc.width = Mathf.RoundToInt(terrainData.size.x);
                rtDesc.height = Mathf.RoundToInt(terrainData.size.z);
                float jitterDistance = locationIncrementMin * spawnRule.m_jitterPercent;

                if (spawnerSettings.m_spawnMode == SpawnMode.Add || spawnerSettings.m_spawnMode == SpawnMode.Replace)
                {
                    //In Add or Replace spawn mode, we have to iterate over the terrain and add instances
                    float strength = 0f;

                    //Depending on where we are at with our affected local pixels, we need to start / stop on the relative position on the terrain
                    float startX = entry.Value.affectedLocalPixels.min.x * (m_originTerrain.terrainData.size.x / rtDesc.width);
                    float startY = entry.Value.affectedLocalPixels.min.y * (m_originTerrain.terrainData.size.z / rtDesc.height);
                    float stopX = entry.Value.affectedLocalPixels.max.x * (m_originTerrain.terrainData.size.x / rtDesc.width);
                    float stopY = entry.Value.affectedLocalPixels.max.y * (m_originTerrain.terrainData.size.z / rtDesc.height);

                    //Add a bit of extra seam where we try to spawn outside of the terrain, and then "jitter back on to it"
                    //this creates a more natural distribution on terrain borders
                    stopX += (locationIncrementMax);
                    stopY += (locationIncrementMax);

                    ResourceProtoTree protoTree = spawner.m_settings.m_resources.m_treePrototypes[spawnRule.m_resourceIdx];

                    for (float x = startX; x <= stopX; x += locationIncrementMin)
                    {
                        for (float y = startY; y <= stopY; y += locationIncrementMin)
                        {
                            //roll against failure rate to skip this spawn entirely
                            if (randomGenerator.Next() < spawnRule.m_failureRate)
                            {
                                continue;
                            }


                            //Jitter the position
                            float xPos = x + randomGenerator.Next(-jitterDistance, jitterDistance);
                            float yPos = y + randomGenerator.Next(-jitterDistance, jitterDistance);

                            int localX = Mathf.RoundToInt(xPos * rtDesc.width / terrainData.size.x);
                            int localY = Mathf.RoundToInt(yPos * rtDesc.height / terrainData.size.z);


                            if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                            {

                                colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetTreeTexture.descriptor.width + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;

                                //Random failure chance to feather out the trees with strength, the lower the strength is the higher the chance the spawn will fail, and vice versa
                                if (randomGenerator.Next(spawnRule.m_minRequiredFitness, 1f) > strength)
                                {
                                    continue;
                                }

                                if (!missingResourceHandled)
                                {
                                    if (HandleMissingResources(spawner, spawnRule, entry.Key.terrain))
                                    {
                                        //get the new layer id since the terrain detail resource was just added
                                        protoTypeIndex = spawner.m_settings.m_resources.PrototypeIdxInTerrain(spawnRule.m_resourceType, spawnRule.m_resourceIdx, entry.Key.terrain);
                                    }
                                    missingResourceHandled = true;
                                }

                                TreeInstance treeInstance = new TreeInstance();
                                treeInstance.prototypeIndex = protoTypeIndex;
                                treeInstance.position = new Vector3(xPos / (float)terrainData.size.x, 0, yPos / (float)terrainData.size.z);
                                //Determine width and height scale according to the prototype settings
                                switch (protoTree.m_spawnScale)
                                {
                                    case SpawnScale.Fixed:
                                        treeInstance.widthScale = protoTree.m_minWidth;
                                        treeInstance.heightScale = protoTree.m_minHeight;
                                        break;
                                    case SpawnScale.Random:
                                        float randomValue = randomGenerator.Next();
                                        treeInstance.widthScale = Mathf.Lerp(protoTree.m_minWidth, protoTree.m_maxWidth, randomValue);
                                        treeInstance.heightScale = Mathf.Lerp(protoTree.m_minHeight, protoTree.m_maxHeight, randomValue);
                                        break;
                                    case SpawnScale.Fitness:
                                        treeInstance.widthScale = Mathf.Lerp(protoTree.m_minWidth, protoTree.m_maxWidth, strength);
                                        treeInstance.heightScale = Mathf.Lerp(protoTree.m_minHeight, protoTree.m_maxHeight, strength);
                                        //Debug.Log("Width:" + treeInstance.widthScale.ToString());
                                        //Debug.Log("Height:" + treeInstance.heightScale.ToString());
                                        break;
                                    case SpawnScale.FitnessRandomized:
                                        treeInstance.widthScale = Mathf.Lerp(protoTree.m_minWidth, protoTree.m_maxWidth, strength);
                                        treeInstance.heightScale = Mathf.Lerp(protoTree.m_minHeight, protoTree.m_maxHeight, strength);
                                        float randomFitValue = randomGenerator.Next();
                                        treeInstance.widthScale *= Mathf.Lerp(1f - protoTree.m_widthRandomPercentage, 1f + protoTree.m_widthRandomPercentage, randomFitValue);
                                        treeInstance.heightScale *= Mathf.Lerp(1f - protoTree.m_heightRandomPercentage, 1f + protoTree.m_heightRandomPercentage, randomFitValue);
                                        break;
                                }

                                treeInstance.rotation = randomGenerator.Next(0, 360f);
                                treeInstance.color = Color.Lerp(protoTree.m_dryColour, protoTree.m_healthyColour, strength);
                                treeInstance.lightmapColor = Color.white;
                                spawnedTreeInstances.Add(treeInstance);
                                spawnRule.m_spawnedInstances++;
                            }
                        } // for y loop
                    } //for x loop
                }
                else
                {
                    //In Remove Spawn mode we iterate through the instances of our current prototype ID, and remove them from the array according to their fitness value on the terrain
                    for (int i = spawnedTreeInstances.Count - 1; i >= 0; i--)
                    {
                        TreeInstance treeInstance = spawnedTreeInstances[i];
                        if (treeInstance.prototypeIndex == protoTypeIndex)
                        {
                            float terrainXPos = treeInstance.position.x * terrainData.size.x;
                            float terrainYPos = treeInstance.position.z * terrainData.size.z;

                            int localX = Mathf.RoundToInt(terrainXPos * rtDesc.width / terrainData.size.x);
                            int localY = Mathf.RoundToInt(terrainYPos * rtDesc.height / terrainData.size.z);

                            if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                            {
                                colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetTreeTexture.descriptor.width + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                float strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;
                                if (strength>spawnRule.m_minRequiredFitness)
                                {
                                    spawnedTreeInstances.Remove(treeInstance);
                                    spawnRule.m_spawnedInstances--;
                                }
                            }
                        }
                    }
                }

                terrainData.SetTreeInstances(spawnedTreeInstances.ToArray(), true);
                //GaiaSessionManager.GetSessionManager().m_collisionMaskCache.BakeTerrainTreeCollisions(entry.Key.terrain, protoTypeIndex, 5f);
                //if (cancel)
                //{
                //    break;
                //}
            }
            RenderTexture.ReleaseTemporary(targetTreeTexture);
            targetTreeTexture = null;
            //return cancel;
            return false;
        }

        /// <summary>
        /// Removes all trees in the masked area from the render texture which are not in the "domestic tree list". This is used to clear biomes from foreign trees that are not part of this biome (e.g. removing cactus spawns from a snow biome) 
        /// </summary>
        /// <param name="targetTreeTexture">The render texture containing the desired tree distribution info.</param>
        ///<param name="domesticTreePrefabs">The "allowed" domestic trees which should not be affected by this operation</param>
        ///<param name="removalStrength">The minimum required strength on the target Tree Texture required for a removal to occur.</param>
        public bool RemoveForeignTrees(RenderTexture targetTreeTexture, List<GameObject> domesticTreePrefabs, float removalStrength)
        {
            Color[] colors = GetRTColorArray(targetTreeTexture);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Tree);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                var terrainData = entry.Key.terrain.terrainData;

                int colorIndex = 0;
                //build up a tree instance List
                List<TreeInstance> spawnedTreeInstances = new List<TreeInstance>();
                List<int> domesticTreeIndices = new List<int>();

                for (int i = 0; i < entry.Key.terrain.terrainData.treePrototypes.Length; i++)
                {
                    TreePrototype tp = entry.Key.terrain.terrainData.treePrototypes[i];
                    if (domesticTreePrefabs.Contains(tp.prefab))
                    {
                        domesticTreeIndices.Add(i);
                    }
                }


                //Add the existing trees to our list
                spawnedTreeInstances.AddRange(terrainData.treeInstances);

                //int localResolution = Mathf.Max(terrainData.alphamapResolution, terrainData.heightmapResolution);
                RenderTextureDescriptor rtDesc = targetTreeTexture.descriptor;
                //rtDesc.width = Mathf.RoundToInt(terrainData.size.x);
                //rtDesc.height = Mathf.RoundToInt(terrainData.size.z);
               
                //In Remove Spawn mode we iterate through the instances of our current prototype ID, and remove them from the array according to their fitness value on the terrain
                for (int i = spawnedTreeInstances.Count - 1; i >= 0; i--)
                {
                    TreeInstance treeInstance = spawnedTreeInstances[i];
                    if (!domesticTreeIndices.Contains(treeInstance.prototypeIndex))
                    {
                        float terrainXPos = treeInstance.position.x * terrainData.size.x;
                        float terrainYPos = treeInstance.position.z * terrainData.size.z;

                        int localX = Mathf.RoundToInt(terrainXPos * rtDesc.width / terrainData.size.x);
                        int localY = Mathf.RoundToInt(terrainYPos * rtDesc.height / terrainData.size.z);

                        if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                        {
                            colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetTreeTexture.descriptor.width + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                            float strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;
                            if (strength >= removalStrength)
                            {
                                spawnedTreeInstances.Remove(treeInstance);
                            }
                        }
                    }
                }
                terrainData.SetTreeInstances(spawnedTreeInstances.ToArray(), true);
            }
            RenderTexture.ReleaseTemporary(targetTreeTexture);
            targetTreeTexture = null;
            return false;
        }


        #endregion

        #region GAME OBJECTS
        /// <summary>
        /// Collects the affected terrains for a game object spawn. 
        /// </summary>
        public void CollectTerrainGameObjects()
        {
            RenderTexture previousRT = RenderTexture.active;
            int terrainGameObjectResolution = Math.Min((int)m_originTerrain.terrainData.size.x, 4096);

            m_gameObjectPixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / terrainGameObjectResolution,
            m_originTerrain.terrainData.size.z / terrainGameObjectResolution);

            m_gameObjectBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_gameObjectPixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_terrainDetailBrushTransform.GetBrushXYBounds(), terrainGameObjectResolution, terrainGameObjectResolution, 0);

            CreateDefaultRenderTexture(ref RTgameObject, m_gameObjectPixels.width, m_gameObjectPixels.height, RenderTextureFormat.R16);
            
            AddAffectedTerrainPixels(m_gameObjectPixels, MultiTerrainOperationType.GameObject, terrainGameObjectResolution, terrainGameObjectResolution);
        }

        /// <summary>
        /// Spawns game objects across the operation area according to the distribution information in the supplied Render Texture. 
        /// </summary>
        /// <param name="targetGameObjectTexture">The render texture containing the desired game object distribution info</param>
        /// <param name="protoGO">The Gaia tree game object prototype data</param>
        /// <param name="target">A target transform under which to create the new Game Object instances.</param>
        /// <param name="spawnMode">The used spawn mode (replace, add, etc.)</param>
        /// <param name="centerTerrainOnly">Should the Game Objects be applied to the center terrain of the operation only?</param>
        public bool SetTerrainGameObjects(RenderTexture targetGameObjectTexture, ResourceProtoGameObject protoGO, SpawnRule rule, SpawnerSettings spawnerSettings, int randomSeed, ref int instanceCounter, float removalStrength = 0f, bool centerTerrainOnly = false)
        {
            XorshiftPlus randomGenerator = new XorshiftPlus(randomSeed);

            //bool cancel = false;
            Color[] colors = GetRTColorArray(targetGameObjectTexture);
            
            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject && (x.Key.terrain == m_originTerrain || !centerTerrainOnly));

            //int completedEntries = 0;
            int totalEntries = relevantEntries.Count();

            //Sanity check for location increments
            rule.m_locationIncrementMin = Mathf.Max(GaiaConstants.minlocationIncrement, rule.m_locationIncrementMin);
          
            float jitterDistance = rule.m_locationIncrementMin * rule.m_jitterPercent;

            //Calculate the actual location increment, taking the global spawn density into account
            float locationIncrementMin = rule.m_locationIncrementMin * (1 / spawnerSettings.m_spawnDensity);


            //Store successful spawn locations in this list to perform self collision tests
            List<Vector2> pastSpawnLocations = new List<Vector2>();

            foreach (var entry in relevantEntries)
            {

                //Determine the parent (should only be required once per relevant entry)
                Transform target = GaiaUtils.GetGOSpawnTarget(rule, protoGO.m_name, entry.Key.terrain);

                //Track all spawned objects to parent them to the target object in the hierarchy in one go
                List<GameObject> spawnedObjects = new List<GameObject>();

                //the target can be down multiple levels below other objects that might be a floating point fix member. 
                //If that is the case, we need to make sure everything we spawn is not set up as static
                //otherwise it will not move correctly with the origin shift

                bool switchOffStatic = false;
#if GAIA_PRO_PRESENT
                if (target.GetComponentInParent<FloatingPointFixMember>() != null)
                {
                    switchOffStatic = true;
                }
#endif


                    if (!entry.Value.IsAffected())
                    continue;


                var terrainData = entry.Key.terrain.terrainData;
                int colorIndex = 0;


                //RenderTextureDescriptor rtDesc = targetGameObjectTexture.descriptor;
                //rtDesc.width = Mathf.RoundToInt(terrainData.size.x);
                //rtDesc.height = Mathf.RoundToInt(terrainData.size.z);
                int terrainGameObjectResolution = Math.Min((int)m_originTerrain.terrainData.size.x, 4096);

                if (spawnerSettings.m_spawnMode == SpawnMode.Add || spawnerSettings.m_spawnMode == SpawnMode.Replace)
                {
                    //Adding or replacing: We iterate over the terrain and add instances according to fitness
                    //(The removal for replace mode already took place in the spawner earlier)

                    float strength = 0f;

                    //Depening on where we are at with our affected local pixels, we need to start / stop on the relative position on the terrain
                    float startX = entry.Value.affectedLocalPixels.min.x * (m_originTerrain.terrainData.size.x / terrainGameObjectResolution);
                    float startY = entry.Value.affectedLocalPixels.min.y * (m_originTerrain.terrainData.size.z / terrainGameObjectResolution);
                    float stopX = entry.Value.affectedLocalPixels.max.x  * (m_originTerrain.terrainData.size.x / terrainGameObjectResolution);
                    float stopY = entry.Value.affectedLocalPixels.max.y * (m_originTerrain.terrainData.size.z / terrainGameObjectResolution);

                    //Add a bit of extra seam where we try to spawn outside of the terrain, and then "jitter back on to it"
                    //this creates a more natural distribution on terrain borders
                    stopX += (locationIncrementMin);
                    stopY += (locationIncrementMin);

                    if (spawnerSettings.m_spawnMode == SpawnMode.Replace)
                    {
                        instanceCounter = 0;
                    }

                    for (float x = startX; x <= stopX; x += locationIncrementMin)
                    {
                        for (float y = startY; y <= stopY; y += locationIncrementMin)
                        {
                            //roll against failure rate to skip this spawn entirely
                            if (randomGenerator.Next() < rule.m_failureRate)
                            {
                                continue;
                            }


                            //Jitter the position
                            float xPos = x + (randomGenerator.Next(-jitterDistance, jitterDistance) / 2f);
                            float yPos = y + (randomGenerator.Next(-jitterDistance, jitterDistance) / 2f);

                            int localX = Mathf.RoundToInt(xPos * terrainGameObjectResolution / terrainData.size.x);
                            int localY = Mathf.RoundToInt(yPos * terrainGameObjectResolution / terrainData.size.z);


                            if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                            {
                                colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetGameObjectTexture.descriptor.width + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;

                                //Random failure chance to feather out the game objects with strength, the lower the strength is the higher the chance the spawn will fail, and vice versa
                                if (randomGenerator.Next(rule.m_minRequiredFitness, 1f) > strength)
                                {
                                    continue;
                                }

                                float xWorldSpace = xPos + entry.Key.terrain.transform.position.x;
                                float zWorldSpace = yPos + entry.Key.terrain.transform.position.z;

                                //if self collision checks are active, check against the past spawn locations to see if the bounds radii would collide
                                if (rule.m_boundsCollisionCheck)
                                {
                                    bool collision = false;
                                    //Iterate through the list backwards so we can remove irrelevant entries while we iterate
                                    //Entries are irrelevant when the spawn position has moved so far on the X axis
                                    //so that a collision is not possible anymore.
                                    for (int i = pastSpawnLocations.Count() - 1; i >= 0; i--)
                                    {
                                        if (Vector2.Distance(new Vector2(xWorldSpace, zWorldSpace), pastSpawnLocations[i]) < (protoGO.m_dna.m_boundsRadius * 2))
                                        {
                                            collision = true;
                                        }
                                        else
                                        {
                                            //check if we are so far on the X-axis so that a collision is impossible in the future
                                            if (xWorldSpace > pastSpawnLocations[i].x + (protoGO.m_dna.m_boundsRadius * 2))
                                            {
                                                //we are, we can remove that past Spawn Location
                                                pastSpawnLocations.RemoveAt(i);
                                            }
                                        }
                                    }

                                    if (collision)
                                    {
                                        //we collided, abort the spawn and continue iterating
                                        continue;
                                    }
                                }    

                                float spawnRotationY = randomGenerator.Next(rule.m_minDirection, rule.m_maxDirection);

                                //For Gamebjects we don't want to take the strength directly as only spawn criteria - GOs spawn over a larger area, need to check the average strength of the area first, bx doing a sub-iteration.
                                int xBounds = Mathf.RoundToInt(protoGO.m_dna.m_boundsRadius * terrainGameObjectResolution / terrainData.size.x);
                                int yBounds = Mathf.RoundToInt(protoGO.m_dna.m_boundsRadius * terrainGameObjectResolution / terrainData.size.z);
                                int subXMin = localX - xBounds;
                                int subXMax = localX + xBounds;
                                int subYMin = localY - yBounds;
                                int subYMax = localY + yBounds;

                                float accumulatedStrength = 0f;
                                float numberOfChecks = 0;

                                int Xincrement = Math.Max(1, Mathf.CeilToInt((subXMax - subXMin) * (1-(rule.m_boundsCheckQuality / 100f))));
                                int Yincrement = Math.Max(1, Mathf.CeilToInt((subYMax - subYMin) * (1-(rule.m_boundsCheckQuality / 100f))));

                                int localMinusOperationY = entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y;
                                int localMinusOperationX = entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x;
                                for (int subX = subXMin; subX <= subXMax; subX += Xincrement)
                                {
                                    for (int subY = subYMin; subY <= subYMax; subY += Yincrement)
                                    {
                                        colorIndex = (subY - localMinusOperationY) * targetGameObjectTexture.descriptor.width + subX - localMinusOperationX;
                                        accumulatedStrength += (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;
                                        numberOfChecks++;
                                    }
                                }

                                //only actually spawn if the minimum strength is still being hit on average across the bounds area

                                float avg = accumulatedStrength / numberOfChecks;

                                if (avg<rule.m_minRequiredFitness)
                                {
                                    continue;
                                }

                                float scale = protoGO.m_dna.m_scaleMultiplier;
                                int spawnedInstances = 0;
                                float boundsRadius = protoGO.m_dna.m_boundsRadius * scale;
                                Vector3 scaleVect = new Vector3(scale, scale, scale);
                                float yWorldSpace = terrainData.GetInterpolatedHeight(xPos / (float)terrainData.size.x, yPos / (float)terrainData.size.z) + entry.Key.terrain.transform.position.y;
                                Vector3 worldSpacelocation = new Vector3(xWorldSpace, yWorldSpace, zWorldSpace);
                                pastSpawnLocations.Add(new Vector2(worldSpacelocation.x, worldSpacelocation.z));
                                ResourceProtoGameObjectInstance gpi;
                                for (int idx = 0; idx < protoGO.m_instances.Length; idx++)
                                {
                                    gpi = protoGO.m_instances[idx];

                                    if (gpi.m_desktopPrefab == null)
                                    {
                                        Debug.LogWarning("Spawn Rule " + rule.m_name + " is missing a prefab for a GameObject. Please check the resource settings in this rule and check if the instance at position " + (idx + 1).ToString() + " has a prefab maintained.");
                                        continue;
                                    }
                                    spawnedInstances = randomGenerator.Next(gpi.m_minInstances, gpi.m_maxInstances); //Randomly choose how many instances to spawn
                                    for (int inst = 0; inst < spawnedInstances; inst++) //For each instance
                                    {
                                        if (randomGenerator.Next() >= gpi.m_failureRate) //Handle failure override
                                        {
                                            Vector3 instanceLocation = worldSpacelocation;
                                            instanceLocation.x += (randomGenerator.Next(gpi.m_minSpawnOffsetX, gpi.m_maxSpawnOffsetX) * scale);
                                            instanceLocation.z += (randomGenerator.Next(gpi.m_minSpawnOffsetZ, gpi.m_maxSpawnOffsetZ) * scale);
                                            instanceLocation = Gaia.GaiaUtils.RotatePointAroundPivot(instanceLocation, worldSpacelocation, new Vector3(0f, spawnRotationY, 0f));

                                            //check if the strength is still valid in that spot for a spawn to happen
                                            //we don't want instances to be thrown out of a valid area by the offset / rotation
                                            int instanceLocalX = Mathf.RoundToInt((instanceLocation.x - entry.Key.terrain.transform.position.x) * terrainGameObjectResolution / terrainData.size.x);
                                            int instanceLocalY = Mathf.RoundToInt((instanceLocation.z - entry.Key.terrain.transform.position.z) * terrainGameObjectResolution / terrainData.size.z);

                                            colorIndex = (instanceLocalY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetGameObjectTexture.descriptor.width + instanceLocalX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                            float instanceStrength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;

                                            //check again per instance if this spot has a good strength
                                            if (rule.m_minInstanceRequiredFitness > instanceStrength)
                                            {
                                                continue;
                                            }

                                            float scalarX = (instanceLocation.x - entry.Key.terrain.transform.position.x) / (float)terrainData.size.x;
                                            float scalarZ = (instanceLocation.z - entry.Key.terrain.transform.position.z) / (float)terrainData.size.z;

                                            Vector3 interpolatedNormal = terrainData.GetInterpolatedNormal(scalarX, scalarZ);

                                            instanceLocation.y = terrainData.GetInterpolatedHeight(scalarX, scalarZ) + entry.Key.terrain.transform.position.y;

                                            //Determine the local scale according to the instance resource settings

                                            Vector3 localScale = new Vector3();
                                            switch (gpi.m_spawnScale)
                                            {
                                                case SpawnScale.Fixed:
                                                    if (gpi.m_commonScale)
                                                    {
                                                        localScale = new Vector3(gpi.m_minScale, gpi.m_minScale, gpi.m_minScale);
                                                    }
                                                    else
                                                    {
                                                        localScale = new Vector3(gpi.m_minXYZScale.x, gpi.m_minXYZScale.y, gpi.m_minXYZScale.z);
                                                    }
                                                    break;
                                                case SpawnScale.Random:
                                                    if (gpi.m_commonScale)
                                                    {
                                                        float randomValue = randomGenerator.Next(gpi.m_minScale, gpi.m_maxScale);
                                                        localScale = new Vector3(randomValue, randomValue, randomValue);
                                                    }
                                                    else
                                                    {
                                                        localScale = new Vector3(randomGenerator.Next(gpi.m_minXYZScale.x, gpi.m_maxXYZScale.x),
                                                                                randomGenerator.Next(gpi.m_minXYZScale.y, gpi.m_maxXYZScale.y),
                                                                                randomGenerator.Next(gpi.m_minXYZScale.z, gpi.m_maxXYZScale.z));
                                                    }
                                                    break;
                                                case SpawnScale.Fitness:
                                                    if (gpi.m_commonScale)
                                                    {
                                                        float fitnessValue = Mathf.Lerp(gpi.m_minScale, gpi.m_maxScale, instanceStrength);
                                                        localScale = new Vector3(fitnessValue, fitnessValue, fitnessValue);
                                                    }
                                                    else
                                                    {
                                                        localScale = new Vector3(Mathf.Lerp(gpi.m_minScale, gpi.m_maxScale, instanceStrength),
                                                                                Mathf.Lerp(gpi.m_minScale, gpi.m_maxScale, instanceStrength),
                                                                                Mathf.Lerp(gpi.m_minScale, gpi.m_maxScale, instanceStrength));
                                                    }
                                                    break;
                                                case SpawnScale.FitnessRandomized:
                                                    if (gpi.m_commonScale)
                                                    {
                                                        float fitnessValue = Mathf.Lerp(gpi.m_minScale, gpi.m_maxScale, instanceStrength);
                                                        localScale = new Vector3(fitnessValue, fitnessValue, fitnessValue);
                                                        localScale *= randomGenerator.Next(1f - gpi.m_scaleRandomPercentage, 1f + gpi.m_scaleRandomPercentage);
                                                    }
                                                    else
                                                    {
                                                        float xScale = Mathf.Lerp(gpi.m_minXYZScale.x, gpi.m_maxXYZScale.x, instanceStrength);
                                                        float yScale = Mathf.Lerp(gpi.m_minXYZScale.y, gpi.m_maxXYZScale.y, instanceStrength);
                                                        float zScale = Mathf.Lerp(gpi.m_minXYZScale.z, gpi.m_maxXYZScale.z, instanceStrength);
                                                        xScale *= randomGenerator.Next(1f - gpi.m_XYZScaleRandomPercentage.x, 1f + gpi.m_XYZScaleRandomPercentage.x);
                                                        yScale *= randomGenerator.Next(1f - gpi.m_XYZScaleRandomPercentage.y, 1f + gpi.m_XYZScaleRandomPercentage.y);
                                                        zScale *= randomGenerator.Next(1f - gpi.m_XYZScaleRandomPercentage.z, 1f + gpi.m_XYZScaleRandomPercentage.z);

                                                        localScale = new Vector3(xScale,yScale,zScale);
                                                    }
                                                    break; 
                                            }

                                            float localDist = Vector3.Distance(worldSpacelocation, instanceLocation);
                                            float maxDistance = Mathf.Max(Mathf.Max(Mathf.Abs(gpi.m_maxSpawnOffsetX), Mathf.Abs(gpi.m_minSpawnOffsetX), Mathf.Max(Mathf.Abs(gpi.m_maxSpawnOffsetZ), Mathf.Abs(gpi.m_minSpawnOffsetZ))));
                                            float distanceScale = gpi.m_scaleByDistance.Evaluate(localDist / Mathf.Max(1, maxDistance));

                                            if (gpi.m_yOffsetToSlope == true)
                                            {
                                                //when y-offsetting in the direction of the slope, we add the normal vector of the terrain multiplied with the y-offset
                                                instanceLocation += interpolatedNormal * randomGenerator.Next(gpi.m_minSpawnOffsetY, gpi.m_maxSpawnOffsetY) * localScale.y * scale * distanceScale;
                                            }
                                            else
                                            {
                                                //when offsetting "just upwards", we only add to the y coordinate
                                                instanceLocation.y += (randomGenerator.Next(gpi.m_minSpawnOffsetY, gpi.m_maxSpawnOffsetY) * localScale.y * scale * distanceScale);
                                            }

#if UNITY_EDITOR
                                            GameObject go = PrefabUtility.InstantiatePrefab(gpi.m_desktopPrefab) as GameObject;
                                            spawnedObjects.Add(go);
#else
                                            GameObject go = GameObject.Instantiate(gpi.m_desktopPrefab) as GameObject;
#endif
                                            go.name = "_Sp_" + go.name;
                                            go.transform.position = instanceLocation;

                                            go.transform.localScale = localScale * scale * distanceScale;
                                            go.transform.rotation = Quaternion.Euler(
                                                new Vector3(
                                                    randomGenerator.Next(gpi.m_minRotationOffsetX, gpi.m_maxRotationOffsetX),
                                                    randomGenerator.Next(gpi.m_minRotationOffsetY + spawnRotationY, gpi.m_maxRotationOffsetY + spawnRotationY),
                                                    randomGenerator.Next(gpi.m_minRotationOffsetZ, gpi.m_maxRotationOffsetZ)));

                                            if (protoGO.m_instances[idx].m_rotateToSlope == true)
                                            {
                                                go.transform.rotation = Quaternion.FromToRotation(Vector3.up, interpolatedNormal) * go.transform.rotation;
                                            }

                                            if (switchOffStatic)
                                            {
                                                go.isStatic = false;
                                            }

                                            instanceCounter++;
                                        }
                                    }
                                }
                            }

                        } // for y loop
                    } //for x loop
                }

                else //Spawn Mode check
                {
                    //Remove Mode: We iterate through the spawned instances below the target transform and remove them according to the fitness
#if UNITY_EDITOR
                    for (int i = target.childCount - 1; i >= 0; i--)
                    {
                        Transform childTransform = target.GetChild(i);
                        GameObject go = childTransform.gameObject;
                        if (go != null)
                        {
                            float terrainXPos = go.transform.position.x - entry.Key.terrain.transform.position.x;
                            float terrainYPos = go.transform.position.z - entry.Key.terrain.transform.position.z;

                            int localX = Mathf.RoundToInt(terrainXPos * terrainGameObjectResolution / terrainData.size.x);
                            int localY = Mathf.RoundToInt(terrainYPos * terrainGameObjectResolution / terrainData.size.z);

                            if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                            {
                                colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * terrainGameObjectResolution + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                float strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;
                                if (strength > removalStrength)
                                {
                                    GameObject.DestroyImmediate(go);
                                    instanceCounter--;
                                }
                            }
                        }
                    }
#endif
                }

                foreach (GameObject go in spawnedObjects)
                {
                    go.transform.parent = target;
                }
            }

            RenderTexture.ReleaseTemporary(targetGameObjectTexture);
            targetGameObjectTexture = null;
            //return cancel;
            return false;
        }

#endregion

#region SPAWN EXTENSIONS

        /// <summary>
        /// 
        /// </summary>
        /// <param name="renderTexture"></param>
        /// <param name="protoSpawnExtension"></param>
        /// <param name="spawnRule"></param>
        /// <param name="m_spawnedInstances"></param>
        /// <param name="m_minRequiredFitness"></param>
        /// <param name="v"></param>
        public bool SetSpawnExtensions(RenderTexture targetSpawnExtensionTexture, Spawner spawner, ResourceProtoSpawnExtension protoSpawnExtension, SpawnerSettings spawnerSettings, int ruleIndex, SpawnRule rule, int randomSeed, ref int instanceCounter, float m_minRequiredFitness, bool centerTerrainOnly)
        {
            XorshiftPlus randomGenerator = new XorshiftPlus(randomSeed);

            bool cancel = false;

            Color[] colors = GetRTColorArray(targetSpawnExtensionTexture);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject && (x.Key.terrain == m_originTerrain || !centerTerrainOnly));

            int completedEntries = 0;
            int totalEntries = relevantEntries.Count();

            //Sanity check for location increments
            rule.m_locationIncrementMin = Mathf.Max(GaiaConstants.minlocationIncrement, rule.m_locationIncrementMin);
    
            //Calculate the actual location increment, taking the global spawn density into account
            float locationIncrementMin = rule.m_locationIncrementMin * (1 / spawnerSettings.m_spawnDensity);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                //Determine the parent (should only be required once per relevant entry)
                Transform target = GaiaUtils.GetGOSpawnTarget(rule, protoSpawnExtension.m_name, entry.Key.terrain);

                var terrainData = entry.Key.terrain.terrainData;
                int colorIndex = 0;


                RenderTextureDescriptor rtDesc = targetSpawnExtensionTexture.descriptor;
                rtDesc.width = Mathf.RoundToInt(terrainData.size.x);
                rtDesc.height = Mathf.RoundToInt(terrainData.size.z);

                //Adding or replacing: We iterate over the terrain and add instances according to fitness
                //(The removal for replace mode already took place in the spawner earlier)

                //add a random starting offset between min max increment to avoid spawns becoming too similar
                float startX = 0;
                float startY = 0;

                float strength = 0;

                if (spawnerSettings.m_spawnMode == SpawnMode.Replace)
                {
                    instanceCounter = 0;
                }

                for (float x = startX; x < terrainData.size.x; x += locationIncrementMin)
                {
                   
                    float progress = (float)completedEntries + Mathf.InverseLerp(0, terrainData.size.x, x) / (float)totalEntries;
                    cancel = SpawnProgressBar.UpdateSpawnRuleProgress(progress);
                    if (cancel)
                    {
                        break;
                    }

                    for (float y = startY; y < terrainData.size.z; y += locationIncrementMin)
                    {
                        //roll against failure rate to skip this spawn entirely
                        if (randomGenerator.Next() < rule.m_failureRate)
                        {
                            continue;
                        }

                        //Add up the difference to max location increment, depending on the last strength value
                        //Jitter the position
                        float xPos = x + locationIncrementMin * (randomGenerator.Next(-rule.m_jitterPercent, rule.m_jitterPercent) / 2f);
                        float yPos = y + locationIncrementMin * (randomGenerator.Next(-rule.m_jitterPercent, rule.m_jitterPercent) / 2f);

                        int localX = Mathf.RoundToInt(xPos * rtDesc.width / terrainData.size.x);
                        int localY = Mathf.RoundToInt(yPos * rtDesc.height / terrainData.size.z);


                        if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                        {
                            colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetSpawnExtensionTexture.descriptor.width + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                            strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;

                            //Random failure chance to feather out the game objects with strength, the lower the strength is the higher the chance the spawn will fail, and vice versa
                            if (randomGenerator.Next(rule.m_minRequiredFitness, 1f) > strength)
                            {
                                continue;
                            }

                            float spawnRotationY = randomGenerator.Next(rule.m_minDirection, rule.m_maxDirection);

                            //For Gamebjects we don't want to take the strength directly as only spawn criteria - GOs spawn over a larger area, need to check the average strength of the area first, bx doing a sub-iteration.
                            float bounds = protoSpawnExtension.m_dna.m_boundsRadius;
                            float subXMin = xPos - bounds;
                            float subYMin = yPos - bounds;
                            float subXMax = xPos + bounds;
                            float subYMax = yPos + bounds;

                            float accumulatedStrength = 0f;
                            float numberOfChecks = 0;
                            float increment = (subXMax - subXMin) / rule.m_boundsCheckQuality;


                            for (float subX = subXMin; subX <= subXMax; subX += increment)
                            {
                                for (float subY = subYMin; subY <= subYMax; subY += increment)
                                {
                                    int subLocalX = Mathf.RoundToInt(subX * rtDesc.width / terrainData.size.x);
                                    int subLocalY = Mathf.RoundToInt(subY * rtDesc.height / terrainData.size.z);
                                    if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                                    {
                                        colorIndex = (subLocalY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetSpawnExtensionTexture.descriptor.width + subLocalX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                        accumulatedStrength += (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;
                                    }
                                    else
                                    {
                                        //We hit outside the spawn area, don't want to spawn in this case anyways
                                        accumulatedStrength = int.MinValue;
                                    }
                                    numberOfChecks++;
                                }
                            }

                            //only actually spawn if the minimum strength is still being hit on average across the bounds area

                            float avg = accumulatedStrength / numberOfChecks;

                            if (avg < rule.m_minRequiredFitness)
                            {
                                continue;
                            }
                            float scale = protoSpawnExtension.m_dna.m_scaleMultiplier;

                            int spawnedInstances = 0;
                            float boundsRadius = protoSpawnExtension.m_dna.m_boundsRadius * scale;
                            Vector3 scaleVect = new Vector3(scale, scale, scale);
                            float xWorldSpace = xPos + entry.Key.terrain.transform.position.x;
                            float zWorldSpace = yPos + entry.Key.terrain.transform.position.z;
                            float yWorldSpace = terrainData.GetInterpolatedHeight(xPos / (float)terrainData.size.x, yPos / (float)terrainData.size.z) + entry.Key.terrain.transform.position.y;
                            Vector3 worldSpacelocation = new Vector3(xWorldSpace, yWorldSpace, zWorldSpace);

                            ResourceProtoSpawnExtensionInstance instance;

                            for (int idx = 0; idx < protoSpawnExtension.m_instances.Length; idx++)
                            {
                                instance = protoSpawnExtension.m_instances[idx];

                                if (instance.m_spawnerPrefab == null)
                                {
                                    Debug.LogWarning("Spawn Rule " + rule.m_name + " is missing a prefab for a GameObject. Please check the resource settings in this rule and check if the instance at position " + (idx + 1).ToString() + " has a prefab maintained.");
                                    continue;
                                }

                                spawnedInstances = randomGenerator.Next(instance.m_minSpawnerRuns, instance.m_maxSpawnerRuns); //Randomly choose how many instances to spawn
                                for (int inst = 0; inst < spawnedInstances; inst++) //For each instance
                                {
                                    if (randomGenerator.Next() >= instance.m_failureRate) //Handle failure override
                                    {
                                        Vector3 instanceLocation = worldSpacelocation;
                                        instanceLocation.x += (randomGenerator.Next(instance.m_minSpawnOffsetX, instance.m_maxSpawnOffsetX) * scale);
                                        instanceLocation.z += (randomGenerator.Next(instance.m_minSpawnOffsetZ, instance.m_maxSpawnOffsetZ) * scale);
                                        instanceLocation = Gaia.GaiaUtils.RotatePointAroundPivot(instanceLocation, worldSpacelocation, new Vector3(0f, spawnRotationY, 0f));

                                        float scalarX = (instanceLocation.x - entry.Key.terrain.transform.position.x) / (float)terrainData.size.x;
                                        float scalarZ = (instanceLocation.z - entry.Key.terrain.transform.position.z) / (float)terrainData.size.z;

                                        instanceLocation.y = terrainData.GetInterpolatedHeight(scalarX, scalarZ) + entry.Key.terrain.transform.position.y;
                                           
                                        //Determine the local scale according to the instance resource settings
                                        int instanceLocalX = Mathf.RoundToInt((instanceLocation.x - entry.Key.terrain.transform.position.x) * rtDesc.width / terrainData.size.x);
                                        int instanceLocalY = Mathf.RoundToInt((instanceLocation.z - entry.Key.terrain.transform.position.z) * rtDesc.height / terrainData.size.z);

                                        colorIndex = (instanceLocalY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetSpawnExtensionTexture.descriptor.width + instanceLocalX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                        float instanceFitness = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;



                                        Vector3 localScale = new Vector3();
                                        switch (instance.m_spawnScale)
                                        {
                                            case SpawnScale.Fixed:
                                                if (instance.m_commonScale)
                                                {
                                                    localScale = new Vector3(instance.m_minScale, instance.m_minScale, instance.m_minScale);
                                                }
                                                else
                                                {
                                                    localScale = new Vector3(instance.m_minXYZScale.x, instance.m_minXYZScale.y, instance.m_minXYZScale.z);
                                                }
                                                break;
                                            case SpawnScale.Random:
                                                if (instance.m_commonScale)
                                                {
                                                    float randomValue = randomGenerator.Next(instance.m_minScale, instance.m_maxScale);
                                                    localScale = new Vector3(randomValue, randomValue, randomValue);
                                                }
                                                else
                                                {
                                                    localScale = new Vector3(randomGenerator.Next(instance.m_minXYZScale.x, instance.m_maxXYZScale.x),
                                                                            randomGenerator.Next(instance.m_minXYZScale.y, instance.m_maxXYZScale.y),
                                                                            randomGenerator.Next(instance.m_minXYZScale.z, instance.m_maxXYZScale.z));
                                                }
                                                break;
                                            case SpawnScale.Fitness:
                                                if (instance.m_commonScale)
                                                {
                                                    float fitnessValue = Mathf.Lerp(instance.m_minScale, instance.m_maxScale, instanceFitness);
                                                    localScale = new Vector3(fitnessValue, fitnessValue, fitnessValue);
                                                }
                                                else
                                                {
                                                    localScale = new Vector3(Mathf.Lerp(instance.m_minScale, instance.m_maxScale, instanceFitness),
                                                                            Mathf.Lerp(instance.m_minScale, instance.m_maxScale, instanceFitness),
                                                                            Mathf.Lerp(instance.m_minScale, instance.m_maxScale, instanceFitness));
                                                }
                                                break;
                                            case SpawnScale.FitnessRandomized:
                                                if (instance.m_commonScale)
                                                {
                                                    float fitnessValue = Mathf.Lerp(instance.m_minScale, instance.m_maxScale, instanceFitness);
                                                    localScale = new Vector3(fitnessValue, fitnessValue, fitnessValue);
                                                    localScale *= randomGenerator.Next(1f - instance.m_scaleRandomPercentage, 1f + instance.m_scaleRandomPercentage);
                                                }
                                                else
                                                {
                                                    float xScale = Mathf.Lerp(instance.m_minXYZScale.x, instance.m_maxXYZScale.x, instanceFitness);
                                                    float yScale = Mathf.Lerp(instance.m_minXYZScale.y, instance.m_maxXYZScale.y, instanceFitness);
                                                    float zScale = Mathf.Lerp(instance.m_minXYZScale.z, instance.m_maxXYZScale.z, instanceFitness);
                                                    xScale *= randomGenerator.Next(1f - instance.m_XYZScaleRandomPercentage.x, 1f + instance.m_XYZScaleRandomPercentage.x);
                                                    yScale *= randomGenerator.Next(1f - instance.m_XYZScaleRandomPercentage.y, 1f + instance.m_XYZScaleRandomPercentage.y);
                                                    zScale *= randomGenerator.Next(1f - instance.m_XYZScaleRandomPercentage.z, 1f + instance.m_XYZScaleRandomPercentage.z);

                                                    localScale = new Vector3(xScale, yScale, zScale);
                                                }
                                                break;
                                        }

                                        float localDist = Vector3.Distance(worldSpacelocation, instanceLocation);
                                        float distanceScale = instance.m_scaleByDistance.Evaluate(localDist / boundsRadius);

                                        SpawnExtensionInfo info = new SpawnExtensionInfo();
                                        info.m_position = instanceLocation;
                                        info.m_rotation = Quaternion.Euler(
                                            new Vector3(
                                                randomGenerator.Next(instance.m_minRotationOffsetX, instance.m_maxRotationOffsetX),
                                                randomGenerator.Next(instance.m_minRotationOffsetY + spawnRotationY, instance.m_maxRotationOffsetY + spawnRotationY),
                                                randomGenerator.Next(instance.m_minRotationOffsetZ, instance.m_maxRotationOffsetZ)));
                                        info.m_scale = localScale * scale * distanceScale;
                                        info.m_currentTerrain = entry.Key.terrain;

                                        //Get all Spawn extensions and fire (could be multiple Spawn Extension components on the prefab)

                                        foreach (ISpawnExtension extension in instance.m_spawnerPrefab.GetComponents<ISpawnExtension>())
                                        {
                                            extension.Spawn(spawner, target, ruleIndex, ruleIndex, info);
                                            instanceCounter++;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }

                if (cancel)
                {
                    break;
                }
            }

            return cancel;

        }

        public bool SetWorldMapStamps (RenderTexture worldMapStampTexture, Spawner spawner, ResourceProtoStampDistribution protoStampDistribution, SpawnMode spawnMode, int ruleIndex, SpawnRule rule, WorldCreationSettings worldCreationSettings, int randomSeed, ref int instanceCounter)
        {
            bool cancel = false;
#if UNITY_EDITOR
            XorshiftPlus randomGenerator = new XorshiftPlus(randomSeed);
            

            GaiaSession session = GaiaSessionManager.GetSessionManager().m_session;

            //Get the max stamper size to not overstep the max internal resolution value of 11574 of the stamper preview
            float range = Mathf.RoundToInt(Mathf.Clamp(worldCreationSettings.m_tileSize, 1, (float)11574 / (float)worldCreationSettings.m_gaiaDefaults.m_heightmapResolution * 100));
            float widthfactor = worldCreationSettings.m_tileSize / 100f;
            float maxStamperSize = range * widthfactor;

            Color[] colors = GetRTColorArray(worldMapStampTexture);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject && (x.Key.terrain == m_originTerrain) && TerrainHelper.IsWorldMapTerrain(x.Key.terrain));

           

            //int completedEntries = 0;
            int totalEntries = relevantEntries.Count();

            //Sanity check for location increments
            rule.m_locationIncrementMin = Mathf.Max(GaiaConstants.minlocationIncrement, rule.m_locationIncrementMin);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                //Determine the parent (the world map stamp tokens are parented below the world map so they move with it automatically)
                Transform target = entry.Key.terrain.transform.Find(GaiaConstants.worldMapStampTokenSpawnTarget);
                if (target == null)
                {
                    GameObject container = new GameObject(GaiaConstants.worldMapStampTokenSpawnTarget);
                    container.transform.parent = entry.Key.terrain.transform;
                    target = container.transform;
                }

                var terrainData = entry.Key.terrain.terrainData;
                int colorIndex = 0;


                RenderTextureDescriptor rtDesc = worldMapStampTexture.descriptor;
                //rtDesc.width = Mathf.RoundToInt(terrainData.size.x);
                //rtDesc.height = Mathf.RoundToInt(terrainData.size.z);

                float strength = 0f;

                //add a random starting offset between min max increment to avoid spawns becoming too similar
                float startX = 0;
                float startY = 0;

                //location increment is relative to the terrain size of our output terrain - for a larger output terrain we need to spawn more stamps for a good result
                float locationIncrementMin = (terrainData.size.x / 100 * rule.m_locationIncrementMin) / (1f + ((worldCreationSettings.m_xTiles - 1f) / 50f));

                if (spawnMode == SpawnMode.Replace)
                {
                    instanceCounter = 0;
                }

                for (float x = startX; x < terrainData.size.x; x += locationIncrementMin)
                {
                    for (float y = startY; y < terrainData.size.z; y += locationIncrementMin)
                    {
                        //Jitter the position
                        float xPos = x + locationIncrementMin * randomGenerator.Next(-rule.m_jitterPercent, rule.m_jitterPercent * 2f);
                        float yPos = y + locationIncrementMin * randomGenerator.Next(-rule.m_jitterPercent, rule.m_jitterPercent * 2f);

                        //Debug.Log("X:" + xPos.ToString() + " Y:" + yPos.ToString());

                        int localX = Mathf.RoundToInt(xPos * rtDesc.width / terrainData.size.x);
                        int localY = Mathf.RoundToInt(yPos * rtDesc.height / terrainData.size.z);


                        if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                        {
                            colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * worldMapStampTexture.descriptor.width + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                            strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;
                            //Random failure chance to feather out the game objects with strength, the lower the strength is the higher the chance the spawn will fail, and vice versa
                            if (randomGenerator.Next(rule.m_minRequiredFitness, 1f) > strength)
                            {
                                continue;
                            }

                            float spawnRotationY = randomGenerator.Next(rule.m_minDirection, rule.m_maxDirection);
                            float xWorldSpace = xPos + entry.Key.terrain.transform.position.x;
                            float zWorldSpace = yPos + entry.Key.terrain.transform.position.z;
                            float yWorldSpace = terrainData.GetInterpolatedHeight(xPos / (float)terrainData.size.x, yPos / (float)terrainData.size.z) + entry.Key.terrain.transform.position.y;


                            //Get a random texture for the selected feature type
                            float chanceSum = protoStampDistribution.m_featureSettings.Sum(a => a.m_chanceStrengthMapping.Evaluate(strength));
                            float stackedChanceAggregate = 0;
                            foreach (StampFeatureSettings stampFeatureSetting in protoStampDistribution.m_featureSettings)
                            {
                                stampFeatureSetting.m_stackedChance = Mathf.InverseLerp(0, chanceSum, stackedChanceAggregate);
                                stackedChanceAggregate += stampFeatureSetting.m_chanceStrengthMapping.Evaluate(strength);
                            }
                            protoStampDistribution.m_featureSettings.Sort((a, b) => a.m_stackedChance.CompareTo(b.m_stackedChance));
                            float randomValue = randomGenerator.Next();
                            //since our list is sorted by chance raising, we can check which list entry has a chance lower than the rolled random value
                            StampFeatureSettings selectedFeatureSetting = protoStampDistribution.m_featureSettings[0];
                            for (int i = 0; i < protoStampDistribution.m_featureSettings.Count; i++)
                            {
                                //Last entry in the list? If we came this far, this entry wins automatically
                                if (i == protoStampDistribution.m_featureSettings.Count - 1)
                                {
                                    selectedFeatureSetting = protoStampDistribution.m_featureSettings[i];
                                    break;
                                }
                                else
                                {
                                    if (protoStampDistribution.m_featureSettings[i].m_stackedChance < randomValue && randomValue < protoStampDistribution.m_featureSettings[i + 1].m_stackedChance)
                                    {
                                        selectedFeatureSetting = protoStampDistribution.m_featureSettings[i];
                                        break;
                                    }
                                }
                            }

                            //Get a random texture from that directory
                            string directory = GaiaDirectories.GetStampDirectory() + Path.DirectorySeparatorChar + selectedFeatureSetting.m_featureType;
                            var info = new DirectoryInfo(directory);

                            if (info == null)
                            {
                                Debug.LogWarning("Could not access directory " + directory + " when trying to pick a stamp for random terrain generation!");
                                continue;
                            }
                            FileInfo[] allFiles = info.GetFiles();
                            FileInfo[] allTextures = allFiles.Where(a => a.Extension != ".meta").ToArray();
                            string path = allTextures[randomGenerator.Next(0, allTextures.Length - 1)].FullName;
                            path = path.Remove(0, Application.dataPath.Length - "Assets".Length);

                            Texture2D chosenTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

                            StamperSettings stamperSettings = ScriptableObject.CreateInstance<StamperSettings>();
                            stamperSettings.m_operation = (GaiaConstants.FeatureOperation)selectedFeatureSetting.m_operation;

                            if (stamperSettings.m_operation == GaiaConstants.FeatureOperation.RaiseHeight || stamperSettings.m_operation == GaiaConstants.FeatureOperation.AddHeight || stamperSettings.m_operation == GaiaConstants.FeatureOperation.MixHeight)
                            {
                                //Move the base level to 0 to make sure the stamp is not hidden right from the start
                                stamperSettings.m_baseLevel = 0f;
                            }
                            if (stamperSettings.m_operation == GaiaConstants.FeatureOperation.LowerHeight || stamperSettings.m_operation == GaiaConstants.FeatureOperation.SubtractHeight)
                            {
                                stamperSettings.m_baseLevel = 1f;
                            }

                            float widthValue = 0.5f;
                            if (selectedFeatureSetting.m_tieWidthToStrength)
                            {
                                widthValue = selectedFeatureSetting.m_chanceStrengthMapping.Evaluate(strength);
                            }
                            else
                            {
                                widthValue = randomGenerator.Next();
                            }
                            //Mulitply the desired stamper size with the number of tiles so that the stamp scales accordingly for different sized worlds
                            //but also clamp it to the stamper size limits 
                            float unscaledWidth = Mathf.Lerp(selectedFeatureSetting.m_minWidth, selectedFeatureSetting.m_maxWidth, widthValue);
                            stamperSettings.m_width = Mathf.Clamp((worldCreationSettings.m_xTiles * unscaledWidth), 0, maxStamperSize);
                            float heightValue = 0.5f;
                                
                            if (selectedFeatureSetting.m_tieHeightToStrength)
                            {
                                heightValue = selectedFeatureSetting.m_chanceStrengthMapping.Evaluate(strength);
                            }
                            else
                            {
                                heightValue = randomGenerator.Next();
                            }
                            if (stamperSettings.m_operation != FeatureOperation.MixHeight)
                            {
                                stamperSettings.m_height = Mathf.Lerp(selectedFeatureSetting.m_minHeight, selectedFeatureSetting.m_maxHeight, heightValue);
                            }
                            else
                            {
                                stamperSettings.m_mixHeightStrength = Mathf.Lerp(selectedFeatureSetting.m_minMixStrength, selectedFeatureSetting.m_maxMixStrength, heightValue);
                                stamperSettings.m_mixMidPoint = Mathf.Lerp(selectedFeatureSetting.m_minMixMidPoint, selectedFeatureSetting.m_maxMixMidPoint, randomGenerator.Next());
                            }
                            stamperSettings.m_rotation = randomGenerator.Next(0f, 360f);

                            stamperSettings.m_stamperInputImageMask.m_operation = ImageMaskOperation.ImageMask;
                            stamperSettings.m_stamperInputImageMask.ImageMaskTexture = chosenTexture;
                            stamperSettings.m_stamperInputImageMask.m_influence = selectedFeatureSetting.m_stampInfluence;

                            ImageMask[] imageMasks = null;
                            switch (selectedFeatureSetting.m_borderMaskStyle)
                            {
                                case BorderMaskStyle.ImageMask:

                                    //Get a random border mask texture from the mask directory
                                    string borderMaskDirectory = GaiaDirectories.GetStampDirectory() + Path.DirectorySeparatorChar + selectedFeatureSetting.m_borderMaskType;
                                    info = new DirectoryInfo(borderMaskDirectory);

                                    if (info == null)
                                    {
                                        Debug.LogWarning("Could not access directory " + borderMaskDirectory + " when trying to pick a border mask for random terrain generation!");
                                        continue;
                                    }
                                    allFiles = info.GetFiles();
                                    allTextures = allFiles.Where(a => a.Extension != ".meta").ToArray();
                                    path = allTextures[randomGenerator.Next(0, allTextures.Length - 1)].FullName;
                                    path = path.Remove(0, Application.dataPath.Length - "Assets".Length);

                                    Texture2D chosenMaskTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                                    imageMasks = new ImageMask[1] {
                                    new ImageMask() { m_operation = ImageMaskOperation.ImageMask, ImageMaskTexture = chosenMaskTexture, m_influence = ImageMaskInfluence.Local }
                                    };
                                    break;
                                case BorderMaskStyle.DistanceMask:
                                    imageMasks = new ImageMask[1] {
                                    new ImageMask() { m_operation = ImageMaskOperation.DistanceMask, m_influence = ImageMaskInfluence.Local }
                                    };
                                    break;
                                case BorderMaskStyle.None:
                                    //do nothing and leave the secondary masks null 
                                    break;
                            }


                            stamperSettings.m_imageMasks = imageMasks;

                            //Roll for inversion of the stamp
                            if (randomGenerator.Next(0f, 100f) <= selectedFeatureSetting.m_invertChance)
                            {
                                GaiaUtils.InvertAnimationCurve(ref stamperSettings.m_imageMasks[0].m_strengthTransformCurve);
                            }

                            string tokenName = stamperSettings.m_operation.ToString() + " " + chosenTexture.name;
                            GameObject stamperTokenGO = new GameObject(tokenName);
                            WorldMapStampToken wmst = stamperTokenGO.AddComponent<WorldMapStampToken>();

                            wmst.m_featureType = selectedFeatureSetting.m_featureType;
                            wmst.m_gizmoColor = rule.m_visualisationColor;
                            wmst.m_connectedStamperSettings = stamperSettings;
                            //Y-Offset - needs to be relative to the height at this point so it scales nicely over to different terrain heights.
                            yWorldSpace += Mathf.Lerp(yWorldSpace * (selectedFeatureSetting.m_minYOffset / 100f), yWorldSpace * (selectedFeatureSetting.m_maxYOffset / 100f), randomGenerator.Next());
                            Vector3 worldSpacelocation = new Vector3(xWorldSpace, yWorldSpace, zWorldSpace);
                            stamperTokenGO.transform.position = worldSpacelocation;
                            stamperTokenGO.transform.parent = target;
                            wmst.SyncLocationToStamperSettings();
                            wmst.UpdateGizmoPos();

                            stamperSettings.m_stamperInputImageMask.FreeTextureReferences();
                            if (stamperSettings.m_imageMasks != null)
                            {
                                foreach (ImageMask im in stamperSettings.m_imageMasks)
                                {
                                    im.FreeTextureReferences();
                                }
                            }

                            instanceCounter++;
                        }
                    } // for y loop
                }// for x loop

                if (cancel)
                {
                    break;
                }
            }
            //Get Rid off all stamp texture references that have built up during spawning
            Resources.UnloadUnusedAssets();
#endif
            return cancel;

        }


        public void SetProbes(RenderTexture targetProbeTexture, Spawner spawner, ResourceProtoProbe protoProbe, SpawnMode spawnMode, int index, SpawnRule spawnRule, int randomSeed, ref int instanceCounter, bool seaLeveLActive, float seaLevel)
        {
#if UNITY_EDITOR
            XorshiftPlus randomGenerator = new XorshiftPlus(randomSeed);

            //bool cancel = false;
            Color[] colors = GetRTColorArray(targetProbeTexture);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject);

            //int completedEntries = 0;
            int totalEntries = relevantEntries.Count();

            //Sanity check for location increments
            spawnRule.m_locationIncrementMin = Mathf.Max(GaiaConstants.minlocationIncrement, spawnRule.m_locationIncrementMin);
            spawnRule.m_locationIncrementMax = Mathf.Max(spawnRule.m_locationIncrementMin, spawnRule.m_locationIncrementMax);

            //Calculate probe size
            float avglocationIncrement = (spawnRule.m_locationIncrementMax + spawnRule.m_locationIncrementMin) / 2f;
            Vector3 probeSize = new Vector3(avglocationIncrement, m_originTerrain.terrainData.size.y, avglocationIncrement);




            foreach (var entry in relevantEntries)
            {

                if (!entry.Value.IsAffected())
                    continue;

                //Determine the parent (should only be required once per relevant entry)
                Transform target = GaiaUtils.GetGOSpawnTarget(spawnRule, protoProbe.m_name, entry.Key.terrain);


                //the target can be down multiple levels below other objects that might be a floating point fix member. 
                //If that is the case, we need to make sure everything we spawn is not set up as static
                //otherwise it will not move correctly with the origin shift

                bool switchOffStatic = false;
#if GAIA_PRO_PRESENT
                if (target.GetComponentInParent<FloatingPointFixMember>() != null)
                {
                    switchOffStatic = true;
                }
#endif


                //Get Light Probe Group object & storage if we are spawning light probes
                Transform lightProbeTransform = target.Find("Light Probes Group Data");
                GameObject lightProbeObject = null;
                LightProbeGroup lightProbeGroup = null;
                List <Vector3> probePositions = new List<Vector3>();
                if (protoProbe.m_probeType == ProbeType.LightProbe)
                {
                    if (lightProbeTransform == null)
                    {
                        lightProbeObject = new GameObject("Light Probes Group Data");
                        lightProbeObject.transform.parent = target;
                        lightProbeTransform = lightProbeObject.transform;
                        if (switchOffStatic)
                        {
                            lightProbeObject.isStatic = false;
                        }

                    }

                    lightProbeGroup = lightProbeObject.GetComponent<LightProbeGroup>();
                    if (lightProbeGroup == null)
                    {
                        lightProbeGroup = lightProbeObject.AddComponent<LightProbeGroup>();
                        lightProbeGroup.probePositions = new Vector3[0];
                    }
                }


                var terrainData = entry.Key.terrain.terrainData;
                int colorIndex = 0;
                int terrainGameObjectResolution = Math.Min((int)m_originTerrain.terrainData.size.x, 4096);

                if (spawnMode == SpawnMode.Add || spawnMode == SpawnMode.Replace)
                {
                    //Adding or replacing: We iterate over the terrain and add instances according to fitness
                    //(The removal for replace mode already took place in the spawner earlier)

                    float strength = 0f;

                    //Depening on where we are at with our affected local pixels, we need to start / stop on the relative position on the terrain
                    float startX = entry.Value.affectedLocalPixels.min.x * (m_originTerrain.terrainData.size.x / terrainGameObjectResolution);
                    float startY = entry.Value.affectedLocalPixels.min.y * (m_originTerrain.terrainData.size.z / terrainGameObjectResolution);
                    float stopX = entry.Value.affectedLocalPixels.max.x * (m_originTerrain.terrainData.size.x / terrainGameObjectResolution);
                    float stopY = entry.Value.affectedLocalPixels.max.y * (m_originTerrain.terrainData.size.z / terrainGameObjectResolution);

                    if (spawnMode == SpawnMode.Replace)
                    {
                        instanceCounter = 0;
                    }

                    for (float x = startX; x <= stopX; x += spawnRule.m_locationIncrementMin)
                    {

                        for (float y = startY; y <= stopY; y += spawnRule.m_locationIncrementMin)
                        {
                            //Jitter the position
                            float xPos = x + spawnRule.m_locationIncrementMin * (randomGenerator.Next(-spawnRule.m_jitterPercent, spawnRule.m_jitterPercent) / 2f);
                            float yPos = y + spawnRule.m_locationIncrementMin * (randomGenerator.Next(-spawnRule.m_jitterPercent, spawnRule.m_jitterPercent) / 2f);

                            //Debug.Log("X:" + xPos.ToString() + " Y:" + yPos.ToString());

                            int localX = Mathf.RoundToInt(xPos * terrainGameObjectResolution / terrainData.size.x);
                            int localY = Mathf.RoundToInt(yPos * terrainGameObjectResolution / terrainData.size.z);


                            if (entry.Value.affectedLocalPixels.Contains(new Vector2Int(localX, localY)))
                            {
                                colorIndex = (localY - (entry.Value.affectedLocalPixels.y - entry.Value.affectedOperationPixels.y)) * targetProbeTexture.descriptor.width + localX - (entry.Value.affectedLocalPixels.x - entry.Value.affectedOperationPixels.x);
                                strength = (colorIndex >= 0 && colorIndex < colors.Length - 1) ? colors[colorIndex].r : 0;
                                //Random failure chance to feather out the game objects with strength, the lower the strength is the higher the chance the spawn will fail, and vice versa
                                if (randomGenerator.Next(spawnRule.m_minRequiredFitness, 1f) > strength)
                                {
                                    continue;
                                }
                                Vector3 newPosition = new Vector3(xPos + entry.Key.terrain.transform.position.x, 0, yPos + entry.Key.terrain.transform.position.z);
                                if (protoProbe.m_probeType == ProbeType.ReflectionProbe)
                                {
#if GAIA_PRO_PRESENT
                                    ReflectionProbeManager rpManager = ReflectionProbeManager.Instance;
                                    if (rpManager != null)
                                    {
                                        ReflectionProbe rp = ReflectionProbeUtils.CreateReflectionProbe(newPosition, probeSize, entry.Key.terrain, protoProbe.m_reflectionProbeData, seaLeveLActive, seaLevel, target, false, rpManager.UseReflectionProbeCuller);
                                        if (switchOffStatic)
                                        {
                                            rp.gameObject.isStatic = false;
                                        }
                                    }
                                    else
                                    {
                                        ReflectionProbe rp = ReflectionProbeUtils.CreateReflectionProbe(newPosition, probeSize, entry.Key.terrain, protoProbe.m_reflectionProbeData, seaLeveLActive, seaLevel, target, false);
                                        if (switchOffStatic)
                                        {
                                            rp.gameObject.isStatic = false;
                                        }
                                    }
#else
                                    ReflectionProbe rp = ReflectionProbeUtils.CreateReflectionProbe(newPosition, probeSize, entry.Key.terrain, protoProbe.m_reflectionProbeData, seaLeveLActive, seaLevel, target, false);
                                    if (switchOffStatic)
                                    {
                                        rp.gameObject.isStatic = false;
                                    }
#endif

                                    instanceCounter++;
                                }

                                else if (protoProbe.m_probeType == ProbeType.LightProbe)
                                {
                                    float sampledHeight = entry.Key.terrain.SampleHeight(newPosition);
                                    if (sampledHeight > seaLevel)
                                    {
                                        newPosition.y = sampledHeight + 2.5f;
                                        Vector3 position = newPosition - lightProbeTransform.transform.position; //Translate to local space relative to lpg
                                        probePositions.Add(position);
                                        instanceCounter++;
                                    }

                                }
                            }
                        }
                    }

                    if (protoProbe.m_probeType == ProbeType.LightProbe)
                    {
                        lightProbeGroup.probePositions = probePositions.ToArray();
                    }
                }
            }
#endif
        }


#endregion

        #region BAKED MASKS


        /// <summary>
        /// Collects the affected terrains for evaluating collision masks. 
        /// </summary>
        public void CollectTerrainBakedMasks()
        {
            RenderTexture previousRT = RenderTexture.active;
            int terrainCollisionResolution = (int)Math.Min(m_range * 2, 4097);//m_originTerrain.terrainData.heightmapResolution; // (int)Math.Min(m_range*2,4097);

            m_collisionPixelSize = new Vector2(
            m_originTerrain.terrainData.size.x / (terrainCollisionResolution - 1.0f),
            m_originTerrain.terrainData.size.z / (terrainCollisionResolution - 1.0f));

            m_bakedMaskBrushTransform = TerrainPaintUtility.CalculateBrushTransform(m_originTerrain, GaiaUtils.ConvertPositonToTerrainUV(m_originTerrain, new Vector2(m_originTransform.position.x, m_originTransform.position.z)), m_range, m_originTransform.rotation.eulerAngles.y);

            m_bakedMaskPixels = GetPixelsForResolution(m_originTerrain.terrainData.size, m_bakedMaskBrushTransform.GetBrushXYBounds(), terrainCollisionResolution, terrainCollisionResolution, 0);

            CreateDefaultRenderTexture(ref RTbakedMask, terrainCollisionResolution, terrainCollisionResolution, RenderTextureFormat.R16);
            
            AddAffectedTerrainPixels(m_bakedMaskPixels, MultiTerrainOperationType.BakedMask, terrainCollisionResolution, terrainCollisionResolution);
        }


        /// <summary>
        /// Stores the combined collison mask info in RTCollision for the passed in collision mask array.
        /// </summary>
        /// <param name="collisionMasks">An array of collision masks for evaluation.</param>
        public void GetCollisionMask(CollisionMask[] collisionMasks)
        {
            GaiaSessionManager gaiaSessionManager = GaiaSessionManager.GetSessionManager(false);
            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();
            RenderTexture currentRT = RenderTexture.active;
            RenderTextureDescriptor rtDesc = RTbakedMask.descriptor;
            //rtDesc.width = targetTextureWidth;
            //rtDesc.height = targetTextureHeight;

            //RenderTexture output = new RenderTexture(rtDesc);

            RenderTexture.active = RTbakedMask;
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, m_bakedMaskPixels.width, 0, m_bakedMaskPixels.height);

            //prepare some non-render textures required for processing
            RenderTexture tempsourceTexture = RenderTexture.GetTemporary(rtDesc);
            //Texture2D tempsourceTexture = Texture2D.whiteTexture;
            //Texture2D finalTexture = new Texture2D(rtDesc.width, rtDesc.height);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.BakedMask);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;

                RenderTexture workaround1 = RenderTexture.GetTemporary(RTbakedMask.descriptor);
          
                RenderTexture completeCollisionMask = RenderTexture.GetTemporary(RTbakedMask.descriptor);
                RenderTexture.active = completeCollisionMask;
                //we start with a full white texture, the areas that would create a collision are painted in black
                GL.Clear(true, true, Color.white);


                //We need a buffer texture to continously feed the output of the tree collision baking back into the shader.
                //while still iterating through the trees.
                RenderTexture collisionMaskBuffer = RenderTexture.GetTemporary(completeCollisionMask.descriptor);
                //Clear the buffer for a clean start
                Graphics.Blit(completeCollisionMask, collisionMaskBuffer);

                Material combineMaterial = new Material(Shader.Find("Hidden/Gaia/CombineCollisionMasks"));
                combineMaterial.SetFloat("_Strength", 1f);



                foreach (CollisionMask collMask in collisionMasks.Where(x => x.m_active == true))
                {

                    int treePrototypeID = -99;

                    if (collMask.m_type == BakedMaskType.RadiusTree)
                    {
                        //Get the tree prototype ID from the spawn rule GUID stored in the collision mask
                        SpawnRule sr = CollisionMask.m_allTreeSpawnRules.FirstOrDefault(x => x.GUID == collMask.m_treeSpawnRuleGUID);
                        if (sr == null)
                        {
                            continue;
                        }

                        treePrototypeID = TerrainHelper.GetTreePrototypeIDFromSpawnRule(sr, entry.Key.terrain);

                        if (treePrototypeID == -99)
                        {
                            continue;
                        }
                    }

                    BakedMaskTypeInternal bmti = BakedMaskTypeInternal.RadiusTag;
                    switch (collMask.m_type)
                    {
                        case BakedMaskType.RadiusTag:
                            bmti = BakedMaskTypeInternal.RadiusTag;
                            break;
                        case BakedMaskType.RadiusTree:
                            bmti = BakedMaskTypeInternal.RadiusTree;
                            break;
                        case BakedMaskType.LayerGameObject:
                            bmti = BakedMaskTypeInternal.LayerGameObject;
                            break;
                        case BakedMaskType.LayerTree:
                            bmti = BakedMaskTypeInternal.LayerTree;
                            break;
                            //case BakedMaskType.GameObjectFreeSelection:
                            //    bmti = BakedMaskTypeInternal.GameObjectFreeSelection;
                            //    break;
                            //case BakedMaskType.GameObjectSpawnRule:
                            //    bmti = BakedMaskTypeInternal.GameObjectSpawnRule;
                            //    break;
                    }

                    tempsourceTexture = gaiaSessionManager.m_bakedMaskCache.LoadBakedMask(entry.Key.terrain, bmti, collMask, "", treePrototypeID);
                    if (tempsourceTexture != null)
                    {
                        if (collMask.m_type == BakedMaskType.LayerGameObject || collMask.m_type == BakedMaskType.LayerTree)
                        {
                            //For layer masks we invert how the invert flag works, since the meshes etc.
                            //that we want to avoid are rendered in white on the render texture.
                            combineMaterial.SetInt("_Invert", collMask.m_invert ? 0 : 1);
                        }
                        else
                        {
                            combineMaterial.SetInt("_Invert", collMask.m_invert ? 1 : 0);
                        }
                        combineMaterial.SetTexture("_InputTex", collisionMaskBuffer);
                        combineMaterial.SetTexture("_ImageMaskTex", tempsourceTexture);
                        combineMaterial.SetVector("_Dimensions", new Vector4(collisionMaskBuffer.width, collisionMaskBuffer.height, tempsourceTexture.width, tempsourceTexture.height));
                        Graphics.Blit(tempsourceTexture, completeCollisionMask, combineMaterial, 0);
                        //store result in buffer for the next iteration
                        Graphics.Blit(completeCollisionMask, collisionMaskBuffer);
                        
                    }
                }
                if (tempsourceTexture != null)
                {
                    //RenderTexture.active = completeCollisionMask;
                    //finalTexture.ReadPixels(new Rect(0, 0, completeCollisionMask.width, completeCollisionMask.height), 0, 0);
                    //finalTexture.Apply();
                    RenderTexture.active = RTbakedMask;
                   // FilterMode oldFilterMode = FilterMode.Point;
                    //oldFilterMode = completeCollisionMask.filterMode;
                    completeCollisionMask.filterMode = FilterMode.Bilinear;
                    blitMaterial.SetTexture("_MainTex", completeCollisionMask);
                    blitMaterial.SetPass(0);
                    CopyIntoPixels(entry.Value.affectedOperationPixels, entry.Value.affectedLocalPixels, completeCollisionMask);
                    //completeCollisionMask.filterMode = oldFilterMode;
                }
                RenderTexture.ReleaseTemporary(collisionMaskBuffer);
                collisionMaskBuffer = null;
                RenderTexture.ReleaseTemporary(completeCollisionMask);
                completeCollisionMask = null;
                RenderTexture.ReleaseTemporary(workaround1);
                workaround1 = null;

            }

            GL.PopMatrix();


            RenderTexture.active = currentRT;
            if (tempsourceTexture != Texture2D.whiteTexture && tempsourceTexture !=null)
            {
            //    tempsourceTexture.Release();
                tempsourceTexture = null;
            }
            //GameObject.DestroyImmediate(finalTexture, true);
            //finalTexture = null;

            //GC.Collect();
        }

        /// <summary>
        /// Stores the world biome mask info in RTCollision for the passed in world biome mask ID
        /// </summary>
        /// <param name="worldBiomeMaskId">The ID of the world biome mask we need to in the array.</param>
        public void GetWorldBiomeMask(string worldBiomeMaskGUID)
        {
            GaiaSessionManager gaiaSessionManager = GaiaSessionManager.GetSessionManager(false);
            Material blitMaterial = TerrainPaintUtility.GetBlitMaterial();
            RenderTexture currentRT = RenderTexture.active;
            RenderTextureDescriptor rtDesc = RTbakedMask.descriptor;

            RenderTexture.active = RTbakedMask;
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, m_bakedMaskPixels.width, 0, m_bakedMaskPixels.height);
            rtDesc.enableRandomWrite = true;
            //prepare some non-render textures required for processing
            //NOTE: Do not release this render texture! It is referenced in the baked mask cache in the session manager.
            //Releasing it from here will render the cache pointless!
            RenderTexture tempsourceTexture = RenderTexture.GetTemporary(rtDesc);
            //Texture2D tempsourceTexture = Texture2D.whiteTexture;
            //Texture2D finalTexture = new Texture2D(rtDesc.width, rtDesc.height);

            var relevantEntries = affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.BakedMask);

            foreach (var entry in relevantEntries)
            {
                if (!entry.Value.IsAffected())
                    continue;
                tempsourceTexture = gaiaSessionManager.m_bakedMaskCache.LoadBakedMask(entry.Key.terrain, BakedMaskTypeInternal.WorldBiomeMask, null, worldBiomeMaskGUID, 0);

                if (tempsourceTexture != null)
                {
                    //scale the source texture so it fits into the affected local pixels
                    rtDesc.width = RTbakedMask.width;
                    rtDesc.height = RTbakedMask.height;
                    RenderTexture scaledRT = RenderTexture.GetTemporary(rtDesc);
                    Graphics.Blit(tempsourceTexture, scaledRT);
                    RenderTexture.active = RTbakedMask;
                    tempsourceTexture.filterMode = FilterMode.Bilinear;
                    blitMaterial.SetTexture("_MainTex", tempsourceTexture);
                    blitMaterial.SetPass(0);
                    CopyIntoPixels(entry.Value.affectedOperationPixels, entry.Value.affectedLocalPixels, scaledRT);
                    RenderTexture.ReleaseTemporary(scaledRT);
                    scaledRT = null;
                }
            }

            GL.PopMatrix();


            RenderTexture.active = currentRT;


            //NOTE: Do not release the "tempsourceTexture" render texture here! It is referenced in the baked mask cache in the session manager.
            //Releasing it from here will render the cache pointless!

        }

#endregion

#endregion

#region HELPERS


        /// <summary>
        /// Add the resources related to the rules passed in into the terrain if they are not already there
        /// </summary>
        /// <param name="rules">Index of rules with resources that should be added to the terrain</param>
        public bool HandleMissingResources(Spawner spawner, SpawnRule rule, Terrain terrain)
        {
            TerrainMissingSpawnRules tmsr = m_terrainsMissingSpawnRules.Find(x => x.terrain == terrain && x.spawnRulesWithMissingResources.Contains(rule));
            if (tmsr != null)
            {
                rule.AddResourceToTerrain(spawner, new Terrain[1] { terrain });
                tmsr.spawnRulesWithMissingResources.Remove(rule);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clean up when this class is not longer needed
        /// </summary>
        public void CloseOperation()
        {
            if (RTbakedMask != null)
            {
                RTbakedMask.DiscardContents();
                RenderTexture.ReleaseTemporary(RTbakedMask);
                RTbakedMask = null;
            }
            if (RTdetailmap != null)
            {
                RTdetailmap.DiscardContents();
                RenderTexture.ReleaseTemporary(RTdetailmap);
                RTdetailmap = null;
            }
            if (RTgameObject != null)
            {
                RTgameObject.DiscardContents();
                RenderTexture.ReleaseTemporary(RTgameObject);
                RTgameObject = null;
            }
            if (RTheightmap != null)
            {
                RTheightmap.DiscardContents();
                RenderTexture.ReleaseTemporary(RTheightmap);
                RTheightmap = null;
            }
            if (RTnormalmap != null)
            {
                RTnormalmap.DiscardContents();
                RenderTexture.ReleaseTemporary(RTnormalmap);
                RTnormalmap = null;
            }
            if (RTterrainTree != null)
            {
                RTterrainTree.DiscardContents();
                RenderTexture.ReleaseTemporary(RTterrainTree);
                RTterrainTree = null;
            }
            if (RTtextureSplatmap != null)
            {
                RTtextureSplatmap.DiscardContents();
                RenderTexture.ReleaseTemporary(RTtextureSplatmap);
                RTtextureSplatmap = null;
            }

            ////Sync heightmaps after the changes
            foreach (var terrain in affectedHeightmapData)
            {
                terrain.terrainData.SyncHeightmap();
                terrain.editorRenderFlags = TerrainRenderFlags.All;
            }

            ////Sync splatmaps after the changes
            foreach (var terrainData in affectedSplatmapData)
            {
                terrainData.SetBaseMapDirty();
                terrainData.SyncTexture(TerrainData.AlphamapTextureName);
            }

            m_terrainsMissingSpawnRules.Clear();

            QualitySettings.masterTextureLimit = m_originalMasterTextureLimit;
        }


        /// <summary>
        /// Helper function to copy a pixel rect onto the render texture.
        /// </summary>
        /// <param name="targetPixels">The target pixels on the render texture.</param>
        /// <param name="originalPixels">The original pixels to be copied.</param>
        /// <param name="originalTexture">The original texture to copy from.</param>
        public static void CopyIntoPixels(RectInt targetPixels, RectInt originalPixels, Texture originalTexture)
        {
            if ((targetPixels.width + targetPixels.height > 1))
            {
                GL.Begin(GL.QUADS);
                GL.Color(new Color(1.0f, 1.0f, 1.0f, 1.0f));

                float width = originalPixels.width / (float)originalTexture.width;
                float height = originalPixels.height / (float)originalTexture.height;
                float x = originalPixels.x / (float)originalTexture.width;
                float y = originalPixels.y / (float)originalTexture.height;

                Rect originalUVs = new Rect(x, y, width, height);

                //Construct the vertices
                //Vertex 1
                GL.TexCoord2(originalUVs.x, originalUVs.y);
                GL.Vertex3(targetPixels.x, targetPixels.y, 0.0f);
                //Vertex2
                GL.TexCoord2(originalUVs.x, originalUVs.yMax);
                GL.Vertex3(targetPixels.x, targetPixels.yMax, 0.0f);
                //Vertex3
                GL.TexCoord2(originalUVs.xMax, originalUVs.yMax);
                GL.Vertex3(targetPixels.xMax, targetPixels.yMax, 0.0f);
                //Vertex4
                GL.TexCoord2(originalUVs.xMax, originalUVs.y);
                GL.Vertex3(targetPixels.xMax, targetPixels.y, 0.0f);
                GL.End();
            }
        }

        /// <summary>
        /// Helper function to set up shared material properties in shaders that perform heightmap changes
        /// </summary>
        /// <param name="material">The material in question</param>
        /// <param name="opType">The operation that is being performed.</param>
        public void SetupMaterialProperties(Material material, MultiTerrainOperationType opType)
        {
            RectInt pixelRect = new RectInt();
            Vector2 pixelSize = new Vector2();
            BrushTransform brushTransform = new BrushTransform();

            GetOperationInfo(ref pixelRect, ref pixelSize, ref brushTransform, opType);

            float pcSizeX = (pixelRect.width) * pixelSize.x;
            float pcSizeZ = (pixelRect.height) * pixelSize.y;
            Vector2 sizeU = pcSizeX * brushTransform.targetX;
            Vector2 sizeV = pcSizeZ * brushTransform.targetY;
            float operationOriginX = pixelRect.xMin * pixelSize.x;
            float operationOriginZ = pixelRect.yMin * pixelSize.y;

            Vector2 brushOffset = brushTransform.targetOrigin + operationOriginX * brushTransform.targetX + operationOriginZ * brushTransform.targetY;

            material.SetVector("_PCUVToBrushUVScales", new Vector4(sizeU.x, sizeU.y, sizeV.x, sizeV.y));
            material.SetVector("_PCUVToBrushUVOffset", new Vector4(brushOffset.x, brushOffset.y, 0.0f, 0.0f));

            //Debug only
            //Vector4 _PCUVToBrushUVScales = new Vector4(sizeU.x, sizeU.y, sizeV.x, sizeV.y);
            //Vector2 _PCUVToBrushUVOffset = new Vector4(brushOffset.x, brushOffset.y);
            //for (int x = 0; x <= pixelRect.width; x++)
            //{
            //    for (int y = 0; y <= pixelRect.height; y++)
            //    {
            //        Vector2 pcUV = new Vector2(x, y);
            //        Vector2 brushUV = PaintContextUVToBrushUV(_PCUVToBrushUVScales, _PCUVToBrushUVOffset, pcUV);
            //        Vector2 heightmapUV = PaintContextUVToHeightmapUV(pcUV);
            //    }
            //}
        }

        //private Vector2 PaintContextUVToBrushUV(Vector4 _PCUVToBrushUVScales, Vector2 _PCUVToBrushUVOffset, Vector2 pcUV)
        //{
        //    return new Vector2(_PCUVToBrushUVScales.x, _PCUVToBrushUVScales.y) * pcUV.x +
        //   new Vector2(_PCUVToBrushUVScales.z, _PCUVToBrushUVScales.w) * pcUV.y +
        //   _PCUVToBrushUVOffset;
        //}

        //private Vector2 PaintContextUVToHeightmapUV(Vector2 pcUV)
        //{
        //    return pcUV;
        //}

        private void GetOperationInfo(ref RectInt pixelRect, ref Vector2 pixelSize, ref BrushTransform brushTransform, MultiTerrainOperationType opType)
        {
            switch (opType)
            {
                case MultiTerrainOperationType.Heightmap:
                    pixelRect = m_heightmapPixels;
                    pixelSize = m_heightmapPixelSize;
                    brushTransform = m_heightmapBrushTransform;
                    break;
            }
        }

        /// <summary>
        /// Helper function to add the terrains hit by the operation range to the dictionary of affected operation pixels.
        /// </summary>
        /// <param name="opRect">Rect representing the operation</param>
        /// <param name="opType">Operation Type</param>
        /// <param name="opWidth">Width of the operation</param>
        /// <param name="opHeight">Height of the operation</param>
        /// <param name="layer">Terrain layer (if textures / splatmap operation)</param>
        private void AddAffectedTerrainPixels(RectInt opRect, MultiTerrainOperationType opType, int opWidth, int opHeight, TerrainLayer layer = null)
        {
            //clear out any old data associated with this operation type
            foreach (Terrain t in Terrain.activeTerrains)
            {
                affectedTerrainPixels.Remove(new TerrainOperation() { terrain = t, operationType = opType });
            }
            //Transfer the 2d PixelRect in a 3D Bounds object to see where it intersects with terrain bounds
            //Bounds brushBounds = new Bounds(new Vector3(opRect.center.x, m_originTerrain.transform.position.y + m_originTerrain.terrainData.size.y / 2f, opRect.center.y), new Vector3(opRect.width, m_originTerrain.terrainData.size.y, opRect.height));
            //needs to be in world space
            //brushBounds.center = new Vector3(brushBounds.center.x + m_originTerrain.transform.position.x, m_originTerrain.transform.position.y, brushBounds.center.z + m_originTerrain.transform.position.z);

            //Take rotation into account for the brush bounds calculation
            float rad = (m_originTransform.rotation.eulerAngles.y + 45) * Mathf.Deg2Rad;
            float rotatedRange = m_range * Mathf.Sqrt(2) * Mathf.Max(Mathf.Abs(Mathf.Cos(rad)), Mathf.Abs(Mathf.Sin(rad)));

            //Use max range for y to make sure we always catch the terrains regardless of the y-distance to the terrains
            Bounds brushBounds = new Bounds(m_originTransform.position, new Vector3(rotatedRange, float.MaxValue, rotatedRange));

            //remove 1 from width and height - if the brushBounds ends exactly at the border to another terrain, and the border of that terrain touches brush bounds on the edge
            //it would be included in the operation. Not good for world spawning as this drags in a lot of unneccesary terrains into the operation!

            brushBounds.size = new Vector3(brushBounds.size.x - 1, brushBounds.size.y - 1, brushBounds.size.z - 1);

            // add center tile
            if (layer == null)
            {
                if (m_validTerrainNames.Count == 0 || m_validTerrainNames.Contains(m_originTerrain.name))
                {
                    affectedTerrainPixels.Add(new TerrainOperation() { terrain = m_originTerrain, operationType = opType }, GetAffectedPixels(opRect, 0, 0, opWidth, opHeight, -1, -1));
                }
            }
            else
            {
                int tileLayerIndex = TerrainPaintUtility.FindTerrainLayerIndex(m_originTerrain, layer);
                if (tileLayerIndex == -1)
                {
                    if (m_validTerrainNames.Count == 0 || m_validTerrainNames.Contains(m_originTerrain.name))
                    {
                        affectedTerrainPixels.Add(new TerrainOperation() { terrain = m_originTerrain, operationType = opType }, GetAffectedPixels(opRect, 0, 0, opWidth, opHeight, -1, -1));
                    }
                    //Debug.LogWarning("Could not find layer index on terrain for layer " + layer.name);
                }
                else
                {
                    if (m_validTerrainNames.Count == 0 || m_validTerrainNames.Contains(m_originTerrain.name))
                    {
                        affectedTerrainPixels.Add(new TerrainOperation() { terrain = m_originTerrain, operationType = opType }, GetAffectedPixels(opRect, 0, 0, opWidth, opHeight, tileLayerIndex / 4, tileLayerIndex % 4));
                    }
                }
            }
            //Debug.Log("Brush Bounds Center: " + brushBounds.center.ToString() + " Extents: " + brushBounds.extents.ToString()); 

            //Go through all active terrains, process the ones that intersect with the brush & are affected by the change
            foreach (Terrain t in Terrain.activeTerrains)
            {
                //continue only if not the origin terrain, and if the terrain type (worldmap or non worldmap) matches the worldmap operation flag
                if (t != m_originTerrain && (TerrainHelper.IsWorldMapTerrain(t) == m_isWorldMapOperation))
                {
                    //Check needs to performed in world space, terrain bounds are in local space of the terrain
                    Bounds worldSpaceBounds = t.terrainData.bounds;
                    worldSpaceBounds.center = new Vector3(worldSpaceBounds.center.x + t.transform.position.x, worldSpaceBounds.center.y + t.transform.position.y, worldSpaceBounds.center.z + t.transform.position.z);
                    if (brushBounds.Intersects(worldSpaceBounds))
                    {
                        int horizDelta = GetHorizontalDelta(m_originTerrain, t);
                        int vertDelta = GetVerticalDelta(m_originTerrain, t);
                        if (layer == null)
                        {
                            if (m_validTerrainNames.Count == 0 || m_validTerrainNames.Contains(t.name))
                            {
                                affectedTerrainPixels.Add(new TerrainOperation() { terrain = t, operationType = opType }, GetAffectedPixels(opRect, horizDelta * (opWidth - 1), vertDelta * (opHeight - 1), opWidth, opHeight, -1, -1));
                            }
                        }
                        else
                        {
                            int tileLayerIndex = TerrainPaintUtility.FindTerrainLayerIndex(t, layer);
                            if (tileLayerIndex == -1)
                            {
                                if (m_validTerrainNames.Count == 0 || m_validTerrainNames.Contains(t.name))
                                {
                                    affectedTerrainPixels.Add(new TerrainOperation() { terrain = t, operationType = opType }, GetAffectedPixels(opRect, horizDelta * (opWidth - 1), vertDelta * (opHeight - 1), opWidth, opHeight, -1, -1));
                                }
                            }
                            else
                            {
                                if (m_validTerrainNames.Count == 0 || m_validTerrainNames.Contains(t.name))
                                {
                                    affectedTerrainPixels.Add(new TerrainOperation() { terrain = t, operationType = opType }, GetAffectedPixels(opRect, horizDelta * (opWidth - 1), vertDelta * (opHeight - 1), opWidth, opHeight, tileLayerIndex / 4, tileLayerIndex % 4));
                                }
                            }
                        }
                    }
                }
            }

        }


        /// <summary>
        /// Gets the horizontal difference in "terrain pieces" between a terrain and the origin terrain of the operation
        /// </summary>
        /// <param name="originTerrain">The origin terrain for this operation.</param>
        /// <param name="t">The terrain to determine the difference for.</param>
        /// <returns></returns>
        private int GetHorizontalDelta(Terrain originTerrain, Terrain t)
        {
            return Mathf.RoundToInt((t.transform.position.x - originTerrain.transform.position.x) / originTerrain.terrainData.size.x);
        }


        /// <summary>
        /// Gets the vertical difference in "terrain pieces" between a terrain and the origin terrain of the operation
        /// </summary>
        /// <param name="originTerrain">The origin terrain for this operation.</param>
        /// <param name="t">The terrain to determine the difference for.</param>
        /// <returns></returns>
        private int GetVerticalDelta(Terrain originTerrain, Terrain t)
        {
            return Mathf.RoundToInt((t.transform.position.z - originTerrain.transform.position.z) / originTerrain.terrainData.size.z);
        }
        /// <summary>
        /// Calculates a Rect in the correct texture (heightmap, splatmap, etc.) resolution when the "localBounds" parameter would be laid over a terrain..
        /// </summary>
        /// <param name="terrainSize">Size of the terrain</param>
        /// <param name="localBounds">The local bounds to evaluate</param>
        /// <param name="inputTextureWidth">Width of the texture in question.</param>
        /// <param name="inputTextureHeight">HEight of the texture in question.</param>
        /// <param name="additionalSeam">Additional Pixels to add to the rect</param>
        /// <returns></returns>
        private RectInt GetPixelsForResolution(Vector3 terrainSize, Rect localBounds, int inputTextureWidth, int inputTextureHeight, int additionalSeam)
        {
            float Xsize = (inputTextureWidth - 1.0f) / terrainSize.x;
            float Ysize = (inputTextureHeight - 1.0f) / terrainSize.z;
            int xMin = Mathf.FloorToInt(localBounds.xMin * Xsize) - additionalSeam;
            int yMin = Mathf.FloorToInt(localBounds.yMin * Ysize) - additionalSeam;
            int xMax = Mathf.CeilToInt(localBounds.xMax * Xsize) + additionalSeam;
            int yMax = Mathf.CeilToInt(localBounds.yMax * Ysize) + additionalSeam;
            int width = xMax - xMin + 1;
            int height = yMax - yMin + 1;
            return new RectInt(xMin, yMin, width, height);
        }
        /// <summary>
        /// Gets the default preview material for the Gaia Stamper
        /// </summary>
        /// <returns></returns>
        public static Material GetDefaultGaiaStamperPreviewMaterial()
        {
            if (m_GaiaStamperPreviewMaterial == null)
                m_GaiaStamperPreviewMaterial = new Material(Shader.Find("Hidden/Gaia/StampPreview"));
            return m_GaiaStamperPreviewMaterial;
        }
        /// <summary>
        /// Gets the default preview material for the Gaia Spawner
        /// </summary>
        /// <returns></returns>
        public static Material GetDefaultGaiaSpawnerPreviewMaterial()
        {
            if (m_GaiaSpawnerPreviewMaterial == null)
                m_GaiaSpawnerPreviewMaterial = new Material(Shader.Find("Hidden/Gaia/SpawnerPreview"));
            return m_GaiaSpawnerPreviewMaterial;
        }

        /// <summary>
        /// Visualises an operation in the scene view
        /// </summary>
        /// <param name="opType">Operation Type</param>
        /// <param name="previewTexture">The contents to visualise.</param>
        /// <param name="mat">The material used for visualisaton.</param>
        /// <param name="pass">The pass used in the material.</param>
        public void Visualize(MultiTerrainOperationType opType, RenderTexture previewTexture, Material mat, int pass)
        {
            Texture meshTexture = previewTexture;
            meshTexture.filterMode = FilterMode.Point;
            FilterMode currentFilterMode = meshTexture.filterMode;
            Vector3 terrainPos = m_originTerrain.GetPosition();

            RectInt pixels = new RectInt();
            Vector2 pixelSize = new Vector2();
            BrushTransform brushTransform = new BrushTransform();

            //Pull correct resolution sizes according to operation type
            GetOperationInfo(ref pixels, ref pixelSize, ref brushTransform, opType);

            //Prepare all relevant material properties
            float heightmapPixelsWidth = 1.0f / meshTexture.width;
            float heigthmapPixelsHeight = 1.0f / meshTexture.height;
            int qPixelX = pixels.width;
            int qPixelY = pixels.height;
            int numVerts = qPixelX * qPixelY * (2 * 3);

#if UNITY_2019_3_OR_NEWER

            int vertexLimit = 16777216;
            int skipVertices = 1;
            while (numVerts > vertexLimit / 2)   
            {
                qPixelX = (qPixelX + 1) / 2;
                qPixelY = (qPixelY + 1) / 2;
                numVerts = qPixelX * qPixelY * (2 * 3);
                skipVertices *= 2;
            }
#endif

            float sizeX = pixelSize.x;
            float sizeY = 2.0f * m_originTerrain.terrainData.heightmapScale.y;
            float sizeZ = pixelSize.y;
            float operationOriginX = pixels.xMin * pixelSize.x;
            float operationOriginZ = pixels.yMin * pixelSize.y;
            float operationSizeX = pixelSize.x;
            float operationSizeZ = pixelSize.y;

            Vector2 sizeU = operationSizeX * brushTransform.targetX;
            Vector2 sizeV = operationSizeZ * brushTransform.targetY;
            Vector2 brushUVoffset = brushTransform.targetOrigin + operationOriginX * brushTransform.targetX + operationOriginZ * brushTransform.targetY;

            //Set material properties
#if UNITY_2019_3_OR_NEWER
            mat.SetVector("_QuadRez", new Vector4(qPixelX, qPixelY, numVerts, skipVertices));
#else
            mat.SetVector("_QuadRez", new Vector4(qPixelX, qPixelY, numVerts, 0.0f));
#endif
            mat.SetVector("_HeightmapUV_PCPixelsX", new Vector4(heightmapPixelsWidth, 0.0f, 0.0f, 0.0f));
            mat.SetVector("_HeightmapUV_PCPixelsY", new Vector4(0.0f, heigthmapPixelsHeight, 0.0f, 0.0f));
            mat.SetVector("_HeightmapUV_Offset", new Vector4(0.5f * heightmapPixelsWidth, 0.5f * heigthmapPixelsHeight, 0.0f, 0.0f));
            mat.SetTexture("_Heightmap", meshTexture);
            mat.SetVector("_ObjectPos_PCPixelsX", new Vector4(sizeX, 0.0f, 0.0f, 0.0f));
            mat.SetVector("_ObjectPos_HeightMapSample", new Vector4(0.0f, sizeY, 0.0f, 0.0f));
            mat.SetVector("_ObjectPos_PCPixelsY", new Vector4(0.0f, 0.0f, sizeZ, 0.0f));
            mat.SetVector("_ObjectPos_Offset", new Vector4(pixels.xMin * sizeX, 0.0f, pixels.yMin * sizeZ, 1.0f));
            mat.SetVector("_BrushUV_PCPixelsX", new Vector4(sizeU.x, sizeU.y, 0.0f, 0.0f));
            mat.SetVector("_BrushUV_PCPixelsY", new Vector4(sizeV.x, sizeV.y, 0.0f, 0.0f));
            mat.SetVector("_BrushUV_Offset", new Vector4(brushUVoffset.x, brushUVoffset.y, 0.0f, 1.0f));
            mat.SetTexture("_BrushTex", Texture2D.whiteTexture);
            mat.SetVector("_TerrainObjectToWorldOffset", terrainPos);
            mat.SetPass(pass);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, numVerts);

            meshTexture.filterMode = currentFilterMode;
        }



        private AffectedPixels GetAffectedPixels(RectInt opRect, int opCoordinateX, int opCoordinateY, int opWidth, int opHeight, int splatmapID, int channelID)
        {
            AffectedPixels returnPixels = new AffectedPixels();
            returnPixels.pixelCoordinate = new Vector2Int(opCoordinateX, opCoordinateY);

            returnPixels.affectedLocalPixels = new RectInt()
            {
                x = Mathf.Max(0, opRect.x - opCoordinateX),
                y = Mathf.Max(0, opRect.y - opCoordinateY),
                xMax = Mathf.Min(opWidth, opRect.xMax - opCoordinateX),
                yMax = Mathf.Min(opHeight, opRect.yMax - opCoordinateY)
            };
            returnPixels.affectedOperationPixels = new RectInt(
            returnPixels.affectedLocalPixels.x + returnPixels.pixelCoordinate.x - opRect.x,
            returnPixels.affectedLocalPixels.y + returnPixels.pixelCoordinate.y - opRect.y,
            returnPixels.affectedLocalPixels.width,
            returnPixels.affectedLocalPixels.height
            );

            returnPixels.channelID = channelID;
            returnPixels.splatMapID = splatmapID;

            return returnPixels;
        }

        /// <summary>
        /// Fetches an 1-dimensional Color array from the current destination render texture from this paint context.
        /// </summary>
        /// <returns></returns>
        private Color[] GetRTColorArray(RenderTexture targetTexture)
        {
            RenderTextureDescriptor rtDesc = targetTexture.descriptor;
            Texture2D copyTexture = new Texture2D(rtDesc.width, rtDesc.height, TextureFormat.RGBAFloat, false);
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = targetTexture;
            copyTexture.ReadPixels(new Rect(0f, 0f, rtDesc.width, rtDesc.height), 0, 0);
            copyTexture.Apply();
            var colors = copyTexture.GetPixels(0, 0, copyTexture.width, copyTexture.height);
            RenderTexture.active = currentRT;
            return colors;
        }

#endregion

    }

}
