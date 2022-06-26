using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    public enum DeletionTaskType { File, Directory}
    public enum MaintenanceCheckType { Contains, Equals }

    [System.Serializable]
    public class DeletionTask
    {
        public string m_pathContains = "";
        public DeletionTaskType m_taskType = DeletionTaskType.File;
        public bool m_includeSubDirectories = true;
        public MaintenanceCheckType m_checkType = MaintenanceCheckType.Contains;
        public string m_Name = "";
        public string m_fileExtension = "";
    }


    [System.Serializable]
    public class RenameTask
    {
        public string m_oldPath = "";
        public string m_newPath = "";
    }



    public class GaiaMaintenanceProfile : ScriptableObject
    {
        public List<DeletionTask> m_deletionTasks = new List<DeletionTask>();
        public List<RenameTask> m_renameTasks = new List<RenameTask>();
        public string[] meshColliderPrefabPaths;
    }

}
