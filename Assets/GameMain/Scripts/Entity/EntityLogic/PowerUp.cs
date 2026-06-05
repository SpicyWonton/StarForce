using System.Collections;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
	public class PowerUp: Entity
	{
        [SerializeField]
        private PowerUpData m_powerUpData = null;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            gameObject.SetLayerRecursively(Constant.Layer.PowerUpLayerId);
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_powerUpData = userData as PowerUpData;
            if (m_powerUpData ==  null)
            {
                Log.Error("PowerUp data is invalid.");
                return;
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            CachedTransform.Translate(Vector3.back * m_powerUpData.Speed * elapseSeconds, Space.World);
        }

        public void ApplyPowerUp()
        {
            switch (m_powerUpData.PowerUpType)
            {
                case PowerUpData.EPowerUpType.AttackSpeedUp:
                    GameEntry.Buff.SetAttackSpeedUp(m_powerUpData.Value, m_powerUpData.Duration);
                    break;
            }
        }
    }
}