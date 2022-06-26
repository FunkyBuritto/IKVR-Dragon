using System;

namespace Gaia.ShaderUtilities
{
    public enum ShaderIDs
    {
        Null,
        PW_General_Forward,
        PW_General_Deferred
    }
    
    public static class PWShaderNameUtility
    { 
        public static String[] ShaderName =
       {
           "",
           "PWS/PW_General_Forward",
           "PWS/PW_General_Deferred"
       };
    }
}


