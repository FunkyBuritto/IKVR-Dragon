
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Gaia
{
    [System.Serializable]
    public class FlattenOperationSettings : ScriptableObject
    {
        #region public input variables
        /// <summary>
        /// List of all the Terrains that need to be flattened.
        /// </summary>
        public List<string> m_TerrainsList;
        #endregion


       


    }
}