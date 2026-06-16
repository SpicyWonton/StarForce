using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public partial class MyAssetComponent : GameFrameworkComponent
    {
        [SerializeField]
        private bool m_AutoInitialize = true;

        [SerializeField]
        private int m_MaxConcurrentLoadTaskCount = 3;

        private readonly Dictionary<string, MyAssetRecord> m_AssetMap = new Dictionary<string, MyAssetRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MyBundleRecord> m_BundleMap = new Dictionary<string, MyBundleRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LoadedAssetInfo> m_LoadedAssets = new Dictionary<string, LoadedAssetInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LoadingAssetInfo> m_LoadingAssets = new Dictionary<string, LoadingAssetInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LoadedBundleInfo> m_LoadedBundles = new Dictionary<string, LoadedBundleInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LoadingBundleInfo> m_LoadingBundles = new Dictionary<string, LoadingBundleInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly List<MyAssetLoadTask> m_WaitingTasks = new List<MyAssetLoadTask>();

        private bool m_Initialized;
        private int m_RunningLoadTaskCount;
        private long m_NextTaskSerialId;

        public bool IsInitialized => m_Initialized;

        private void Start()
        {
            if (m_AutoInitialize)
            {
                Initialize();
            }
        }

        public bool Initialize()
        {
            if (m_Initialized)
            {
                return true;
            }

            string versionPath = Path.Combine(Application.streamingAssetsPath, "MyAssets", MyAssetPaths.VersionFileName);
            if (!File.Exists(versionPath))
            {
                Log.Error("MyAsset version file not found: '{0}'.", versionPath);
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(versionPath);
            }
            catch (Exception exception)
            {
                Log.Error("Failed to read MyAsset version file: '{0}', error: {1}", versionPath, exception.Message);
                return false;
            }

            MyAssetVersion version = JsonUtility.FromJson<MyAssetVersion>(json);
            if (version == null)
            {
                Log.Error("Failed to parse MyAsset version file: '{0}'.", versionPath);
                return false;
            }

            m_AssetMap.Clear();
            m_BundleMap.Clear();

            if (version.assets != null)
            {
                foreach (MyAssetRecord assetRecord in version.assets)
                {
                    if (assetRecord == null || string.IsNullOrEmpty(assetRecord.assetPath))
                    {
                        continue;
                    }

                    m_AssetMap[assetRecord.assetPath] = assetRecord;
                }
            }

            if (version.bundles != null)
            {
                foreach (MyBundleRecord bundleRecord in version.bundles)
                {
                    if (bundleRecord == null || string.IsNullOrEmpty(bundleRecord.bundleName))
                    {
                        continue;
                    }

                    m_BundleMap[bundleRecord.bundleName] = bundleRecord;
                }
            }

            m_Initialized = true;
            Log.Info("MyAsset initialized. Assets: {0}, Bundles: {1}.", m_AssetMap.Count, m_BundleMap.Count);
            return true;
        }

        public MyAssetLoadHandle LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            return LoadAssetAsync<T>(assetPath, 0);
        }

        public MyAssetLoadHandle LoadAssetAsync<T>(string assetPath, int priority) where T : UnityEngine.Object
        {
            MyAssetLoadHandle handle = new MyAssetLoadHandle(this, assetPath);
            EnqueueLoadTask(new MyAssetLoadTask
            {
                Priority = priority,
                SerialId = m_NextTaskSerialId++,
                Handle = handle,
                Routine = LoadAssetCoroutine<T>(assetPath, handle)
            });
            return handle;
        }
    }
}
