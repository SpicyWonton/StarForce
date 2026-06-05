using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public class BuffComponent : GameFrameworkComponent
    {
        // 射速提升buff
        private float m_AttackSpeedUpScale = 1f;
        private float m_AttackSpeedUpDuration = 0f;

        public float AttackSpeedUpScale => m_AttackSpeedUpScale;

        private void Start()
        {
        }

        private void OnDestroy()
        {
        }

        private void Update()
        {
            if (m_AttackSpeedUpDuration > 0f)
            {
                m_AttackSpeedUpDuration -= Time.deltaTime;
                if (m_AttackSpeedUpDuration <= 0f)
                {
                    m_AttackSpeedUpScale = 1f;
                }
            }
        }

        public void SetAttackSpeedUp(float scale, float duration)
        {
            m_AttackSpeedUpScale = scale;
            m_AttackSpeedUpDuration = duration;
        }
    }
}