using System;
using System.Collections.Generic;

namespace StarForce
{
    public partial class MyAssetComponent
    {
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

            LoadedAssetInfo loadedAsset;
            if (!m_LoadedAssets.TryGetValue(assetRecord.assetPath, out loadedAsset))
            {
                return;
            }

            loadedAsset.ReferenceCount--;
            if (loadedAsset.ReferenceCount > 0)
            {
                return;
            }

            m_LoadedAssets.Remove(assetRecord.assetPath);
            ReleaseAssetBundles(assetRecord, unloadLoadedObjects);
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
            m_LoadedAssets.Clear();
            m_LoadingAssets.Clear();
            m_LoadingBundles.Clear();
            ClearWaitingLoadTasks("MyAsset waiting load task was canceled by UnloadAllBundles.");
        }

        private void ReleaseAssetBundles(MyAssetRecord assetRecord, bool unloadLoadedObjects = false)
        {
            foreach (string bundleName in GetRequiredBundleNames(assetRecord))
            {
                UnloadBundle(bundleName, unloadLoadedObjects);
            }
        }

        private void AddBundleReferences(MyAssetRecord assetRecord)
        {
            foreach (string bundleName in GetRequiredBundleNames(assetRecord))
            {
                LoadedBundleInfo loadedBundle;
                if (m_LoadedBundles.TryGetValue(bundleName, out loadedBundle))
                {
                    loadedBundle.ReferenceCount++;
                }
            }
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
    }
}
