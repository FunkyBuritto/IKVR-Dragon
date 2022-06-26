using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    ///// <summary>
    ///// Priority definitions. The idea is to give the priority a human readable identification and control all priorities in a central location
    ///// instead of 100s of places within the application. The priority of these items is defined by their position in this enum, first entry = lowest priority, last entry = highest priority.
    ///// </summary>
    public enum ProgressBarPriority { TerrainLoading, MinMaxHeightCalculation, CreateBiomeTools, CreateSceneTools, BiomeRemoval, MultiTerrainAction, TerrainMeshExport, MaskMapExport, Stamping, Spawning, WorldCreation, GaiaGeospatial, Maintenance };

    //public class ProgressBarPriority
    //{
    //    public string m_name;
    //    public int m_priority;
    //}

    /// <summary>
    /// Static class to display an unified progress bar in Gaia. Allows to set a priority so that a progress bar for a longer, overarching process
    /// is not constantly interrupted by smaller processes, e.g. the world creation progress bar is not interrupted by progress bars for terrain loading.
    /// </summary>
    public static class ProgressBar
    {
#if UNITY_EDITOR
        /// <summary>
        /// The current priority of the last progress bar that was shown.
        /// </summary>
        public static int m_currentPriority =0;
        /// <summary>
        /// The average duration in milliseconds to perform one of the progress bar steps.
        /// </summary>
        static long m_averageStepDuration = 0;
        /// <summary>
        /// The timestamp when the last step for this progress bar was completed before.
        /// </summary>
        static long m_lastStepTimeStamp = 0;
#endif

        /// <summary>
        /// Shows a progress bar with a given priority.
        /// </summary>
        /// <param name="priorityName">An enum value that controls the priority of this progress bar. (See ProgressBar.cs for the definitions!)</param>
        /// <param name="title">The title for the progress bar window. Will be appended by step count and ETA if chosen.</param>
        /// <param name="info">The info text below the progress bar.</param>
        /// <param name="currentStep">The current step for the process that the progress bar represents. If the process can't be split into steps, put 0 in here.</param>
        /// <param name="totalSteps">The total amount of steps for the process that the progress bar represents. If the process can't be split into steps, put 0 in here.</param>
        /// <param name="displayETA">Whether you want an ETA (estimated time of arrival) to be displayed in the title</param>
        /// <param name="cancelable">Whether the progress bar should have a cancel button.</param>
        /// <returns>Returns true if the user clicked on the cancel button in the progress bar.</returns>
        public static bool Show(ProgressBarPriority priority, string title, string info, int currentStep = 0, int totalSteps = 0, bool displayETA = false, bool cancelable = false)
        {
#if UNITY_EDITOR
            int newPriority = 0;
            newPriority = (int)priority;

            //New priority needs to be higher as current one, otherwise we won't interrupt the current progress bar
            if (newPriority < m_currentPriority)
            {
                return false;
            }

            if (newPriority != m_currentPriority)
            {
                m_currentPriority = newPriority;
                //new priority? reset the variables for ETA calculation as well
                m_averageStepDuration = 0;
                m_lastStepTimeStamp = 0;
            }
            if (totalSteps > 0)
            {
                title = string.Format(title + " - Step {0} of {1}", currentStep.ToString(), totalSteps.ToString());
            }

            //ETA calculations
            if (m_lastStepTimeStamp > 0 && currentStep > 0)
            {
                m_averageStepDuration = (m_averageStepDuration * (currentStep -1) + (GaiaUtils.GetUnixTimestamp() - m_lastStepTimeStamp)) / (long)currentStep;
            }
            m_lastStepTimeStamp = GaiaUtils.GetUnixTimestamp();

            if (displayETA && currentStep >=1)
            {
                title += ", ETA: " + TimeSpan.FromMilliseconds(m_averageStepDuration * (totalSteps -currentStep)).ToString(@"hh\:mm\:ss"); 
            }

            //Progress calculations
            float progress = 0.5f;
            if (totalSteps > 0)
            {
                progress = (float)currentStep / (float)totalSteps;
            }

            if (cancelable)
            {
                if (EditorUtility.DisplayCancelableProgressBar(title, info, progress))
                {
                    //cancel was pressed, make sure the process bar is being cleared in any case.
                    m_currentPriority = 0;
                    m_averageStepDuration = 0;
                    m_lastStepTimeStamp = 0;
                    EditorUtility.ClearProgressBar();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                EditorUtility.DisplayProgressBar(title, info, progress);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Clears the current progress bar, if the given name is current priority or higher.
        /// </summary>
        public static void Clear(ProgressBarPriority priority)
        {
#if UNITY_EDITOR
            //Get the priority of the given name
            int newPriority = 0;
            newPriority = (int)priority;

            //Progress bar with lower priority may not clear the current one
            if (newPriority < m_currentPriority)
            {
                return;
            }

            m_currentPriority = 0;
            m_averageStepDuration = 0;
            m_lastStepTimeStamp = 0;
            EditorUtility.ClearProgressBar();
#endif
        }

    }
}