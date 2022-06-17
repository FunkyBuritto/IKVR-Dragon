using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Gaia
{
    public static class SpawnProgressBar
    {
        static string m_title;
        static string m_subTitle;
        static float m_totalProgress;
        static float m_currentSpawnRuleProgress;
        static int m_totalSpawnRulesCompleted;
        static int m_totalSpawnRuleCount;
        static int m_spawnerRulesCompleted;
        static int m_spawnerRuleCount;

        public static bool UpdateProgressBar(string spawnerName, int totalSpawnRuleCount, int totalSpawnRulesCompleted, int spawnerRuleCount, int spawnerRulesCompleted)
        {
            //m_title = string.Format("Spawning: {0} of {1} Rules Total", totalSpawnRulesCompleted.ToString(), m_totalSpawnRuleCount.ToString());
            m_title = "Spawning";
            m_subTitle = string.Format(spawnerName + " {0} of {1} Rules", spawnerRulesCompleted.ToString(), spawnerRuleCount.ToString());
            m_totalSpawnRulesCompleted = totalSpawnRulesCompleted;
            m_totalSpawnRuleCount = totalSpawnRuleCount;
            m_spawnerRulesCompleted = spawnerRulesCompleted;
            m_spawnerRuleCount = spawnerRuleCount;
            m_currentSpawnRuleProgress = 0;

            return ProgressBar.Show(ProgressBarPriority.Spawning, m_title, m_subTitle, m_totalSpawnRulesCompleted, m_totalSpawnRuleCount,true,true);
        }

        public static bool UpdateSpawnRuleProgress(float progress)
        {
            m_currentSpawnRuleProgress = progress;
#if UNITY_EDITOR
            return ProgressBar.Show(ProgressBarPriority.Spawning, m_title, m_subTitle, m_totalSpawnRulesCompleted, m_totalSpawnRuleCount, true, true);
#else
            return false;
#endif
        }

        public static void ClearProgressBar()
        {
            ProgressBar.Clear(ProgressBarPriority.Spawning);
        }

    private static float CalculateProgress()
        {
            return Mathf.InverseLerp(0, m_totalSpawnRuleCount, (float)m_totalSpawnRulesCompleted + m_currentSpawnRuleProgress);
        }
    }
}
