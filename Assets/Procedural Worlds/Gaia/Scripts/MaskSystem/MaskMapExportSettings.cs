// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;
using static Gaia.GaiaConstants;
using UnityEditor;


public enum MaskMapExportMultiTerrainOption { SeparateTextures, OneCombinedTexture }
/*
 * Scriptable Object containing mask map export settings 
 */

namespace Gaia
{
    [System.Serializable]
    public class MaskMapChannelSettings
    {
        public bool m_channelIsFoldedOut = true;
        public bool m_channelIsActive = true;
        public string m_channelName;
        public Color m_visualisationColor;
    }


    [System.Serializable]
    public class MaskMapExportSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        public MaskMapChannelSettings m_redChannelSettings = new MaskMapChannelSettings() { m_channelName = "Red Channel", m_visualisationColor = Color.red};
        public MaskMapChannelSettings m_greenChannelSettings = new MaskMapChannelSettings() { m_channelName = "Green Channel", m_visualisationColor = Color.green };
        public MaskMapChannelSettings m_blueChannelSettings = new MaskMapChannelSettings() { m_channelName = "Blue Channel", m_visualisationColor = Color.blue };
        public MaskMapChannelSettings m_alphaChannelSettings = new MaskMapChannelSettings() { m_channelName = "Alpha Channel", m_visualisationColor = Color.magenta };


        public float m_range = 1024;
        public string m_exportDirectory = "Assets/Gaia User Data/Gaia Exports";
        public string m_exportFileName = "MaskMap";
        public MaskMapExportMultiTerrainOption m_multiTerrainOption = MaskMapExportMultiTerrainOption.SeparateTextures;
        public int m_combinedTextureResolution = 2048;
        public bool m_addTerrainNameToFileName = true;
        public GaiaConstants.ImageFileType m_exportFileType = ImageFileType.Exr;
        public int m_exportJpgQuality = 75;
        public string lastSavePath;
        public float m_x;
        public float m_y;
        public float m_z;


        [SerializeField]
        private ImageMask[] red_imageMasks = new ImageMask[0];

        //Using a property to make sure the image mask list is always initialized
        //<summary>All image filters that are being applied in this spawning process</summary>

        public ImageMask[] m_red_imageMasks
        {
            get
            {
                if (red_imageMasks == null)
                {
                    red_imageMasks = new ImageMask[0];
                }
                return red_imageMasks;
            }
            set
            {
                red_imageMasks = value;
            }
        }


        [SerializeField]
        private ImageMask[] green_imageMasks = new ImageMask[0];

        //Using a property to make sure the image mask list is always initialized
        //<summary>All image filters that are being applied in this spawning process</summary>

        public ImageMask[] m_green_imageMasks
        {
            get
            {
                if (green_imageMasks == null)
                {
                    green_imageMasks = new ImageMask[0];
                }
                return green_imageMasks;
            }
            set
            {
                green_imageMasks = value;
            }
        }

        [SerializeField]
        private ImageMask[] blue_imageMasks = new ImageMask[0];

        //Using a property to make sure the image mask list is always initialized
        //<summary>All image filters that are being applied in this spawning process</summary>

        public ImageMask[] m_blue_imageMasks
        {
            get
            {
                if (blue_imageMasks == null)
                {
                    blue_imageMasks = new ImageMask[0];
                }
                return blue_imageMasks;
            }
            set
            {
                blue_imageMasks = value;
            }
        }


        [SerializeField]
        private ImageMask[] alpha_imageMasks = new ImageMask[0];


        public bool m_readWriteEnabled = true;

        //Using a property to make sure the image mask list is always initialized
        //<summary>All image filters that are being applied in this spawning process</summary>

        public ImageMask[] m_alpha_imageMasks
        {
            get
            {
                if (alpha_imageMasks == null)
                {
                    alpha_imageMasks = new ImageMask[0];
                }
                return alpha_imageMasks;
            }
            set
            {
                alpha_imageMasks = value;
            }
        }

        /// <summary>
        /// Removes References to Texture2Ds in Image masks. The image mask will still remember the GUID of that texture to load it when needed.
        /// Call this when you are "done" with the mask map export settings to free up memory caused by these references.
        /// </summary>
        public void ClearImageMaskTextures()
        {
            foreach (ImageMask im in m_red_imageMasks)
            {
                im.FreeTextureReferences();
            }
            foreach (ImageMask im in m_green_imageMasks)
            {
                im.FreeTextureReferences();
            }
            foreach (ImageMask im in m_blue_imageMasks)
            {
                im.FreeTextureReferences();
            }
            foreach (ImageMask im in m_alpha_imageMasks)
            {
                im.FreeTextureReferences();
            }

            Resources.UnloadUnusedAssets();
        }


        public void OnAfterDeserialize()
        {
    
        }

        public void OnBeforeSerialize()
        {
   
        }
    }
}
