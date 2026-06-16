using System;

namespace StarForce
{
    public sealed class MyAssetLoadHandle
    {
        private readonly MyAssetComponent m_Component;
        private readonly string m_AssetPath;
        private Action m_Completed;
        private bool m_IsLoaded;
        private bool m_IsReleased;

        public MyAssetLoadHandle(MyAssetComponent component, string assetPath)
        {
            m_Component = component;
            m_AssetPath = assetPath;
        }

        public string AssetPath => m_AssetPath;

        public bool IsDone
        {
            get;
            private set;
        }

        public bool IsSuccess
        {
            get;
            private set;
        }

        public bool IsReleased => m_IsReleased;

        public UnityEngine.Object Asset
        {
            get;
            private set;
        }

        public string ErrorMessage
        {
            get;
            private set;
        }

        public event Action Completed
        {
            add
            {
                if (IsDone)
                {
                    value();
                    return;
                }

                m_Completed += value;
            }
            remove
            {
                m_Completed -= value;
            }
        }

        public void Release()
        {
            if (m_IsReleased)
            {
                return;
            }

            m_IsReleased = true;
            if (m_IsLoaded)
            {
                ReleaseInternal();
            }
        }

        internal void Complete(bool isSuccess, UnityEngine.Object asset, string errorMessage)
        {
            IsDone = true;
            IsSuccess = isSuccess;
            Asset = asset;
            ErrorMessage = errorMessage;
            m_IsLoaded = isSuccess;

            Action completed = m_Completed;
            m_Completed = null;
            if (completed != null)
            {
                completed();
            }

            if (m_IsReleased && m_IsLoaded)
            {
                ReleaseInternal();
            }
        }

        private void ReleaseInternal()
        {
            m_IsLoaded = false;
            if (m_Component != null)
            {
                m_Component.UnloadAsset(m_AssetPath);
            }
        }
    }
}
