using System;
using System.Collections;
using GameFramework.DataTable;
using UnityEngine;

namespace StarForce
{
    [Serializable]
    public class PowerUpData : EntityData
    {
        public enum EPowerUpType : byte
        {
            Unknown = 0,
            AttackSpeedUp,
            MultiShot,
        }

        [SerializeField]
        private EPowerUpType m_PowerUpType = EPowerUpType.Unknown;

        [SerializeField]
        private float m_Duration = 0f;

        [SerializeField]
        private float m_Value = 0f;

        [SerializeField]
        private float m_Speed = 0f;

        [SerializeField]
        private int m_SoundId = 0;

        public PowerUpData(int entityId, int typeId) : base(entityId, typeId)
        {
            var tbPowerUp = GameEntry.DataTable.Tables.TbDTPowerUp;
            var drPowerUp = tbPowerUp.GetOrDefault(TypeId);
            if (drPowerUp == null)
            {
                return;
            }

            m_PowerUpType = (EPowerUpType)drPowerUp.PowerUpType;
            m_Duration = drPowerUp.Duration;
            m_Value = drPowerUp.Value;
            m_Speed = drPowerUp.Speed;
            m_SoundId = drPowerUp.SoundId;
        }

        public EPowerUpType PowerUpType => m_PowerUpType;

        public float Duration => m_Duration;

        public float Value => m_Value;

        public float Speed => m_Speed;

        public int SoundId => m_SoundId;
    }
}