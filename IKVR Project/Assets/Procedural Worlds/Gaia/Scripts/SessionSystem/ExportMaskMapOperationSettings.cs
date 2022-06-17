
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Gaia
{
    [System.Serializable]
    public class ExportMaskMapOperationSettings : ScriptableObject
    {
        #region public input variables
        /// <summary>
        /// Holds the mask map exporter settings used for the export op.
        /// </summary>
        public MaskMapExportSettings m_maskMapExportSettings;
        /// <summary>
        /// The terrain names which were exported from.
        /// </summary>
        public List<string> m_terrainNames = new List<string>();
        /// <summary>
        /// Whether the export op was global or not. Global means the export will be done per each terrain, local means the epxort will be performed with a fixed range in a fixed location.
        /// </summary>
        public bool isGlobalExport = false;
        #endregion
    }
}