using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(GaiaStopwatchDataset))]
    public class GaiaStopwatchDatasetEditor : PWEditor, IPWEditor
    {
        private GaiaStopwatchDataset m_gaiastopwatchDataset;
        private EditorUtils m_editorUtils;
        private bool m_eventsActivated;
        private Func<GaiaStopWatchEvent, object> m_orderByLambda;
        [SerializeField]
        private bool[] m_unfoldedStates;
        [SerializeField]
        private GaiaStopwatchOrderBy m_orderBy = GaiaStopwatchOrderBy.TotalDuration;
        [SerializeField]
        private bool m_orderbyDescending = true;


        public void OnEnable()
        {
            m_gaiastopwatchDataset = (GaiaStopwatchDataset)target;
            m_unfoldedStates = new bool[m_gaiastopwatchDataset.m_events.Count];
            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        public override void OnInspectorGUI()
        {


            m_editorUtils.Initialize(); // Do not remove this!

            m_orderBy = (GaiaStopwatchOrderBy)m_editorUtils.EnumPopup("OrderBy", m_orderBy);
            m_orderbyDescending = m_editorUtils.Toggle("Descending", m_orderbyDescending);
            switch (m_orderBy)
            {
                case GaiaStopwatchOrderBy.FirstStart:
                    m_orderByLambda = x => x.m_firstStartTimeStamp;
                    break;
                case GaiaStopwatchOrderBy.Name:
                    m_orderByLambda = x => x.m_name;
                    break;
                case GaiaStopwatchOrderBy.TotalDuration:
                    m_orderByLambda = x => x.m_accumulatedTime;
                    break;
            }

            List<GaiaStopWatchEvent> relevantEvents;

            if (m_orderbyDescending)
            {
                relevantEvents = m_gaiastopwatchDataset.m_events.FindAll(x => x.m_parent == "" || x.m_parent ==null).OrderByDescending(m_orderByLambda).ToList();
            }
            else
            {
                relevantEvents = m_gaiastopwatchDataset.m_events.FindAll(x => x.m_parent == "" || x.m_parent == null).OrderBy(m_orderByLambda).ToList();
            }

            foreach (GaiaStopWatchEvent stopWatchEvent in relevantEvents)
            {
                DrawStopwatchEvent(stopWatchEvent);
            }


        }

        private void DrawStopwatchEvent(GaiaStopWatchEvent stopWatchEvent)
        {
            int id = m_gaiastopwatchDataset.m_events.FindIndex(x => x == stopWatchEvent);
            m_unfoldedStates[id] = m_editorUtils.Foldout(m_unfoldedStates[id], new GUIContent(stopWatchEvent.m_name));
            if (m_unfoldedStates[id])
            {
                EditorGUI.indentLevel++;
                m_editorUtils.LabelField("FirstStart", new GUIContent(stopWatchEvent.m_firstStartTimeStamp.ToString()));
                m_editorUtils.LabelField("TotalDuration", new GUIContent(stopWatchEvent.m_accumulatedTime.ToString()));
                m_editorUtils.LabelField("Calls", new GUIContent(stopWatchEvent.m_callCount.ToString()));
                m_editorUtils.LabelField("DurationPerCall", new GUIContent(stopWatchEvent.m_durationPerCall.ToString()));


                List<GaiaStopWatchEvent> relevantEvents;

                if (m_orderbyDescending)
                {
                    relevantEvents = m_gaiastopwatchDataset.m_events.FindAll(x => x.m_parent == stopWatchEvent.m_name).OrderByDescending(m_orderByLambda).ToList();
                }
                else
                {
                    relevantEvents = m_gaiastopwatchDataset.m_events.FindAll(x => x.m_parent == stopWatchEvent.m_name).OrderBy(m_orderByLambda).ToList();
                }

                foreach (GaiaStopWatchEvent subEvent in relevantEvents)
                {
                    DrawStopwatchEvent(subEvent);
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}