#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace Gaia
{
    [ExecuteInEditMode]
    public class UIConfiguration : MonoBehaviour
    {
        #region Variables
        [Header("UI Settings")]
        [Tooltip("Sets the UI text color")]
        public Color32 m_uiTextColor = new Color32(255, 255, 255, 255);
        [Tooltip("Button used to toggle the UI on and off")]
        public KeyCode m_uiToggleButton = KeyCode.U;
        private Text m_textContent;

        private Color32 storedColor;
        private bool storedUIStatus = true;
        #endregion

        #region UI Text Setup
        /// <summary>
        /// Starting function setup
        /// </summary>
        void Start()
        {
            storedColor = m_uiTextColor;

            if (m_textContent != null)
            {
                m_textContent.color = storedColor;

#if UNITY_EDITOR
                if (!m_textContent.text.Contains("Open the Location Manager"))
                {
                    string locationProfilePath = GaiaUtils.GetAssetPath("Location Profile.asset");
                    if (!string.IsNullOrEmpty(locationProfilePath))
                    {
                        LocationSystemScriptableObject locationProfile = AssetDatabase.LoadAssetAtPath<LocationSystemScriptableObject>(locationProfilePath);
                        if (locationProfile != null)
                        {
                            m_textContent.text += "\r\n\r\nOpen the Location Manager from the Advanced Tab in the Gaia Manager to bookmark interesting locations:\r\n";
                            m_textContent.text += string.Format("{0} + {1} (new bookmark), {0} + {2} / {3} (cycle bookmarks)", locationProfile.m_mainKey.ToString(), locationProfile.m_addBookmarkKey.ToString(), locationProfile.m_prevBookmark.ToString(), locationProfile.m_nextBookmark.ToString());
                            m_textContent.text += "\r\n\r\n";
                        }
                    }
                }
#endif
            }
        }

        void OnEnable()
        {
            if (m_textContent == null)
            {
                GameObject controlTextGO = GameObject.Find("Control Text");
                if (controlTextGO != null)
                {
                    m_textContent = controlTextGO.GetComponent<Text>();
                }
            }
        }

        void LateUpdate()
        {
            storedColor = m_uiTextColor;

            if (storedUIStatus && Input.GetKeyDown(m_uiToggleButton))
            {
                m_textContent.enabled = false;
                storedUIStatus = false;
            }

            else if (!storedUIStatus && Input.GetKeyDown(m_uiToggleButton))
            {
                m_textContent.enabled = true;
                storedUIStatus = true;
            }


            if (m_textContent != null)
            {
                if (m_textContent.color != storedColor)
                {
                    m_textContent.color = storedColor;
                }
            }

        }
        #endregion
    }
}