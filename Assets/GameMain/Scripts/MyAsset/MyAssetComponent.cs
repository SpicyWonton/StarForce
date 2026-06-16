using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public class MyAssetComponent : GameFrameworkComponent
    {
        [SerializeField]
        private bool m_AutoInitialize = true;

        private readonly Dictionary<string, MyAssetRecord> m_AssetMap = new Dictionary<string, MyAssetRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MyBundleRecord> m_BundleMap = new Dictionary<string, MyBundleRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LoadedBundleInfo> m_LoadedBundles = new Dictionary<string, LoadedBundleInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LoadingBundleInfo> m_LoadingBundles = new Dictionary<string, LoadingBundleInfo>(StringComparer.OrdinalIgnoreCase);

        private bool m_Initialized;

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

        public void LoadAssetAsync<T>(string assetPath, Action<T> onCompleted) where T : UnityEngine.Object
        {
            StartCoroutine(LoadAssetCoroutine(assetPath, onCompleted));
        }

        public void UnloadBundle(string bundleName, bool unloadLoadedObjects = false)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                return;
            }

            LoadedBundleInfo loadedBundle;
            if (!m_LoadedBundles.TryGetValue(bundleName, out loadedBundle))
            {
                return;
            }

            loadedBundle.ReferenceCount--;
            if (loadedBundle.ReferenceCount > 0)
            {
                return;
            }

            if (loadedBundle.Bundle != null)
            {
                loadedBundle.Bundle.Unload(unloadLoadedObjects);
            }

            m_LoadedBundles.Remove(bundleName);
        }

        public void UnloadAsset(string assetPath, bool unloadLoadedObjects = false)
        {
            MyAssetRecord assetRecord;
            if (!TryGetAssetRecord(assetPath, out assetRecord))
            {
                return;
            }

            foreach (string bundleName in GetRequiredBundleNames(assetRecord))
            {
                UnloadBundle(bundleName, unloadLoadedObjects);
            }
        }

        public void UnloadAllBundles(bool unloadLoadedObjects = false)
        {
            foreach (LoadedBundleInfo loadedBundle in m_LoadedBundles.Values)
            {
                if (loadedBundle.Bundle != null)
                {
                    loadedBundle.Bundle.Unload(unloadLoadedObjects);
                }
            }

            m_LoadedBundles.Clear();
        }

        private IEnumerator LoadAssetCoroutine<T>(string assetPath, Action<T> onCompleted) where T : UnityEngine.Object
        {
            if (!m_Initialized && !Initialize())
            {
                onCompleted?.Invoke(null);
                yield break;
            }

            MyAssetRecord assetRecord;
            if (!TryGetAssetRecord(assetPath, out assetRecord))
            {
                Log.Error("MyAsset asset not found: '{0}'.", assetPath);
                onCompleted?.Invoke(null);
                yield break;
            }

            foreach (string bundleName in GetRequiredBundleNames(assetRecord))
            {
                yield return EnsureBundleLoaded(bundleName);
                if (!m_LoadedBundles.ContainsKey(bundleName))
                {
                    onCompleted?.Invoke(null);
                    yield break;
                }
            }

            LoadedBundleInfo loadedBundle = m_LoadedBundles[assetRecord.bundleName];
            AssetBundleRequest request = loadedBundle.Bundle.LoadAssetAsync<T>(assetPath);
            yield return request;

            onCompleted?.Invoke(request.asset as T);
        }

        private IEnumerator EnsureBundleLoaded(string bundleName)
        {
            LoadedBundleInfo loadedBundle;
            if (m_LoadedBundles.TryGetValue(bundleName, out loadedBundle))
            {
                loadedBundle.ReferenceCount++;
                yield break;
            }

            LoadingBundleInfo loadingBundle;
            if (m_LoadingBundles.TryGetValue(bundleName, out loadingBundle))
            {
                loadingBundle.ReferenceCount++;
                while (m_LoadingBundles.ContainsKey(bundleName))
                {
                    yield return null;
                }

                yield break;
            }

            MyBundleRecord bundleRecord;
            if (!m_BundleMap.TryGetValue(bundleName, out bundleRecord))
            {
                Log.Error("MyAsset bundle not found: '{0}'.", bundleName);
                yield break;
            }

            string bundlePath = Path.Combine(Application.streamingAssetsPath, "MyAssets", bundleRecord.fileName);
            if (!File.Exists(bundlePath))
            {
                Log.Error("MyAsset bundle file not found: '{0}'.", bundlePath);
                yield break;
            }

            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
            loadingBundle = new LoadingBundleInfo
            {
                Request = request,
                ReferenceCount = 1
            };
            m_LoadingBundles[bundleName] = loadingBundle;

            yield return request;
            m_LoadingBundles.Remove(bundleName);

            if (request.assetBundle == null)
            {
                Log.Error("Failed to load MyAsset bundle: '{0}'.", bundlePath);
                yield break;
            }

            m_LoadedBundles[bundleName] = new LoadedBundleInfo
            {
                Bundle = request.assetBundle,
                ReferenceCount = loadingBundle.ReferenceCount
            };
        }

        private HashSet<string> GetRequiredBundleNames(MyAssetRecord assetRecord)
        {
            HashSet<string> bundleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (assetRecord == null)
            {
                return bundleNames;
            }

            if (!string.IsNullOrEmpty(assetRecord.bundleName))
            {
                bundleNames.Add(assetRecord.bundleName);
            }

            if (assetRecord.dependencyAssetPaths == null)
            {
                return bundleNames;
            }

            foreach (string dependencyAssetPath in assetRecord.dependencyAssetPaths)
            {
                MyAssetRecord dependencyRecord;
                if (TryGetAssetRecord(dependencyAssetPath, out dependencyRecord)
                    && !string.IsNullOrEmpty(dependencyRecord.bundleName))
                {
                    bundleNames.Add(dependencyRecord.bundleName);
                }
            }

            return bundleNames;
        }

        private bool TryGetAssetRecord(string assetPath, out MyAssetRecord assetRecord)
        {
            assetRecord = null;
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (m_AssetMap.TryGetValue(assetPath, out assetRecord))
            {
                return true;
            }

            string normalizedPath = assetPath.Replace('\\', '/');
            return m_AssetMap.TryGetValue(normalizedPath, out assetRecord);
        }

        private sealed class LoadedBundleInfo
        {
            public AssetBundle Bundle;
            public int ReferenceCount;
        }

        private sealed class LoadingBundleInfo
        {
            public AssetBundleCreateRequest Request;
            public int ReferenceCount;
        }
    }
}
