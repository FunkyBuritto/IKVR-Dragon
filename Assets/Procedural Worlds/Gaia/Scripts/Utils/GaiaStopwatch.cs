using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Gaia
{
    
    /// <summary>
    /// Data structure to collect data about specific events that are called repeatedly while the stopwatch is running.
    /// </summary>
    [System.Serializable]
    public class GaiaStopWatchEvent
    {
        public string m_name;
        public string m_parent;
        public bool m_started;
        public int m_callCount;
        public long m_firstStartTimeStamp;
        public long m_lastStartTimeStamp;
        public long m_lastStopTimeStamp;
        public long m_durationPerCall;
        public long m_accumulatedTime;
    }


    //Simple static wrapper to access one Stopwatch instance across all Gaia
    //Can be used to measure time across multiple parts of the application
    //without having to deal with passing stopwatch instances around 
    public static class GaiaStopwatch
    {
#if GAIA_DEBUG
        public static Stopwatch m_stopwatch = new Stopwatch();
        public static GaiaStopWatchEvent m_currentEvent;
        static long m_lastLogElapsed = 0;
        public static bool m_isEnabled;
        public static long m_accumulatedYieldTime = 0;
        static List<GaiaStopWatchEvent> m_events = new List<GaiaStopWatchEvent>();
#endif

        private static void Start()
        {
#if GAIA_DEBUG
            //Always start a new list of events to not overwrite previous data
            m_events = new List<GaiaStopWatchEvent>();
            m_stopwatch.Reset();
            m_stopwatch.Start();
            LogWithTime("Starting a new Gaia Stopwatch Log. All logged times in milliseconds.");
#endif
        }

        public static void LogWithTime(string logText)
        {
#if GAIA_DEBUG
            UnityEngine.Debug.Log(String.Format("{0:N0}",m_stopwatch.ElapsedMilliseconds.ToString()) + " | Diff: " + String.Format("{0:N0}", (m_stopwatch.ElapsedMilliseconds - m_lastLogElapsed).ToString()) + " | " + logText);
            m_lastLogElapsed = m_stopwatch.ElapsedMilliseconds;
#endif
        }

        public static void StartEvent(string name)
        {
#if GAIA_DEBUG
            //Stopwatch not running yet? If enabled, we begin a new log now.
            if (!m_stopwatch.IsRunning)
            {
                if (m_isEnabled)
                {
                    Start();
                }
                else
                {
                    return;
                }
            }
            
            GaiaStopWatchEvent stopWatchEvent = m_events.Find(x => x.m_name == name);

            if (stopWatchEvent != null)
            {
                if (!stopWatchEvent.m_started)
                {
                    stopWatchEvent.m_lastStartTimeStamp = m_stopwatch.ElapsedMilliseconds;
                    stopWatchEvent.m_started = true;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Trying to start an event '" + name + "' with the Gaia Stopwatch that has already been started before!");
                }
            }
            else
            {
                //Event does not exist yet, let's create it
                stopWatchEvent = new GaiaStopWatchEvent()
                {
                    m_firstStartTimeStamp = m_stopwatch.ElapsedMilliseconds,
                    m_lastStartTimeStamp = m_stopwatch.ElapsedMilliseconds,
                    m_name = name,
                    m_started = true,
                };

                m_events.Add(stopWatchEvent);
            }
            LogWithTime("Start of Event: " + name + " (" + stopWatchEvent.m_callCount + ")");
            //assign parent if current Event not is null, otherwise this will become the first current event
            if (m_currentEvent!=null)
            {
                stopWatchEvent.m_parent = m_currentEvent.m_name;
            }
            m_currentEvent = stopWatchEvent;
#endif
        }

        public static void EndEvent(string name, bool warning =true)
        {
#if GAIA_DEBUG
            if (!m_stopwatch.IsRunning)
            {
                return;
            }

            GaiaStopWatchEvent stopWatchEvent = m_events.Find(x => x.m_name == name && x.m_started==true);

            if (stopWatchEvent != null)
            {
                stopWatchEvent.m_lastStopTimeStamp = m_stopwatch.ElapsedMilliseconds;
                stopWatchEvent.m_started = false;
                stopWatchEvent.m_callCount++;
                stopWatchEvent.m_accumulatedTime += stopWatchEvent.m_lastStopTimeStamp - stopWatchEvent.m_lastStartTimeStamp;
                stopWatchEvent.m_durationPerCall = stopWatchEvent.m_accumulatedTime / stopWatchEvent.m_callCount;
            }
            else
            {
                if (warning)
                {
                    UnityEngine.Debug.LogWarning("Trying to stop an event '" + name + "' with the Gaia Stopwatch, but that event does not exist or is not running!");
                }
                return;
            }

            //End all child events that are still running
            var runningChildEvents = m_events.FindAll(x => x.m_parent == stopWatchEvent.m_name && x.m_started == true);
            if (runningChildEvents != null)
            {
                foreach (GaiaStopWatchEvent runningChildEvent in m_events.FindAll(x => x.m_parent == stopWatchEvent.m_name && x.m_started == true))
                {
                    EndEvent(runningChildEvent.m_name);
                }
            }

            //Return current event to parent, if any
            if (stopWatchEvent.m_parent != null)
            {
                m_currentEvent = m_events.Find(x => x.m_name == stopWatchEvent.m_parent);
            }
            LogWithTime("End of Event: " + name + " (" + stopWatchEvent.m_callCount + "), Total time accumulated for this Event: " + String.Format("{0:N0}",stopWatchEvent.m_accumulatedTime));
#endif
        }

        public static void Stop(bool outputData = true)
        {
#if GAIA_DEBUG
            if (!m_stopwatch.IsRunning || !m_isEnabled)
            {
                return;
            }

            //end any running events
            foreach (GaiaStopWatchEvent stopWatchEvent in m_events.FindAll(x=>x.m_started==true))
            {
                EndEvent(stopWatchEvent.m_name);
            }
            m_currentEvent = null;
            m_lastLogElapsed = 0;
            m_stopwatch.Stop();
            m_isEnabled = false;
            LogWithTime("Stopping the Gaia Stopwatch Log.");
            if (outputData)
            {
                GameObject parentGO = GaiaUtils.GetStopwatchDataObject();
                GameObject stopWatchDataObject = new GameObject(string.Format("Gaia Stopwatch Run {0:yyyy-MM-dd--HH-mm-ss}", DateTime.Now));
                stopWatchDataObject.transform.parent = parentGO.transform;
                GaiaStopwatchDataset newDataset = stopWatchDataObject.AddComponent<GaiaStopwatchDataset>();
                newDataset.m_events = m_events;
            }
#endif

        }
    }
}