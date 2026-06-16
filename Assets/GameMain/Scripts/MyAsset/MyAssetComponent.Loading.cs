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
                while (!loadingAsset.IsDone)
                {
                    yield return null;
                }

                T loadedWaitingAsset = loadingAsset.Asset as T;
                if (loadingAsset.IsSuccess && loadedWaitingAsset != null)
                {
                    handle.Complete(true, loadedWaitingAsset, null);
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
                    loadingAsset.IsDone = true;
                    m_LoadingAssets.Remove(normalizedAssetPath);
                    ReleaseAssetBundles(assetRecord);
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
                loadingAsset.IsDone = true;
                m_LoadingAssets.Remove(normalizedAssetPath);
                ReleaseAssetBundles(assetRecord);
                handle.Complete(false, null, errorMessage);
                yield break;
            }

            m_LoadedAssets[normalizedAssetPath] = new LoadedAssetInfo
            {
                Asset = asset,
                ReferenceCount = loadingAsset.ReferenceCount
            };
            loadingAsset.Asset = asset;
            loadingAsset.IsSuccess = true;
            loadingAsset.IsDone = true;
            m_LoadingAssets.Remove(normalizedAssetPath);

            handle.Complete(true, asset, null);
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

    }
}
