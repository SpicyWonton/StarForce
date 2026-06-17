using System.Collections;
using System.IO;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public partial class MyAssetComponent
    {
        private IEnumerator LoadAssetCoroutine<T>(string assetPath, MyAssetLoadHandle handle) where T : UnityEngine.Object
        {
            if (!m_Initialized && !Initialize())
            {
                string errorMessage = "MyAsset initialize failed.";
                handle.Complete(false, null, errorMessage);
                yield break;
            }

            MyAssetRecord assetRecord;
            if (!TryGetAssetRecord(assetPath, out assetRecord))
            {
                string errorMessage = string.Format("MyAsset asset not found: '{0}'.", assetPath);
                Log.Error(errorMessage);
                handle.Complete(false, null, errorMessage);
                yield break;
            }

            string normalizedAssetPath = assetRecord.assetPath;
            LoadedAssetInfo loadedAsset;
            if (m_LoadedAssets.TryGetValue(normalizedAssetPath, out loadedAsset))
            {
                loadedAsset.ReferenceCount++;
                T cachedAsset = loadedAsset.Asset as T;
                if (cachedAsset == null)
                {
                    loadedAsset.ReferenceCount--;
                    string errorMessage = string.Format("MyAsset cached asset type mismatch: '{0}'.", normalizedAssetPath);
                    Log.Error(errorMessage);
                    handle.Complete(false, null, errorMessage);
                    yield break;
                }

                handle.Complete(true, cachedAsset, null);
                yield break;
            }

            LoadingAssetInfo loadingAsset;
            if (m_LoadingAssets.TryGetValue(normalizedAssetPath, out loadingAsset))
            {
                loadingAsset.ReferenceCount++;
                while (m_LoadingAssets.ContainsKey(normalizedAssetPath))
                {
                    yield return null;
                }

                LoadedAssetInfo loadedWaitingAsset;
                if (m_LoadedAssets.TryGetValue(normalizedAssetPath, out loadedWaitingAsset))
                {
                    T waitingAsset = loadedWaitingAsset.Asset as T;
                    if (waitingAsset != null)
                    {
                        handle.Complete(true, waitingAsset, null);
                    }
                    else
                    {
                        string errorMessage = string.Format("MyAsset cached asset type mismatch: '{0}'.", normalizedAssetPath);
                        Log.Error(errorMessage);
                        handle.Complete(false, null, errorMessage);
                    }
                }
                else
                {
                    string errorMessage = string.IsNullOrEmpty(loadingAsset.ErrorMessage)
                        ? string.Format("MyAsset asset load failed: '{0}'.", normalizedAssetPath)
                        : loadingAsset.ErrorMessage;
                    handle.Complete(false, null, errorMessage);
                }

                yield break;
            }

            loadingAsset = new LoadingAssetInfo
            {
                ReferenceCount = 1
            };
            m_LoadingAssets[normalizedAssetPath] = loadingAsset;

            foreach (string bundleName in GetRequiredBundleNames(assetRecord))
            {
                yield return EnsureBundleLoaded(bundleName);
                if (!m_LoadedBundles.ContainsKey(bundleName))
                {
                    string errorMessage = string.Format("MyAsset bundle load failed: '{0}'.", bundleName);
                    loadingAsset.ErrorMessage = errorMessage;
                    m_LoadingAssets.Remove(normalizedAssetPath);
                    handle.Complete(false, null, errorMessage);
                    yield break;
                }
            }

            LoadedBundleInfo loadedBundle = m_LoadedBundles[assetRecord.bundleName];
            AssetBundleRequest request = loadedBundle.Bundle.LoadAssetAsync<T>(normalizedAssetPath);
            yield return request;

            T asset = request.asset as T;
            if (asset == null)
            {
                string errorMessage = string.Format("MyAsset asset type mismatch or missing: '{0}'.", normalizedAssetPath);
                Log.Error(errorMessage);
                loadingAsset.ErrorMessage = errorMessage;
                m_LoadingAssets.Remove(normalizedAssetPath);
                handle.Complete(false, null, errorMessage);
                yield break;
            }

            m_LoadedAssets[normalizedAssetPath] = new LoadedAssetInfo
            {
                Asset = asset,
                ReferenceCount = loadingAsset.ReferenceCount
            };
            AddBundleReferences(assetRecord);

            m_LoadingAssets.Remove(normalizedAssetPath);

            handle.Complete(true, asset, null);
        }

        private IEnumerator EnsureBundleLoaded(string bundleName)
        {
            if (m_LoadedBundles.ContainsKey(bundleName))
            {
                yield break;
            }

            if (m_LoadingBundles.Contains(bundleName))
            {
                while (m_LoadingBundles.Contains(bundleName))
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
            m_LoadingBundles.Add(bundleName);

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
                ReferenceCount = 0
            };
        }

    }
}
