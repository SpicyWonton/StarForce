using System.IO;
using UnityEditor;
using UnityEngine;

namespace StarForce.Editor.MyAsset
{
    public static class MyAssetVerifier
    {
        [MenuItem("Tools/MyAsset/Verify")]
        public static void Verify()
        {
            if (!VerifyCollection())
            {
                return;
            }

            VerifyBuildOutput();
        }

        private static bool VerifyCollection()
        {
            if (!File.Exists(MyAssetPaths.CollectionRelativePath))
            {
                Debug.LogErrorFormat("Collection config not found: '{0}'.", MyAssetPaths.CollectionRelativePath);
                return false;
            }

            string json = File.ReadAllText(MyAssetPaths.CollectionRelativePath);
            MyAssetCollection collection = JsonUtility.FromJson<MyAssetCollection>(json);
            if (collection == null || collection.bundles == null)
            {
                Debug.LogError("Failed to parse MyAsset collection config.");
                return false;
            }

            int assetCount = 0;
            foreach (MyBundleConfig bundle in collection.bundles)
            {
                if (bundle == null || string.IsNullOrEmpty(bundle.bundleName))
                {
                    Debug.LogWarning("Found bundle with empty name in collection config.");
                    continue;
                }

                foreach (string guid in bundle.assetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        Debug.LogWarningFormat("Bundle '{0}' contains missing guid '{1}'.", bundle.bundleName, guid);
                        continue;
                    }

                    assetCount++;
                }
            }

            Debug.LogFormat(
                "MyAsset collection verified. Bundles: {0}, Assets: {1}.",
                collection.bundles.Count,
                assetCount);
            return true;
        }

        private static void VerifyBuildOutput()
        {
            string outputFolder = MyAssetPaths.OutputRelativeFolder;
            string versionPath = Path.Combine(outputFolder, MyAssetPaths.VersionFileName);
            if (!File.Exists(versionPath))
            {
                Debug.LogWarningFormat(
                    "Build output not found at '{0}'. Run Tools/MyAsset/Build after configuring collection.",
                    versionPath);
                return;
            }

            string versionJson = File.ReadAllText(versionPath);
            MyAssetVersion version = JsonUtility.FromJson<MyAssetVersion>(versionJson);
            if (version == null)
            {
                Debug.LogErrorFormat("Failed to parse version file: '{0}'.", versionPath);
                return;
            }

            int missingBundleCount = 0;
            if (version.bundles != null)
            {
                foreach (MyBundleRecord bundleRecord in version.bundles)
                {
                    string bundlePath = Path.Combine(outputFolder, bundleRecord.fileName);
                    if (!File.Exists(bundlePath))
                    {
                        missingBundleCount++;
                        Debug.LogErrorFormat("Missing bundle file: '{0}'.", bundlePath);
                    }
                }
            }

            if (missingBundleCount > 0)
            {
                Debug.LogErrorFormat("Build output verification failed. Missing bundles: {0}.", missingBundleCount);
                return;
            }

            Debug.LogFormat(
                "MyAsset build output verified. Assets: {0}, Bundles: {1}, Folder: '{2}'.",
                version.assets != null ? version.assets.Count : 0,
                version.bundles != null ? version.bundles.Count : 0,
                outputFolder);
        }
    }
}
