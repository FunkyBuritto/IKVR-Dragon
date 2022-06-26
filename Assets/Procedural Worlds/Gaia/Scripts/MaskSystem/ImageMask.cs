using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using System.Linq;

namespace Gaia
{
    //The odd numbering is for alphabetical ordering of the enum values without destroying references to saved settings files
    public enum ImageMaskOperation { CollisionMask = 5, DistanceMask = 1, GlobalSpawnerMaskStack=13, GrowAndShrink=12, HeightMask = 2, ImageMask = 0, NoiseMask = 4, SlopeMask = 3, Smooth = 8, StrengthTransform = 6, TerrainTexture = 9, WorldBiomeMask = 11, PROConcaveConvex = 10, PROHydraulicErosion = 7 }
    //This "reduced" version of the operation enum is used in all mask locations that are not a spawn rule
    //This is done to filter out operations that make only sense in spawn rules (GlobalSpawnerOutputMask)
    public enum ImageMaskOperationReduced { CollisionMask = 5, DistanceMask = 1, GrowAndShrink = 12, HeightMask = 2, ImageMask = 0, NoiseMask = 4, SlopeMask = 3, Smooth = 8, StrengthTransform = 6, TerrainTexture = 9, WorldBiomeMask = 11, PROConcaveConvex = 10, PROHydraulicErosion = 7 }

    public enum ImageMaskBlendMode { Multiply, GreaterThan, SmallerThan, Add, Subtract }
    public enum ImageMaskDistanceMaskAxes {[Description("XZ Circular")] XZ, [Description("X only")] X, [Description("Z Only")] Z }
    public enum ImageMaskInfluence { Local, Global }
    public enum ImageMaskLocation { SpawnerGlobal,SpawnRule,Stamper,BiomeController,MaskMapExport}

    /// <summary>
    /// Toggle between two different ways of handling height masks
    /// Absolute will store the minimum and maximum value for the mask as absolute values relative to the sea levele
    /// Relative will store the minimum and maximum value for the mask as absolute values relative to the sea levele
    /// </summary>
    public enum HeightMaskType { WorldSpace, RelativeToSeaLevel }
    public enum HeightMaskUnit { Meter, Percent }

    [System.Serializable]
    public class ImageMask
    {
        public bool m_active = true;
        public bool m_invert = false;
        public bool m_hasErrors = false;
        public ImageMaskInfluence m_influence = ImageMaskInfluence.Local;
        public ImageMaskOperation m_operation;
        public ImageMaskBlendMode m_blendMode;
        public ImageMaskLocation m_imageMaskLocation;
        public float m_strength = 1f;
        public float m_seaLevel = 0f;
        //The maximum terrain height, NOT the theoretical maximum height, but the highest measured physical point on the terrain 
        public float m_maxWorldHeight = 0f;


        //The minimum terrain height, NOT the theoretical minimum height, but the lowest measured physical point on the terrain 
        public float m_minWorldHeight = 0f;

        public float m_xOffSet = 0f;
        public float m_zOffSet = 0f;
        public float m_xOffSetScalar = 0f;
        public float m_zOffSetScalar = 0f;

        //The current multi-terrain op we are working on - used to get heightmap, normalmap etc. for the affected area
        [NonSerialized]
        public GaiaMultiTerrainOperation m_multiTerrainOperation;

        //Image Mask specific
        public Texture2D ImageMaskTexture
        {
            get
            {
                if (m_imageMaskTexture == null)
                {
                    if (!string.IsNullOrEmpty(m_imageMaskTextureGUID))
                    {
#if UNITY_EDITOR
                        m_imageMaskTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_imageMaskTextureGUID), typeof(Texture2D));
#endif
                    }
                }
                return m_imageMaskTexture;
            }
            set
            {
                if (value != m_imageMaskTexture)
                {
                    m_imageMaskTexture = value;
                    if (value != null)
                    {
#if UNITY_EDITOR
                        m_imageMaskTextureGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
#endif
                    }
                    else
                    {
                        m_imageMaskTextureGUID = "";
                    }
                }
            }
        }

        private Texture2D m_imageMaskTexture;
        [SerializeField]
        private string m_imageMaskTextureGUID;

        public GaiaConstants.ImageMaskFilterMode m_imageMaskFilterMode;
        public Color m_imageMaskColorSelectionColor = Color.white;
        public float m_imageMaskColorSelectionAccuracy = 0.5f;

        //distance Mask specific
        public AnimationCurve m_distanceMaskCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() { time = 0, value = 1, weightedMode = WeightedMode.None }, new Keyframe() { time = 1, value = 0, weightedMode = WeightedMode.None } });

        //height Mask specific
        public AnimationCurve m_heightMaskCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() { time = 0, value = 0, weightedMode = WeightedMode.None }, new Keyframe() { time = 1, value = 1, weightedMode = WeightedMode.None } });

        //strength Transform specific
        public AnimationCurve m_strengthTransformCurve = NewAnimCurveStraightUpwards();

        public HeightMaskType m_heightMaskType = HeightMaskType.RelativeToSeaLevel;
        [SerializeField]
        private HeightMaskUnit m_heightMaskUnit = HeightMaskUnit.Percent;


        public float m_relativeHeightMin = 25f;
        public float m_relativeHeightMax = 75f;

        public HeightMaskUnit HeightMaskUnit
        {
            get
            {
                return m_heightMaskUnit;
            }
            set
            {
                if (value != m_heightMaskUnit)
                {
                    //Get the meter value for 1% of the terrain height - makes for an easy conversion.
                    float worldHeightDifference = m_maxWorldHeight - m_minWorldHeight;
                    float worldHeightPercentInMeter = worldHeightDifference / 100f;

                    if (value == HeightMaskUnit.Meter)
                    {
                        //Value was in % before - convert all values to meter accordingly
                        m_absoluteHeightMin *= worldHeightPercentInMeter;
                        m_absoluteHeightMax *= worldHeightPercentInMeter;
                        m_relativeHeightMin *= worldHeightPercentInMeter;
                        m_relativeHeightMax *= worldHeightPercentInMeter;
                    }
                    else
                    {
                        //Value was in Meter before - convert all values to % accordingly
                        m_absoluteHeightMin /= worldHeightPercentInMeter;
                        m_absoluteHeightMax /= worldHeightPercentInMeter;
                        m_relativeHeightMin /= worldHeightPercentInMeter;
                        m_relativeHeightMax /= worldHeightPercentInMeter;
                    }
                }
                m_heightMaskUnit = value;
            }
        }



        //The absolute minimum height for the heightmask selection, e.g. "the selection starts at 50 meters"
        public float m_absoluteHeightMin = 50;
        //The absolute maximum height for the heightmask selection, e.g. "the selection ends at 150 meters"
        public float m_absoluteHeightMax = 150;


        //This is a legacy field that is not really used in the height mask anymore, but serves as a flag to detect whether
        //a height mask has been created under an older data structure - it will then automatically be migrated when the mask is used.
        public float m_seaLevelRelativeHeightMin = -109876.54321f;

        //This is a legacy field that is not really used in the height mask anymore, but serves as a flag to detect whether
        //a height mask has been created under an older data structure - it will then automatically be migrated when the mask is used.
        public float m_seaLevelRelativeHeightMax = 109876.54321f;


        public bool tree1active = false;
        public bool tree2active = false;


        public AnimationCurve m_slopeMaskCurve = NewAnimCurveStraightUpwards();
        public float m_slopeMin = 0.0f;
        public float m_slopeMax = 0.1f;

        public ImageMaskDistanceMaskAxes m_distanceMaskAxes;

        public GaiaNoiseSettings m_gaiaNoiseSettings = new GaiaNoiseSettings();

#if UNITY_EDITOR
        public NoiseSettings m_noiseSettings;
        public NoiseToolSettings m_noiseToolSettings = new NoiseToolSettings();
        public NoiseSettingsGUI noiseSettingsGUI;
#endif


        public bool m_ShowNoiseTransformSettings = false;
        public bool m_ShowNoisePreviewTexture = true;
        public bool m_noisePreviewTextureLocked = false;
        public bool m_ShowNoiseTypeSettings = false;
        public bool m_scaleNoiseToTerrainSize = false;

        private Texture2D m_distanceMaskCurveTexture;
        private Texture2D distanceMaskCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_distanceMaskCurveTexture);
            }
        }

        private Texture2D m_heightMaskCurveTexture;
        private Texture2D heightMaskCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_heightMaskCurveTexture);
            }
        }

        private Texture2D m_slopeMaskCurveTexture;
        private Texture2D slopeMaskCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_slopeMaskCurveTexture);
            }
        }

        private Texture2D m_strengthTransformCurveTexture;
        private Texture2D strengthTransformCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_strengthTransformCurveTexture);
            }
        }

        //collision mask specific
        public bool m_collisionMaskExpanded = true;
        public CollisionMask[] m_collisionMasks = new CollisionMask[0];
#if UNITY_EDITOR
        public ReorderableList m_reorderableCollisionMaskList;
#endif


        #region Erosion Settings

        //Eroder class reference for the erosion feature
#if UNITY_EDITOR && GAIA_PRO_PRESENT
        private HydraulicEroder m_Eroder = null;
#endif

        public GaiaConstants.ErosionMaskOutput m_erosionMaskOutput = GaiaConstants.ErosionMaskOutput.Sediment;
        public float m_erosionSimScale = 9f;
        public float m_erosionHydroTimeDelta = 0.05f;
        public int m_erosionHydroIterations = 15;
        public float m_erosionThermalTimeDelta = 0.01f;
        public int m_erosionThermalIterations = 80;
        public int m_erosionThermalReposeAngle = 80;
        public float m_erosionPrecipRate = 0.5f;
        public float m_erosionEvaporationRate = 0.5f;
        public float m_erosionFlowRate = 0.5f;
        public float m_erosionSedimentCapacity = 0.5f;
        public float m_erosionSedimentDepositRate = 0.8f;
        public float m_erosionSedimentDissolveRate = 0.5f;
        public float m_erosionRiverBankDepositRate = 7.0f;
        public float m_erosionRiverBankDissolveRate = 5.0f;
        public float m_erosionRiverBedDepositRate = 5.0f;
        public float m_erosionRiverBedDissolveRate = 5.0f;
        public bool m_erosionShowAdvancedUI;
        public bool m_erosionShowThermalUI;
        public bool m_erosionShowWaterUI;
        public bool m_erosionShowSedimentUI;
        public bool m_erosionShowRiverBankUI;
        #endregion

        //smooth settings
        public float m_smoothVerticality = 0f;
        public float m_smoothBlurRadius = 1f;

        //Texture mask settings
        //public int m_textureLayerId = 0;
        public string m_textureMaskSpawnRuleGUID = "";
        public static SpawnRule[] m_allTextureSpawnRules;
        public static Spawner[] m_allTextureSpawners;
        public static string[] m_allTextureSpawnRuleNames;
        public static int[] m_allTextureSpawnRuleIndices;


        //Convex Concave settings
        public float m_concavity = 1f;
        public float m_concavityFeatureSize = 10f;
        private ComputeShader m_concavityShader;
        public bool m_foldedOut = true;
        public string m_selectedWorldBiomeMaskGUID;

        //GrowAndShrink settings
        public float m_growAndShrinkValue;

        //holds a reference to the output of the spawner global mask stack
        public RenderTexture m_globalSpawnerMaskStackRT;

        public void FreeTextureReferences()
        {
            m_imageMaskTexture = null;
        }

        public void CheckHeightMaskMigration()
        {
            if (m_seaLevelRelativeHeightMax != 109876.54321f || m_seaLevelRelativeHeightMin != -109876.54321f)
            {
                //This is a height mask created under the old height mask model, needs to be migrated
                switch (m_heightMaskType)
                {
                    case HeightMaskType.WorldSpace:
                        //This height mask was using the old "Absolute" setting before
                        //This equals "Relative to Sea Level in Meters" in the new logic
                        m_heightMaskType = HeightMaskType.RelativeToSeaLevel;
                        m_heightMaskUnit = HeightMaskUnit.Meter;
                        m_relativeHeightMin = m_seaLevelRelativeHeightMin;
                        m_relativeHeightMax = m_seaLevelRelativeHeightMax;
                        m_absoluteHeightMin = m_relativeHeightMin + m_seaLevel;
                        m_absoluteHeightMax = m_relativeHeightMax + m_seaLevel;
                        break;
                    case HeightMaskType.RelativeToSeaLevel:
                        //This height mask was using the old "Relative" setting before
                        //This equals "World Space in Percent" in the new logic
                        m_heightMaskType = HeightMaskType.WorldSpace;
                        m_heightMaskUnit = HeightMaskUnit.Percent;
                        m_absoluteHeightMin = m_relativeHeightMin;
                        m_absoluteHeightMax = m_relativeHeightMax;
                        float worldHeightDifference = Mathf.Max(1f, m_maxWorldHeight - m_minWorldHeight);
                        float worldHeightPercentInMeter = worldHeightDifference / 100f;
                        float seaLevelInPercent = m_seaLevel / worldHeightPercentInMeter;
                        m_relativeHeightMin = m_absoluteHeightMin - seaLevelInPercent;
                        m_relativeHeightMax = m_absoluteHeightMax - seaLevelInPercent;
                        break;
                }

                //fill in the magic numbers to mark the migration as complete
                m_seaLevelRelativeHeightMin = -109876.54321f;
                m_seaLevelRelativeHeightMax = 109876.54321f;
            }
        }

        /// <summary>
        /// Applies the mask to an input render texture and returns the result as render texture.
        /// </summary>
        /// <param name="inputTexture">The input texture</param>
        /// <returns>The processed output in a render texture.</returns>
        public RenderTexture Apply(RenderTexture inputTexture, RenderTexture outputTexture)
        {
            RenderTexture currentRT = RenderTexture.active;
#if UNITY_EDITOR
#if GAIA_PRO_PRESENT
            //clean up eroder if not in use anymore
            if (m_Eroder != null && m_operation != ImageMaskOperation.PROHydraulicErosion)
            {
                ClearEroder();
            }
#endif
            Material filterMat = GetCurrentFXFilterMaterial();
            if (filterMat == null)
            {
                Debug.LogWarning("Could not find a filter material for operation " + m_operation.ToString());
                return inputTexture;
            }

            filterMat.SetTexture("_InputTex", inputTexture);
            filterMat.SetFloat("_Strength", m_strength);
            if (m_operation != ImageMaskOperation.PROHydraulicErosion)
            {
                filterMat.SetInt("_Invert", m_invert ? 1 : 0);
            }
            else
            {
                //Special treatement for the hydraulic erosion mask: Flip the invert flag because the erosion map data seems to be inverted already
                filterMat.SetInt("_Invert", m_invert ? 0 : 1);
            }
            if (m_operation == ImageMaskOperation.NoiseMask && IsDefaultStrenghtCurve())
            {
                m_strengthTransformCurve = NewAnimCurveDefaultNoise();

            }
            ImageProcessing.BakeCurveTexture(m_strengthTransformCurve, strengthTransformCurveTexture);
            filterMat.SetTexture("_HeightTransformTex", strengthTransformCurveTexture);

            switch (m_operation)
            {
                case ImageMaskOperation.ImageMask:
#if !GAIA_PRO_PRESENT
                    if (m_imageMaskFilterMode != GaiaConstants.ImageMaskFilterMode.PROColorSelection)
                    {
#endif
                    filterMat.SetTexture("_ImageMaskTex", ImageMaskTexture);
                    filterMat.SetInt("_FilterMode", (int)m_imageMaskFilterMode);
                    filterMat.SetColor("_Color", m_imageMaskColorSelectionColor);
                    filterMat.SetFloat("_ColorAccuracy", m_imageMaskColorSelectionAccuracy);
                    filterMat.SetFloat("_XOffset", m_xOffSetScalar);
                    filterMat.SetFloat("_ZOffset", m_zOffSetScalar);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);
#if !GAIA_PRO_PRESENT
                    }
                    else
                    {
                        Graphics.Blit(inputTexture, outputTexture);
                    }
#endif
                    break;
                case ImageMaskOperation.DistanceMask:
                    ImageProcessing.BakeCurveTexture(m_distanceMaskCurve, distanceMaskCurveTexture);
                    filterMat.SetTexture("_DistanceMaskTex", distanceMaskCurveTexture);
                    filterMat.SetFloat("_XOffset", m_xOffSetScalar);
                    filterMat.SetFloat("_ZOffset", m_zOffSetScalar);
                    filterMat.SetFloat("_AxisMode", (int)m_distanceMaskAxes);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_DistanceMaskTex", null);
                    break;
                case ImageMaskOperation.HeightMask:
                    ImageProcessing.BakeCurveTexture(m_heightMaskCurve, heightMaskCurveTexture);
                    filterMat.SetTexture("_HeightMapTex", m_multiTerrainOperation.RTheightmap);
                    filterMat.SetTexture("_HeightMaskTex", heightMaskCurveTexture);

                    //calculate the correct scalar min max height values according to the current terrain and the sea level
                    Terrain currentTerrain = m_multiTerrainOperation.m_originTerrain;

                    float scalarSeaLevel = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, m_seaLevel);
                    float m_scalarMaxHeight = 0.5f;
                    float m_scalarMinHeight = 0f;
                    float minHeightInMeters = m_absoluteHeightMin;
                    float maxHeightInMeters = m_absoluteHeightMax;
                    float worldHeightDifference = m_maxWorldHeight - m_minWorldHeight;
                    float worldHeightPercentInMeter = worldHeightDifference / 100f;
                    if (m_heightMaskType == HeightMaskType.WorldSpace)
                    {
                        if (m_heightMaskUnit == HeightMaskUnit.Percent)
                        {
                            minHeightInMeters *= worldHeightPercentInMeter;
                            maxHeightInMeters *= worldHeightPercentInMeter;
                        }
                    }
                    else
                    {
                        if (m_heightMaskUnit == HeightMaskUnit.Percent)
                        {
                            minHeightInMeters = (m_relativeHeightMin * worldHeightPercentInMeter) + m_seaLevel;
                            maxHeightInMeters = (m_relativeHeightMax * worldHeightPercentInMeter) + m_seaLevel;
                        }
                        else
                        {
                            minHeightInMeters = m_relativeHeightMin + m_seaLevel;
                            maxHeightInMeters = m_relativeHeightMax + m_seaLevel;
                        }
                    }
                    m_scalarMaxHeight = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, maxHeightInMeters);
                    m_scalarMinHeight = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, minHeightInMeters);
                    //transfer the scalar 0..1 value to -0.5..0.5 as this is how it is used in the shader
                    m_scalarMaxHeight = Mathf.Lerp(0, 0.5f, m_scalarMaxHeight);
                    m_scalarMinHeight = Mathf.Lerp(0, 0.5f, m_scalarMinHeight);

                    filterMat.SetFloat("_MinHeight", m_scalarMinHeight);
                    filterMat.SetFloat("_MaxHeight", m_scalarMaxHeight);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_HeightMapTex", null);
                    filterMat.SetTexture("_HeightMaskTex", null);
                    break;
                case ImageMaskOperation.SlopeMask:
                    ImageProcessing.BakeCurveTexture(m_slopeMaskCurve, slopeMaskCurveTexture);

                    filterMat.SetTexture("_NormalMapTex", m_multiTerrainOperation.RTnormalmap);
                    filterMat.SetTexture("_SlopeMaskTex", slopeMaskCurveTexture);
                    filterMat.SetFloat("_MinSlope", m_slopeMin);
                    filterMat.SetFloat("_MaxSlope", m_slopeMax);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_NormalMapTex", null);
                    filterMat.SetTexture("_SlopeMaskTex", null);
                    break;
                case ImageMaskOperation.NoiseMask:
                    //noise settings can be null when the mask was never viewed in the inspector, e.g. from autospawning
                    if (m_noiseSettings == null)
                    {
                        m_noiseSettings = (NoiseSettings)ScriptableObject.CreateInstance(typeof(NoiseSettings));
                        //Try to initialize from our own Gaia Noise Settings
                        m_noiseSettings.transformSettings.translation = m_gaiaNoiseSettings.m_translation;
                        m_noiseSettings.transformSettings.rotation = m_gaiaNoiseSettings.m_rotation;
                        m_noiseSettings.transformSettings.scale = m_gaiaNoiseSettings.m_scale;
                        m_noiseSettings.domainSettings.noiseTypeName = m_gaiaNoiseSettings.m_noiseTypeName;
                        m_noiseSettings.domainSettings.noiseTypeParams = m_gaiaNoiseSettings.m_noiseTypeParams;
                        m_noiseSettings.domainSettings.fractalTypeName = m_gaiaNoiseSettings.m_fractalTypeName;
                        m_noiseSettings.domainSettings.fractalTypeParams = m_gaiaNoiseSettings.m_fractalTypeParams;
                    }

                    float previewSize = 1 / m_multiTerrainOperation.m_originTerrain.terrainData.size.x;

                    // get proper noise material from current noise settings
                    NoiseSettings noiseSettings = m_noiseSettings;

                    Material matNoise = NoiseUtils.GetDefaultBlitMaterial(m_noiseSettings);

                    // setup the noise material with values in noise settings
                    m_noiseSettings.SetupMaterial(matNoise);

                    // convert brushRotation to radians
                    float brushRotation = 0;
                    //brushRotation *= Mathf.PI / 180;
                    Vector3 brushPosWS = m_multiTerrainOperation.m_originTransform.position + (Vector3)TerrainLoaderManager.Instance.GetOrigin();
                    float brushSize = m_multiTerrainOperation.m_range;

                    //Adjust scaling depending on whether we want to scale up with terrain size
                    if (m_scaleNoiseToTerrainSize)
                    {
                        bool isWorldSpace = (m_noiseToolSettings.coordSpace == CoordinateSpace.World);
                        brushSize = isWorldSpace ? brushSize * previewSize : 1;
                        brushPosWS = isWorldSpace ? brushPosWS * previewSize : Vector3.zero;
                    }
                    else
                    {
                        brushSize = m_multiTerrainOperation.m_range / 512;
                        brushPosWS = (brushPosWS / m_multiTerrainOperation.m_originTerrain.terrainData.size.x) * (m_multiTerrainOperation.m_originTerrain.terrainData.size.x / m_multiTerrainOperation.m_range) * brushSize;
                    }

                    // // override noise transform
                    Quaternion rotQ = Quaternion.AngleAxis(-brushRotation, Vector3.up);
                    Matrix4x4 translation = Matrix4x4.Translate(brushPosWS);
                    Matrix4x4 rotation = Matrix4x4.Rotate(rotQ);
                    Matrix4x4 scale = Matrix4x4.Scale(Vector3.one * brushSize);
                    Matrix4x4 noiseToWorld = translation * scale;

                    matNoise.SetMatrix(NoiseSettings.ShaderStrings.transform,
                                        m_noiseSettings.trs * noiseToWorld);
                    RenderTexture noiseRT = RenderTexture.GetTemporary(m_multiTerrainOperation.RTheightmap.descriptor);
                    int noisePass = NoiseUtils.kNumBlitPasses * NoiseLib.GetNoiseIndex(m_noiseSettings.domainSettings.noiseTypeName);
                    Graphics.Blit(inputTexture, noiseRT, matNoise, noisePass);
                    //now that we have the noise, put it in a simple image mask operation to get the final result
                    filterMat.SetTexture("_ImageMaskTex", noiseRT);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    RenderTexture.ReleaseTemporary(noiseRT);
                    filterMat.SetTexture("_ImageMaskTex", null);
                    noiseRT = null;
                    break;
                case ImageMaskOperation.CollisionMask:
                    RenderTexture.active = currentRT;
                    m_multiTerrainOperation.GetCollisionMask(m_collisionMasks);
                    filterMat.SetTexture("_ImageMaskTex", m_multiTerrainOperation.RTbakedMask);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);
                    break;
                case ImageMaskOperation.StrengthTransform:
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    break;

                case ImageMaskOperation.PROHydraulicErosion:
#if GAIA_PRO_PRESENT
                    m_multiTerrainOperation.RTheightmap.filterMode = FilterMode.Bilinear;
                    Material erosionMat = new Material(Shader.Find("Hidden/GaiaPro/SimpleHeightBlend"));
                    if (m_Eroder == null)
                    {
                        m_Eroder = new HydraulicEroder();
                        m_Eroder.OnEnable();
                    }
                    m_Eroder.inputTextures["Height"] = m_multiTerrainOperation.RTheightmap;

                    Vector2 texelSize = new Vector2(m_multiTerrainOperation.m_originTerrain.terrainData.size.x / m_multiTerrainOperation.m_originTerrain.terrainData.heightmapResolution,
                                                    m_multiTerrainOperation.m_originTerrain.terrainData.size.z / m_multiTerrainOperation.m_originTerrain.terrainData.heightmapResolution);

                    //apply Erosion settings
                    m_Eroder.m_ErosionSettings.m_SimScale.value = m_erosionSimScale;
                    m_Eroder.m_ErosionSettings.m_HydroTimeDelta.value = m_erosionHydroTimeDelta;
                    m_Eroder.m_ErosionSettings.m_HydroIterations.value = m_erosionHydroIterations;
                    m_Eroder.m_ErosionSettings.m_ThermalTimeDelta = m_erosionThermalTimeDelta;
                    m_Eroder.m_ErosionSettings.m_ThermalIterations = m_erosionThermalIterations;
                    m_Eroder.m_ErosionSettings.m_ThermalReposeAngle = m_erosionThermalReposeAngle;
                    m_Eroder.m_ErosionSettings.m_PrecipRate.value = m_erosionPrecipRate;
                    m_Eroder.m_ErosionSettings.m_EvaporationRate.value = m_erosionEvaporationRate;
                    m_Eroder.m_ErosionSettings.m_FlowRate.value = m_erosionFlowRate;
                    m_Eroder.m_ErosionSettings.m_SedimentCapacity.value = m_erosionSedimentCapacity;
                    m_Eroder.m_ErosionSettings.m_SedimentDepositRate.value = m_erosionSedimentDepositRate;
                    m_Eroder.m_ErosionSettings.m_SedimentDissolveRate.value = m_erosionSedimentDissolveRate;
                    m_Eroder.m_ErosionSettings.m_RiverBankDepositRate.value = m_erosionRiverBankDepositRate;
                    m_Eroder.m_ErosionSettings.m_RiverBankDissolveRate.value = m_erosionRiverBankDissolveRate;
                    m_Eroder.m_ErosionSettings.m_RiverBedDepositRate.value = m_erosionRiverBedDepositRate;
                    m_Eroder.m_ErosionSettings.m_RiverBedDissolveRate.value = m_erosionRiverBedDissolveRate;

                    //and erode
                    m_Eroder.ErodeHeightmap(m_multiTerrainOperation.m_originTerrain.terrainData.size, m_multiTerrainOperation.m_terrainDetailBrushTransform.GetBrushXYBounds(), texelSize, false);
                    Vector4 erosionBrushParams = new Vector4(1f, 0.0f, 0.0f, 0.0f);
                    //if (activeLocalFilters)
                    erosionMat.SetTexture("_BrushTex", inputTexture);
                    //else
                    //    erosionMat.SetTexture("_BrushTex", localinputTexture);
                    switch (m_erosionMaskOutput)
                    {
                        //case GaiaConstants.ErosionMaskOutput.ErodedSediment:
                        //    erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Eroded Sediment"]);
                        //    break;
                        //case GaiaConstants.ErosionMaskOutput.Height:
                        //    erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Height"]);
                        //    break;
                        case GaiaConstants.ErosionMaskOutput.Sediment:
                            erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Sediment"]);
                            break;
                        case GaiaConstants.ErosionMaskOutput.WaterFlux:
                            erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Water Flux"]);
                            break;
                        //case GaiaConstants.ErosionMaskOutput.WaterLevel:
                        //    erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Water Level"]);
                        //    break;
                        case GaiaConstants.ErosionMaskOutput.WaterVelocity:
                            erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Water Velocity"]);
                            break;
                    }

                    erosionMat.SetVector("_BrushParams", erosionBrushParams);

                    RenderTexture eroderTempRT = RenderTexture.GetTemporary(m_Eroder.outputTextures["Height"].descriptor);

                    m_multiTerrainOperation.SetupMaterialProperties(erosionMat, MultiTerrainOperationType.Heightmap);
                    Graphics.Blit(m_multiTerrainOperation.RTheightmap, eroderTempRT, erosionMat, 0);

                    filterMat.SetTexture("_InputTex", eroderTempRT);
                    Graphics.Blit(eroderTempRT, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_InputTex", null);
                    erosionMat.SetTexture("_NewHeightTex", null);
                    m_Eroder.ReleaseRenderTextures();
                    RenderTexture.ReleaseTemporary(eroderTempRT);
#else
                    Graphics.Blit(inputTexture, outputTexture);
#endif
                    break;
                case ImageMaskOperation.Smooth:
                    Vector4 brushParams = new Vector4(1f, 0.0f, 0.0f, 0.0f);
                    filterMat.SetTexture("_MainTex", inputTexture);
                    filterMat.SetTexture("_BrushTex", Texture2D.whiteTexture);
                    filterMat.SetTexture("_HeightTransformTex", strengthTransformCurveTexture);
                    filterMat.SetVector("_BrushParams", brushParams);
                    Vector4 smoothWeights = new Vector4(
                        Mathf.Clamp01(1.0f - Mathf.Abs(m_smoothVerticality)),   // centered
                        Mathf.Clamp01(-m_smoothVerticality),                    // min
                        Mathf.Clamp01(m_smoothVerticality),                     // max
                        m_smoothBlurRadius);                                  // kernel size
                    filterMat.SetVector("_SmoothWeights", smoothWeights);

                    //Do not set up the UV properties according to the operation. In this case, this would lead to the "smoothness brush" 
                    //being rotated inside our existing mask stack.
                    filterMat.SetVector("_PCUVToBrushUVScales", new Vector4(1, 0, 0, 1));
                    filterMat.SetVector("_PCUVToBrushUVOffset", new Vector4(0, 0, 0.0f, 0.0f));

                    // Two pass blur (first horizontal, then vertical)
                    //RenderTexture workaround1 = RenderTexture.GetTemporary(m_multiTerrainOperation.RTheightmap.descriptor);
                    RenderTexture tmpsmoothRT = new RenderTexture(m_multiTerrainOperation.RTheightmap.descriptor);
                    //tmpsmoothRT = RenderTexture.GetTemporary(m_multiTerrainOperation.RTheightmap.descriptor);
                    Graphics.Blit(inputTexture, tmpsmoothRT, filterMat, 0);
                    Graphics.Blit(tmpsmoothRT, outputTexture, filterMat, 1);

                    filterMat.SetTexture("_MainTex", null);
                    filterMat.SetTexture("_BrushTex", null);
                    filterMat.SetTexture("_HeightTransformTex", null);
                    tmpsmoothRT.Release();
                    GameObject.DestroyImmediate(tmpsmoothRT);

                    //RenderTexture.ReleaseTemporary(tmpsmoothRT);
                    //RenderTexture.ReleaseTemporary(workaround1);


                    break;
                case ImageMaskOperation.TerrainTexture:
                    SpawnRule sr = m_allTextureSpawnRules.FirstOrDefault(x => x.GUID == m_textureMaskSpawnRuleGUID);
                    if (sr != null)
                    {
                        Spawner spawner = m_allTextureSpawners.FirstOrDefault(x => x.m_settings.m_spawnerRules.Contains(sr));
                        if (spawner != null)
                        {
                            ResourceProtoTexture proto = spawner.m_settings.m_resources.m_texturePrototypes[sr.m_resourceIdx];
                            TerrainLayer layer = TerrainHelper.GetLayerFromPrototype(proto);
                            if (layer != null)
                            {
                                m_multiTerrainOperation.GetSplatmap(layer);
                                filterMat.SetTexture("_ImageMaskTex", m_multiTerrainOperation.RTtextureSplatmap);
                                Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                                filterMat.SetTexture("_ImageMaskTex", null);
                            }
                            else
                            {
                                filterMat.SetTexture("_ImageMaskTex", Texture2D.blackTexture);
                                Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                                filterMat.SetTexture("_ImageMaskTex", null);
                            }
                        }
                    }
                    break;
                case ImageMaskOperation.PROConcaveConvex:
#if GAIA_PRO_PRESENT
                    m_concavityShader = (ComputeShader)(Resources.Load("GaiaConcavity"));
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    int kidx = m_concavityShader.FindKernel("ConcavityMultiply");

                    switch (m_blendMode)
                    {
                        case ImageMaskBlendMode.GreaterThan:
                            kidx = m_concavityShader.FindKernel("ConcavityGreaterThan");
                            break;
                        case ImageMaskBlendMode.SmallerThan:
                            kidx = m_concavityShader.FindKernel("ConcavitySmallerThan");
                            break;
                        case ImageMaskBlendMode.Add:
                            kidx = m_concavityShader.FindKernel("ConcavityAdd");
                            break;
                        case ImageMaskBlendMode.Subtract:
                            kidx = m_concavityShader.FindKernel("ConcavitySubtract");
                            break;
                    }

                    m_concavityShader.SetTexture(kidx, "In_BaseMaskTex", inputTexture);

                    m_concavityShader.SetTexture(kidx, "In_HeightTex", m_multiTerrainOperation.RTheightmap);
                    m_concavityShader.SetTexture(kidx, "In_HeightTransformTex", strengthTransformCurveTexture);
                    m_concavityShader.SetInt("HeightTransformTexResolution", strengthTransformCurveTexture.width - 1);
                    m_concavityShader.SetTexture(kidx, "OutputTex", outputTexture);
                    //cs.SetTexture(kidx, "RemapTex", remapTex);

                    m_concavityShader.SetVector("HeightmapResolution", new Vector2(m_multiTerrainOperation.RTheightmap.width, m_multiTerrainOperation.RTheightmap.height));
                    m_concavityShader.SetVector("TextureResolution", new Vector4(inputTexture.width, inputTexture.height, m_concavityFeatureSize, m_concavity));
                    m_concavityShader.Dispatch(kidx, outputTexture.width, outputTexture.height, 1);
                 
                    m_concavityShader = null;
#else
                    Graphics.Blit(inputTexture, outputTexture);
#endif
                    break;
                case ImageMaskOperation.WorldBiomeMask:
                    //fetch the world biome mask
                    //RenderTexture.active = currentRT;
                    m_multiTerrainOperation.GetWorldBiomeMask(m_selectedWorldBiomeMaskGUID);

                    filterMat.SetTexture("_ImageMaskTex", m_multiTerrainOperation.RTbakedMask);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);

    
                    break;
                case ImageMaskOperation.GrowAndShrink:
                    //translate the grow shrink distance into a scalar terrain value, as the shader works with U/V coordinates
                    float scalarDistance = m_growAndShrinkValue / m_multiTerrainOperation.m_originTerrain.terrainData.size.x;
                    filterMat.SetFloat("_Distance", scalarDistance);
                    filterMat.SetFloat("_TexelSize", inputTexture.texelSize.x);
                    //Alwways on pass 0 as this is the one with the active strength transform
                    Graphics.Blit(inputTexture, outputTexture, filterMat, 0);
                    break;
                case ImageMaskOperation.GlobalSpawnerMaskStack:
                    //treat this mask type like an image mask that takes in the output from the spawner mask stack as direct input
                    filterMat.SetTexture("_ImageMaskTex", m_globalSpawnerMaskStackRT);
                    Graphics.Blit(inputTexture, outputTexture, filterMat, (int)m_blendMode);
                    filterMat.SetTexture("_ImageMaskTex", null);
                    break;
                default:
                    break;
            }

            filterMat.SetTexture("_InputTex", null);
            filterMat.SetTexture("_HeightTransformTex", null);

            GameObject.DestroyImmediate(filterMat);

            //release input texture
            //if (inputTexture != null)
            //{
            //    inputTexture.Release();
            //    GameObject.DestroyImmediate(inputTexture);
            //    inputTexture = null;
            //}
#endif
            return outputTexture;

        }


        public void ClearEroder()
        {
#if UNITY_EDITOR && GAIA_PRO_PRESENT
            if (m_Eroder != null)
            {
                m_Eroder.ReleaseRenderTextures();
                m_Eroder = null;
            }
#endif
        }

        private float CalculateScalarHeightRelativeToSeaLevel(float heightValue, float scalarSeaLevel)
        {
            if (heightValue < 0.25f)
            {
                //The position is below the marked sea level on the slider -> lerp accordingly
                heightValue = Mathf.Lerp(0f, scalarSeaLevel, Mathf.InverseLerp(0, 0.25f, heightValue));
            }
            else
            {
                //The position is above the marked sea level on the slider -> lerp accordingly
                heightValue = Mathf.Lerp(scalarSeaLevel, 1f, Mathf.InverseLerp(0.25f, 1f, heightValue));
            }

            return heightValue;
        }

        /// <summary>
        /// sets up the default linear strenght transform curve that maps the input 1:1 to the output (equals no transformation at all)
        /// </summary>
        /// <param name="max">The maximum value at which the curve ends.</param>
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveStraightUpwards(float max = 1f)
        {
            return new AnimationCurve(new Keyframe[2] { new Keyframe() {
                                                                        inTangent = 1,
                                                                        inWeight = 0,
                                                                        outTangent = 1,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 1,
                                                                        inWeight = 0,
                                                                        outTangent = 1,
                                                                        outWeight = 0,
                                                                        time = 1,
                                                                        value = max,
                                                                        weightedMode = WeightedMode.None
                                                                        } }); ;
        }
        /// <summary>
        /// Checks if the current strenght transform curve is still the default linear curve set at initialization
        /// </summary>
        /// <returns>true if original curve, false if the user altered it</returns>
        private bool IsDefaultStrenghtCurve()
        {
            //Get a default anim curve for comparison
            AnimationCurve defaultCurve = NewAnimCurveStraightUpwards();

            //different number of keys? => it is a different curve
            if (m_strengthTransformCurve.keys.Length != defaultCurve.keys.Length)
            {
                return false;
            }
            //keyframe data different from original? => it is a different curve
            for (int i = 0; i < m_strengthTransformCurve.keys.Length; i++)
            {
                Keyframe current = m_strengthTransformCurve.keys[i];
                Keyframe original = defaultCurve.keys[i];
                if (current.inTangent != original.inTangent ||
                    current.inWeight != original.inWeight ||
                    current.outTangent != original.outTangent ||
                    current.outWeight != original.outWeight ||
                    current.time != original.time ||
                    current.value != original.value ||
                    current.weightedMode != original.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }



        /// <summary>
        /// Sets up a distance map curve suitable for the "water border" style for the base map creation in the random terrain generator.
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveWaterBorder()
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 1,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = -3.183739f,
                                                                        inWeight = 0.02412868f,
                                                                        outTangent = -3.183739f,
                                                                        outWeight = 0,
                                                                        time = 1f,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    }
                                                       }); ;
            return returnCurve;
        }

        /// <summary>
        /// Sets up a distance map curve suitable for the "mountain border" style for the base map creation in the random terrain generator.
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveMountainBorderDistance()
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[2] { new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 0.5f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 1.301094f,
                                                                        inWeight = 0.04557639f,
                                                                        outTangent = 1.301094f,
                                                                        outWeight = 0,
                                                                        time = 1f,
                                                                        value = 1f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                       }); ;
            return returnCurve;
        }

        public static AnimationCurve NewAnimCurveMountainBorderStrength()
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[3] { new Keyframe() {
                                                                        inTangent = 1.102063f,
                                                                        inWeight = 0,
                                                                        outTangent = 1.102063f,
                                                                        outWeight = 0.2820168f,
                                                                        time = 0,
                                                                        value = 0f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 2.96729f,
                                                                        inWeight = 0.08592079f,
                                                                        outTangent = 2.96729f,
                                                                        outWeight = 0.2352394f,
                                                                        time = 0.6f,
                                                                        value = 1f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 2.606707f,
                                                                        inWeight = 0.1011895f,
                                                                        outTangent = 2.606707f,
                                                                        outWeight = 0f,
                                                                        time = 1f,
                                                                        value = 2f,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                       }); ;
            return returnCurve;
        }



        /// <summary>
        /// Sets up a better strenght curve for noise that has a steeper cutoff in strength.
        /// This curve is better suited to get small islands or patches of noise.
        /// </summary>
        /// <param name="distanceFromCenter">How far from the center of the curve the cutoff should take place. The smaller this value is, the "sharper" the cutoff will be for the noise pattern</param>
        /// <returns></returns>
        public static AnimationCurve NewAnimCurveDefaultNoise(float distanceFromCenter = 0.2f)
        {
            AnimationCurve returnCurve = new AnimationCurve(new Keyframe[4] { new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 0,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0f,
                                                                        outTangent = 2.5f,
                                                                        outWeight = 0.3333333f,
                                                                        time = 0.5f - distanceFromCenter,
                                                                        value = 0,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 2.5f,
                                                                        inWeight = 0f,
                                                                        outTangent = 0,
                                                                        outWeight = 0.3333333f,
                                                                        time = 0.5f + distanceFromCenter,
                                                                        value = 1,
                                                                        weightedMode = WeightedMode.None
                                                                    },
                                                        new Keyframe() {
                                                                        inTangent = 0,
                                                                        inWeight = 0,
                                                                        outTangent = 0,
                                                                        outWeight = 0,
                                                                        time = 1,
                                                                        value = 1,
                                                                        weightedMode = WeightedMode.None
                                                                        } }); ;
            return returnCurve;
        }

        private Material GetCurrentFXFilterMaterial()
        {
            string shaderName = "";

            switch (m_operation)
            {
                case ImageMaskOperation.ImageMask:
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.DistanceMask:
                    shaderName = "Hidden/Gaia/FilterDistanceMask";
                    break;
                case ImageMaskOperation.HeightMask:
                    shaderName = "Hidden/Gaia/FilterHeightMask";
                    break;
                case ImageMaskOperation.SlopeMask:
                    shaderName = "Hidden/Gaia/FilterSlopeMask";
                    break;
                case ImageMaskOperation.NoiseMask:
                    //We need the FilterImageMask as material for the final operation AFTER the noise has been calculated
                    //For the shader that creates the noise itself, see the implementation of the noise mask operation
                    //in Apply()
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.CollisionMask:
                    //We need the FilterImageMask as material for the final operation AFTER the Collision mask has been gathered from the operation.
                    //For collection & assembly of the collision mask data, see the implementation of the collision mask operation
                    //in Apply()
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.StrengthTransform:
                    shaderName = "Hidden/GaiaPro/StrengthTransform";
                    break;
                case ImageMaskOperation.PROHydraulicErosion:
                    //We need the Strength Transform as material for the final operation AFTER the Erosion mask has been gathered from the Eroder.
                    //For collection & assembly of the collision mask data, see the implementation of the erosion mask operation
                    //in Apply()
                    shaderName = "Hidden/GaiaPro/StrengthTransform";
                    break;
                case ImageMaskOperation.Smooth:
                    shaderName = "Hidden/Gaia/SmoothHeight";
                    break;
                case ImageMaskOperation.TerrainTexture:
                    //Here we just load the splatmap input into an image mask
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.PROConcaveConvex:
                    //Concave / Convex is calculated in compute shader, we use the strength transform as a workaround in this case
                    shaderName = "Hidden/GaiaPro/StrengthTransform";
                    break;
                case ImageMaskOperation.WorldBiomeMask:
                    //We need the FilterImageMask as material for the final operation AFTER the World Biome mask has been gathered from the operation.
                    //For collection & assembly of the world biome mask data, see the implementation of the world biome mask operation
                    //in Apply()
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                case ImageMaskOperation.GrowAndShrink:
                    shaderName = "Hidden/Gaia/GrowShrink";
                    break;
                case ImageMaskOperation.GlobalSpawnerMaskStack:
                    //For this mask we just treat the output of the global mask stack like an image mask
                    shaderName = "Hidden/Gaia/FilterImageMask";
                    break;
                default:
                    break;

            }

            if (shaderName == "")
            {
                return null;
            }

            return new Material(Shader.Find(shaderName));
        }

        public static void CheckMaskStackForInvalidTextureRules(string objectDescription, string objectName, ImageMask[] maskStack)
        {
            if (ImageMask.m_allTextureSpawners == null || ImageMask.m_allTextureSpawnRules == null)
            {
                ImageMask.RefreshSpawnRuleGUIDs();
            }

            foreach (ImageMask mask in maskStack)
            {
                //Is this a texture mask? If yes, check if the associated texture spawn rule exists, if not, put out a warning
                if (mask.m_operation == ImageMaskOperation.TerrainTexture)
                {
                    //only perform the check if there is actually a GUID selected in the mask
                    if (!string.IsNullOrEmpty(mask.m_textureMaskSpawnRuleGUID))
                    {
                        SpawnRule sr = ImageMask.m_allTextureSpawnRules.FirstOrDefault(x => x.GUID == mask.m_textureMaskSpawnRuleGUID);
                        if (sr == null)
                        {
                            mask.m_active = false;
                            mask.m_hasErrors = true;
                            Debug.LogWarning("The " + objectDescription + " '" + objectName + "' uses a Texture Mask that links to a non-existent texture spawn rule. The spawn might not work as intended. Please select the spawner, and assign the correct texture in the texture mask, or remove the texture mask altogether.");
                        }
                        else
                        {
                            mask.m_hasErrors = false;
                        }
                    }
                    else
                    {
                        mask.m_hasErrors = false;
                    }
                }

            }
        }

        public static void RefreshSpawnRuleGUIDs()
        {
            List<SpawnRule> tempTextureSpawnRules = new List<SpawnRule>();
            List<Spawner> tempTextureSpawner = new List<Spawner>();
            List<string> tempTextureSpawnRuleNames = new List<string>();


            List<SpawnRule> tempTreeSpawnRules = new List<SpawnRule>();
            List<Spawner> tempTreeSpawner = new List<Spawner>();
            List<string> tempTreeSpawnRuleNames = new List<string>();

            Spawner[] allSpawner = Resources.FindObjectsOfTypeAll<Spawner>();
            foreach (Spawner spawner in allSpawner)
            {
                foreach (SpawnRule sr in spawner.m_settings.m_spawnerRules)
                {
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTexture)
                    {
                        tempTextureSpawnRules.Add(sr);
                        tempTextureSpawnRuleNames.Add(sr.m_name);
                        if (!tempTextureSpawner.Contains(spawner))
                        {
                            tempTextureSpawner.Add(spawner);
                        }
                    }
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTree)
                    {
                        tempTreeSpawnRules.Add(sr);
                        tempTreeSpawnRuleNames.Add(sr.m_name);
                        if (!tempTreeSpawner.Contains(spawner))
                        {
                            tempTreeSpawner.Add(spawner);
                        }
                    }
                }
            }
            m_allTextureSpawnRuleIndices = Enumerable
                                               .Repeat(0, (int)((tempTextureSpawnRules.Count - 0) / 1) + 1)
                                               .Select((tr, ti) => tr + (1 * ti))
                                               .ToArray();

            CollisionMask.m_allTreeSpawnRuleIndices = Enumerable
                                               .Repeat(0, (int)((tempTreeSpawnRules.Count - 0) / 1) + 1)
                                               .Select((tr, ti) => tr + (1 * ti))
                                               .ToArray();

            CollisionMask.m_allTreeSpawnRules = tempTreeSpawnRules.ToArray();
            CollisionMask.m_allTreeSpawners = tempTreeSpawner.ToArray();
            CollisionMask.m_allTreeSpawnRuleNames = tempTreeSpawnRuleNames.ToArray();

            m_allTextureSpawnRules = tempTextureSpawnRules.ToArray();
            m_allTextureSpawners = tempTextureSpawner.ToArray();
            m_allTextureSpawnRuleNames = tempTextureSpawnRuleNames.ToArray();
        }

        public static ImageMask Clone(ImageMask source)
        {

            ImageMask target = new ImageMask();
#if UNITY_EDITOR
            GaiaUtils.CopyFields(source, target);

            //sprecial treatment for the GUID - is private
            target.m_imageMaskTextureGUID = source.m_imageMaskTextureGUID;

            //special treatment for the heightmask min max fields - those are properites which will not be copied by the field copy above
            target.m_absoluteHeightMax = source.m_absoluteHeightMax;
            target.m_absoluteHeightMin = source.m_absoluteHeightMin;


            //special treatment for all object fields
            target.m_distanceMaskCurve = new AnimationCurve(source.m_distanceMaskCurve.keys);
            target.m_heightMaskCurve = new AnimationCurve(source.m_heightMaskCurve.keys);
            target.m_slopeMaskCurve = new AnimationCurve(source.m_slopeMaskCurve.keys);
            target.m_strengthTransformCurve = new AnimationCurve(source.m_strengthTransformCurve.keys);

            if (source.m_gaiaNoiseSettings != null)
            {
                target.m_gaiaNoiseSettings = new GaiaNoiseSettings();
                GaiaUtils.CopyFields(source.m_gaiaNoiseSettings, target.m_gaiaNoiseSettings);
            }

            if (source.m_noiseSettings != null)
            {
                target.m_noiseSettings = (NoiseSettings)ScriptableObject.CreateInstance(typeof(NoiseSettings));
                GaiaUtils.CopyFields(source.m_noiseSettings, target.m_noiseSettings);
            }


            target.m_noiseToolSettings = new NoiseToolSettings();
            GaiaUtils.CopyFields(source.m_noiseToolSettings, target.m_noiseToolSettings);

            target.noiseSettingsGUI = null;

            target.m_collisionMasks = new CollisionMask[source.m_collisionMasks.Length];
            //Clone all collision masks as well
            for (int i = 0; i < target.m_collisionMasks.Length; i++)
            {
                target.m_collisionMasks[i] = new CollisionMask();
                GaiaUtils.CopyFields(source.m_collisionMasks[i], target.m_collisionMasks[i]);
            }



            //GaiaUtils.CopyFields(source.m_distanceMaskCurve, target.m_distanceMaskCurve); 
#endif
            return target;

        }

        /// <summary>
        /// Tries to refresh the collision mask layer bitmask selections according to the string array that was serialized with the other data.
        /// </summary>
        public void TryRefreshCollisionMask()
        {
            foreach (CollisionMask collisionMask in m_collisionMasks)
            {
                if (collisionMask.m_type == BakedMaskType.LayerGameObject || collisionMask.m_type == BakedMaskType.LayerTree)
                {
                    LayerMask refreshedMask = new LayerMask();
                    if (GaiaUtils.StringToLayerMask(collisionMask.m_layerMaskLayerNames, ref refreshedMask))
                    {
                        collisionMask.m_layerMask = refreshedMask;
                    }
                }
            }
        }
    }

}




