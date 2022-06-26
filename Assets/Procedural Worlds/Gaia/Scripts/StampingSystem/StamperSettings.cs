// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;
using System;

/*
 * Scriptable Object containing settings for a stamper
 */

namespace Gaia
{
    /// <summary> Contains information about a Sequence of clips to play and how </summary>
    [CreateAssetMenu(menuName = "Procedural Worlds/Gaia/Stamper Settings")]
    [System.Serializable]
    public class StamperSettings : ScriptableObject, ISerializationCallbackReceiver
    {

        #region Public Variables

        /// <summary>
        /// Stamp x location - done this way to expose in the editor as a simple slider
        /// </summary>
        public double m_x = 0;

        /// <summary>
        /// Stamp y location - done this way to expose in the editor as a simple slider
        /// </summary>
        public double m_y = 50;

        /// <summary>
        /// Stamp z location - done this way to expose in the editor as a simple slider
        /// </summary>
        public double m_z = 0;

        /// <summary>
        /// Are these stamper settings folded out when viewed in an editor?
        /// </summary>
        public bool m_isFoldedOut;

        /// <summary>
        /// Stamp width - this is the horizontal scaling factor - applied to both x & z
        /// </summary>
        public float m_width = 10f;

        /// <summary>
        /// Stamp height - this is the vertical scaling factor
        /// </summary>
        public float m_height = 10f;

        /// <summary>
        /// The absolute added / subtracted height for add / subtract height operations
        /// </summary>
        public float m_absoluteHeightValue;

        /// <summary>
        /// Stamp rotation
        /// </summary>
        public float m_rotation = 0f;

        /// <summary>
        /// Is this stamper intended to be used on world map terrains?
        /// </summary>
        public bool m_isWorldmapStamper = false;

        /// <summary>
        /// The current operation that the stamper will perform on the terrain when pressing the stamp button
        /// </summary>
        public GaiaConstants.FeatureOperation m_operation = GaiaConstants.FeatureOperation.RaiseHeight;

        /// <summary>
        /// The ground / base level - value in 0..1 that determines where the base of the stamp is as a % pf overall stamp height. 
        /// Initially loaded from scanned value stored in the stamp, but can be overridden.
        /// </summary>
        public float m_baseLevel = 0f;

        /// <summary>
        /// Whether or not to draw any portion of the stamp below the base level of the stamp
        /// </summary>
        public bool m_drawStampBase = true;

        /// <summary>
        /// Should the base level adapt itself to the existing shape of the terrain?
        /// </summary>
        public bool m_adaptiveBase = false;

        /// <summary>
        /// size of the increased features when using Effects>Contrast
        /// </summary>
        public float m_contrastFeatureSize = 10;

        /// <summary>
        /// strength of the contrast effect when using Effects>Contrast
        /// </summary>
        public float m_contrastStrength = 2;

        /// <summary>
        /// size of the features being included in a terrace when using Effects>Terraces
        /// </summary>
        public float m_terraceCount = 100f;

        /// <summary>
        /// Added Jitter when using Effects>Terraces
        /// </summary>
        public float m_terraceJitterCount = 0.5f;

        /// <summary>
        /// Bevel Amount when using Effects>Terraces
        /// </summary>
        public float m_terraceBevelAmountInterior;

        /// <summary>
        /// Sharpness when using Effects>Sharpen Ridges
        /// </summary>
        public float m_sharpenRidgesMixStrength = 0.5f;

        /// <summary>
        /// Erosion Amount when using Effects>Sharpen Ridges
        /// </summary>
        public float m_sharpenRidgesIterations = 16f;

        public float m_powerOf;
        public float m_smoothVerticality = 0f;
        public float m_smoothBlurRadius = 10f;


        /// <summary>
        /// A fixed Image masked used as input for some of the operations.
        /// </summary>
        public ImageMask m_stamperInputImageMask = new ImageMask();

        /// <summary>
        /// The mix level of the stamp for the mix height operation.
        /// </summary>
        public float m_mixMidPoint = 0.5f;

        /// <summary>
        /// The strength of the mix height operation.
        /// </summary>
        public float m_mixHeightStrength = 0.5f;

        public GaiaConstants.AutoSpawnerArea m_autoSpawnerArea = GaiaConstants.AutoSpawnerArea.Local;
        public GaiaConstants.AutoSpawnerArea m_autoMaskExportArea = GaiaConstants.AutoSpawnerArea.Local;

        /// <summary>
        /// height transform curve when using Effects > Height Transform
        /// </summary>
        public AnimationCurve m_heightTransformCurve = ImageMask.NewAnimCurveStraightUpwards();


        #region Erosion Settings
        public float m_erosionSimScale = 0.5f;
        public float m_erosionHydroTimeDelta = 0.07f;
        public int m_erosionHydroIterations = 10;
        public float m_erosionThermalTimeDelta = 0.01f;
        public int m_erosionThermalIterations = 8;
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
        #endregion


        //<summary>All image filters that are being applied in this stamping process</summary>
        public ImageMask[] m_imageMasks = new ImageMask[0];
        

        /// <summary>
        /// Removes References to Texture2Ds in Image masks. The image mask will still remember the GUID of that texture to load it when needed.
        /// Call this when you are "done" with the stamper settings to free up memory caused by these references.
        /// </summary>
        public void ClearImageMaskTextures()
        {
            m_stamperInputImageMask.FreeTextureReferences();
            if (m_imageMasks != null)
            {
                foreach (ImageMask im in m_imageMasks)
                {
                    im.FreeTextureReferences();
                }
            }
            Resources.UnloadUnusedAssets();
        }

        private void OnDestroy()
        {
            ClearImageMaskTextures();
        }


        #endregion
        #region Serialization

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            //if (m_clipData != null && m_clipData.Length > 0)
            //{
            //    m_clipData = new ClipData[m_clips.Length];
            //    for (int i = 0; i < m_clips.Length; ++i)
            //    {
            //        m_clipData[i] = new ClipData(m_clips[i], 1f);
            //    }
            //    m_clips = null;
            //}
        }


        /// <summary>
        /// Returns the correct stamper scale for an add / subtract operation to add or subtract X meters from the terrain
        /// </summary>
        /// <param name="terrain">The terrain the stamp operation is being performed on</param>
        /// <param name="heightValueInMeter">The height value in meter that needs to be added or subtracted.</param>
        /// <returns></returns>
        public float GetStampScaleByMeter(Terrain terrain, float heightValueInMeter)
        {
            //exception for height = 0, we can't put 0 as y-scale on the stamper.
            if (heightValueInMeter == 0)
            {
                return 0.0000001f;
            }
            return Mathf.Lerp(0f, 50f, Mathf.InverseLerp(0f, terrain.terrainData.size.y, Mathf.Abs(heightValueInMeter)));
        }

        #endregion

    }
}
