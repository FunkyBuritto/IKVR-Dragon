// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;

/*
 * Scriptable Object containing settings for a world map
 */

namespace Gaia
{
    [System.Serializable]
    public class WorldMapSettings : ScriptableObject, ISerializationCallbackReceiver
    {

        #region Public Variables

        /// <summary>
        /// Stamp x location - done this way to expose in the editor as a simple slider
        /// </summary>
        public float m_x = 0f;

        /// <summary>
        /// Stamp y location - done this way to expose in the editor as a simple slider
        /// </summary>
        public float m_y = 50f;

        /// <summary>
        /// Stamp z location - done this way to expose in the editor as a simple slider
        /// </summary>
        public float m_z = 0f;
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

        #endregion

    }
}
