using UnityGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 加成道具表
    /// </summary>
    public class DRPowerUp : DataRowBase
    {
        private int m_Id = 0;

        public override int Id
        {
            get
            {
                return m_Id;
            }
        }

        /// <summary>
        /// 加成类型
        /// </summary>
        public int PowerUpType
        {
            get;
            private set;
        }

        /// <summary>
        /// 持续时间
        /// </summary>
        public float Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// 加成值
        /// </summary>
        public float Value
        {
            get;
            private set;
        }

        /// <summary>
        /// 速度
        /// </summary>
        public float Speed
        {
            get;
            private set;
        }

        /// <summary>
        /// 声音编号
        /// </summary>
        public int SoundId
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            PowerUpType = int.Parse(columnStrings[index++]);
            Duration = float.Parse(columnStrings[index++]);
            Value = float.Parse(columnStrings[index++]);
            Speed = float.Parse(columnStrings[index++]);
            SoundId = int.Parse(columnStrings[index++]);

            return true;
        }
    }
}