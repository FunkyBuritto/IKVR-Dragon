
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Gaia
{
    [System.Serializable]
    public class UndoRedoOperationSettings : ScriptableObject
    {
        #region public input variables
        /// <summary>
        /// List of all the Terrains that need to be loaded for this undo operation.
        /// </summary>
        public List<string> m_TerrainsList;
        #endregion
    }
}