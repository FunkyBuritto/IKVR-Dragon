using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    public class FollowPlayerSystem : MonoBehaviour
    {
        [Header("Global Settings")]
        public bool m_followPlayer = true;
        public bool m_isWaterObject = false;
        public List<GameObject> m_particleObjects = new List<GameObject>();
        public Transform m_player;

        public bool m_useOffset = false;
        public float m_xoffset = 1250f;
        public float m_zoffset = 300f;
        public float m_yOffset = 200f;

        public bool m_useScale = false;
        public Vector3 m_scaleSize = new Vector3(1f, 1f, 1f);

        private void Start()
        {
            if (m_player == null)
            {
                if (GaiaUtils.GetCamera() != null)
                {
                    m_player = GaiaUtils.GetCamera().transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (m_followPlayer)
            {
                if (m_player)
                {
                    if (m_particleObjects != null)
                    {
                        foreach (GameObject particleObject in m_particleObjects)
                        {
                            if (!m_useOffset)
                            {
                                particleObject.transform.position = m_player.position;
                            }
                            else
                            {
                                if (m_isWaterObject)
                                {
                                    if (m_player.position.y < 1f)
                                    {
                                        particleObject.transform.position = new Vector3(m_player.position.x + m_xoffset, m_player.position.y + 70f - m_yOffset, m_player.position.z - m_zoffset);
                                    }
                                    else
                                    {
                                        particleObject.transform.position = new Vector3(m_player.position.x + m_xoffset, m_player.position.y + 10 - m_yOffset, m_player.position.z - m_zoffset);
                                    }
                                }
                                else
                                {
                                    particleObject.transform.position = new Vector3(m_player.position.x + m_xoffset, m_player.position.y - m_yOffset, m_player.position.z - m_zoffset);
                                }
                            }
                        }
                    }
                }
            }

            if (m_useScale)
            {
                gameObject.transform.localScale = m_scaleSize;
            }
        }
    }
}