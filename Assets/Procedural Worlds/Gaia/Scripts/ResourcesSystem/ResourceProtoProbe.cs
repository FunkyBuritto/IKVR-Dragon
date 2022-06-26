using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Gaia.GaiaConstants;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    public enum ProbeType { ReflectionProbe, LightProbe }

    /// <summary>
    /// Used to serialise the detail prototypes
    /// </summary>
    [System.Serializable]
    public class ResourceProtoProbe
    {
        [Tooltip("Resource name.")]
        public string m_name;
        [Tooltip("Probe Type - what kind of probe this is")]
        public ProbeType m_probeType = ProbeType.ReflectionProbe;
        [Tooltip("Holds settings for reflection probes.")]
        public ReflectionProbeData m_reflectionProbeData = new ReflectionProbeData(); 
   }
}