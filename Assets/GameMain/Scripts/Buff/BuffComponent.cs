using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public class BuffComponent : GameFrameworkComponent
    {
        // 射速提升buff
        private float m_AttackSpeedUpScale = 1f;
        private float m_AttackSpeedUpDuration = 0f;

        // 多重射击buff
        private int m_MultiShotCount = 1;
        private float m_MultiShotDuration = 0f;

        public float AttackSpeedUpScale => m_AttackSpeedUpScale;
        public int MultiShotCount => m_MultiShotCount;

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

            if (m_MultiShotDuration > 0f)
            {
                m_MultiShotDuration -= Time.deltaTime;
                if (m_MultiShotDuration <= 0f)
                {
                    m_MultiShotCount = 1;
                }
            }
        }

        public void SetAttackSpeedUp(float scale, float duration)
        {
            m_AttackSpeedUpScale = scale;
            m_AttackSpeedUpDuration = duration;
        }

        public void SetMultiShot(int count, float duration)
        {
            m_MultiShotCount = count;
            m_MultiShotDuration = duration;
        }
    }
}