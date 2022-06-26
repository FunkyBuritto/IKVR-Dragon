using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PWCommon4;
using Gaia.Internal;

namespace Gaia
{
    [CustomEditor(typeof(GaiaUnderwaterEffects))]
    public class GaiaUnderwaterEffectsEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private GaiaUnderwaterEffects m_profile;
        private GaiaConstants.EnvironmentRenderer m_renderPipeline;

        private void OnEnable()
        {
            //Get Profile object
            m_profile = (GaiaUnderwaterEffects)target;
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            m_renderPipeline = GaiaUtils.GetActivePipeline();
        }

        /// <summary>
        /// Setup on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            EditorGUI.BeginChangeCheck();

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);
            }
        }

        private void GlobalSettings(bool helpEnabled)
        {
            m_editorUtils.Heading("GlobalSettings");
            m_profile.EnableUnderwaterEffects = m_editorUtils.Toggle("EnableUnderwaterEffects", m_profile.EnableUnderwaterEffects, helpEnabled);
            m_profile.m_seaLevel = m_editorUtils.FloatField("SeaLevel", m_profile.m_seaLevel, helpEnabled);
            m_profile.m_playerCamera = (Transform)m_editorUtils.ObjectField("PlayerCamera", m_profile.m_playerCamera, typeof(Transform), helpEnabled);
            m_profile.m_startingUnderwater = m_editorUtils.Toggle("StartingUnderwater", m_profile.m_startingUnderwater, helpEnabled);
            bool transitionEnabled = m_profile.m_enableTransitionFX;
            transitionEnabled = m_editorUtils.Toggle("EnableTransitionFX", transitionEnabled, helpEnabled);
            if (m_profile.m_enableTransitionFX != transitionEnabled)
            {
                m_profile.m_enableTransitionFX = transitionEnabled;
                m_profile.SetupTransitionVFX( m_profile.m_enableTransitionFX);
            }
            EditorGUILayout.Space();

            m_editorUtils.Heading("CausticSettings");
            m_profile.m_useCaustics = m_editorUtils.Toggle("UseCaustics", m_profile.m_useCaustics, helpEnabled);
            if (m_profile.m_useCaustics)
            {
                m_profile.m_mainLight = (Light)m_editorUtils.ObjectField("MainLight", m_profile.m_mainLight, typeof(Light), helpEnabled);
                m_profile.m_framesPerSecond = m_editorUtils.IntField("FramesPerSecond", m_profile.m_framesPerSecond, helpEnabled);
                m_profile.m_causticSize = m_editorUtils.Slider("CausticSize", m_profile.m_causticSize, 1f, 100f, helpEnabled);
                m_profile.m_editCausticTextures = m_editorUtils.Toggle("EditCausticTextures", m_profile.m_editCausticTextures, helpEnabled);
                if (m_profile.m_editCausticTextures)
                {
                    for (int i = 0; i < m_profile.m_causticTextures.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        m_profile.m_causticTextures[i] = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("[" + i + "]" + m_editorUtils.GetTextValue("CausticTexture"), m_editorUtils.GetTooltip("CausticTexture")), m_profile.m_causticTextures[i], typeof(Texture2D), false, GUILayout.MaxHeight(16f));
                        if (m_editorUtils.Button("Remove", GUILayout.MaxWidth(30f)))
                        {
                            m_profile.m_causticTextures.RemoveAt(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (m_editorUtils.Button("Add"))
                    {
                        m_profile.m_causticTextures.Add(null);
                    }
                }
            }
            EditorGUILayout.Space();

            m_editorUtils.Heading("FogSettings");
            m_profile.m_supportFog = m_editorUtils.Toggle("SupportFog", m_profile.m_supportFog, helpEnabled);
            if (m_profile.m_supportFog)
            {
                m_profile.m_fogColorGradient = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("FogColor"), m_editorUtils.GetTooltip("FogColor")), m_profile.m_fogColorGradient);
                m_editorUtils.InlineHelp("FogColor", helpEnabled);
                if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    if (RenderSettings.fogMode == FogMode.Linear)
                    {
                        m_profile.m_nearFogDistance = m_editorUtils.FloatField("FogStartDistance", m_profile.m_nearFogDistance, helpEnabled);
                        m_profile.m_fogDistance = m_editorUtils.FloatField("FogEndDistance", m_profile.m_fogDistance, helpEnabled);
                    }
                    else
                    {
                        m_profile.m_fogDensity = m_editorUtils.Slider("FogDensity", m_profile.m_fogDensity, 0f, 1f, helpEnabled);
                    }
                }
                else
                {
                    m_profile.m_fogDistance = m_editorUtils.FloatField("FogEndDistance", m_profile.m_fogDistance, helpEnabled);
                }
            }
            EditorGUILayout.Space();

            m_editorUtils.Heading("AudioSettings");
            m_profile.m_playbackVolume = m_editorUtils.Slider("PlaybackVolume", m_profile.m_playbackVolume, 0f, 1f, helpEnabled);
            m_profile.m_submergeSoundFXDown = (AudioClip)m_editorUtils.ObjectField("SubmergeSoundFXDown", m_profile.m_submergeSoundFXDown, typeof(AudioClip), helpEnabled);
            m_profile.m_submergeSoundFXUp = (AudioClip)m_editorUtils.ObjectField("SubmergeSoundFXUp", m_profile.m_submergeSoundFXUp, typeof(AudioClip), helpEnabled);
            m_profile.m_underwaterSoundFX = (AudioClip)m_editorUtils.ObjectField("UnderwaterSoundFX", m_profile.m_underwaterSoundFX, typeof(AudioClip), helpEnabled);
        }
    }
}