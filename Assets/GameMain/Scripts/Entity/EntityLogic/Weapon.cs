//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 武器类。
    /// </summary>
    public class Weapon : Entity
    {
        private const string AttachPoint = "Weapon Point";

        [SerializeField]
        private WeaponData m_WeaponData = null;

        private float m_NextAttackTime = 0f;

#if UNITY_2017_3_OR_NEWER
        protected override void OnInit(object userData)
#else
        protected internal override void OnInit(object userData)
#endif
        {
            base.OnInit(userData);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnShow(object userData)
#else
        protected internal override void OnShow(object userData)
#endif
        {
            base.OnShow(userData);

            m_WeaponData = userData as WeaponData;
            if (m_WeaponData == null)
            {
                Log.Error("Weapon data is invalid.");
                return;
            }

            GameEntry.Entity.AttachEntity(Entity, m_WeaponData.OwnerId, AttachPoint);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
#else
        protected internal override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
#endif
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);

            Name = Utility.Text.Format("Weapon of {0}", parentEntity.Name);
            CachedTransform.localPosition = Vector3.zero;
        }

        public void TryAttack()
        {
            if (Time.time < m_NextAttackTime)
            {
                return;
            }

            m_NextAttackTime = Time.time + m_WeaponData.AttackInterval * GameEntry.Buff.AttackSpeedUpScale;

            const float bulletSpacing = 0.5f;
            int bulletCount = GameEntry.Buff.MultiShotCount;
            float centerOffset = (bulletCount - 1) * 0.5f;
            for (int i = 0; i < bulletCount; i++)
            {
                GameEntry.Entity.ShowBullet(new BulletData(GameEntry.Entity.GenerateSerialId(), m_WeaponData.BulletId, m_WeaponData.OwnerId, m_WeaponData.OwnerCamp, m_WeaponData.Attack, m_WeaponData.BulletSpeed)
                {
                    Position = CachedTransform.position + Vector3.right * ((i - centerOffset) * bulletSpacing),
                });
            }
            GameEntry.Sound.PlaySound(m_WeaponData.BulletSoundId);
        }
    }
}
