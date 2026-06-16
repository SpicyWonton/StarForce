using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StarForce.Editor.MyAsset
{
    public static class MyAssetBuilder
    {
        [MenuItem("Tools/MyAsset/Build")]
        public static void Build()
        {
            if (!File.Exists(MyAssetPaths.CollectionRelativePath))
            {
                Debug.LogErrorFormat("Collection config not found: '{0}'.", MyAssetPaths.CollectionRelativePath);
                return;
            }

            string json = File.ReadAllText(MyAssetPaths.CollectionRelativePath);
            MyAssetCollection collection = JsonUtility.FromJson<MyAssetCollection>(json);
            if (collection == null || collection.bundles == null || collection.bundles.Count == 0)
            {
                Debug.LogError("MyAsset collection is empty. Add bundles in Tools/MyAsset/Collection first.");
                return;
            }

            Dictionary<string, string> guidToBundle = new Dictionary<string, string>();
            Dictionary<string, string> pathToBundle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> bundleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
            bool hasValidationError = false;

            foreach (MyBundleConfig bundleConfig in collection.bundles)
            {
                if (bundleConfig == null || string.IsNullOrEmpty(bundleConfig.bundleName))
                {
                    Debug.LogWarning("Skip bundle with empty name.");
                    continue;
                }

                if (!bundleNames.Add(bundleConfig.bundleName))
                {
                    Debug.LogErrorFormat("Duplicate MyAsset bundle name: '{0}'.", bundleConfig.bundleName);
                    hasValidationError = true;
                    continue;
                }

                List<string> assetPaths = new List<string>();
                foreach (string guid in bundleConfig.assetGuids)
                {
                    if (string.IsNullOrEmpty(guid))
                    {
                        continue;
                    }

                    string existingBundleName;
                    if (guidToBundle.TryGetValue(guid, out existingBundleName))
                    {
                        Debug.LogErrorFormat(
                            "Asset guid '{0}' is collected by both bundle '{1}' and '{2}'.",
                            guid,
                            existingBundleName,
                            bundleConfig.bundleName);
                        hasValidationError = true;
                        continue;
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        Debug.LogWarningFormat("Skip missing asset guid '{0}' in bundle '{1}'.", guid, bundleConfig.bundleName);
                        continue;
                    }

                    guidToBundle[guid] = bundleConfig.bundleName;
                    pathToBundle[assetPath] = bundleConfig.bundleName;
                    assetPaths.Add(assetPath);
                }

                if (assetPaths.Count == 0)
                {
                    Debug.LogWarningFormat("Bundle '{0}' has no valid assets.", bundleConfig.bundleName);
                    continue;
                }

                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = bundleConfig.bundleName,
                    assetNames = assetPaths.ToArray()
                });
            }

            if (hasValidationError)
            {
                Debug.LogError("MyAsset build stopped because collection validation failed.");
                return;
            }

            if (builds.Count == 0)
            {
                Debug.LogError("No valid bundles to build.");
                return;
            }

            string tempOutputPath = Path.Combine(Path.GetTempPath(), "MyAssetBuild", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempOutputPath);

            try
            {
                BuildAssetBundleOptions options = BuildAssetBundleOptions.ForceRebuildAssetBundle
                    | BuildAssetBundleOptions.StrictMode;
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                    tempOutputPath,
                    builds.ToArray(),
                    options,
                    EditorUserBuildSettings.activeBuildTarget);

                if (manifest == null)
                {
                    Debug.LogError("BuildPipeline.BuildAssetBundles failed.");
                    return;
                }

                string outputFolder = MyAssetPaths.OutputRelativeFolder;
                if (Directory.Exists(outputFolder))
                {
                    Directory.Delete(outputFolder, true);
                }

                Directory.CreateDirectory(outputFolder);

                MyAssetVersion version = new MyAssetVersion();
                foreach (AssetBundleBuild build in builds)
                {
                    string sourceFile = Path.Combine(tempOutputPath, build.assetBundleName);
                    if (!File.Exists(sourceFile))
                    {
                        Debug.LogErrorFormat("Built bundle file not found: '{0}'.", sourceFile);
                        return;
                    }

                    string fileName = build.assetBundleName;
                    string destinationFile = Path.Combine(outputFolder, fileName);
                    File.Copy(sourceFile, destinationFile, true);

                    byte[] fileBytes = File.ReadAllBytes(destinationFile);
                    version.bundles.Add(new MyBundleRecord
                    {
                        bundleName = build.assetBundleName,
                        fileName = fileName,
                        size = fileBytes.LongLength,
                        hash = ComputeHash(fileBytes)
                    });
                }

                foreach (AssetBundleBuild build in builds)
                {
                    foreach (string assetPath in build.assetNames)
                    {
                        MyAssetRecord assetRecord = new MyAssetRecord
                        {
                            assetPath = assetPath,
                            bundleName = build.assetBundleName
                        };

                        string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
                        foreach (string dependencyPath in dependencies)
                        {
                            if (dependencyPath == assetPath)
                            {
                                continue;
                            }

                            if (pathToBundle.ContainsKey(dependencyPath))
                            {
                                assetRecord.dependencyAssetPaths.Add(dependencyPath);
                            }
                            else if (ShouldWarnMissingDependency(dependencyPath))
                            {
                                Debug.LogWarningFormat(
                                    "Asset '{0}' depends on uncollected asset '{1}'. It will not be recorded as a MyAsset dependency.",
                                    assetPath,
                                    dependencyPath);
                            }
                        }

                        version.assets.Add(assetRecord);
                    }
                }

                string versionJson = JsonUtility.ToJson(version, true);
                string versionPath = Path.Combine(outputFolder, MyAssetPaths.VersionFileName);
                File.WriteAllText(versionPath, versionJson, Encoding.UTF8);

                AssetDatabase.Refresh();
                Debug.LogFormat(
                    "MyAsset build completed. Bundles: {0}, Assets: {1}, Output: '{2}'.",
                    version.bundles.Count,
                    version.assets.Count,
                    outputFolder);
            }
            finally
            {
                if (Directory.Exists(tempOutputPath))
                {
                    Directory.Delete(tempOutputPath, true);
                }
            }
        }

        private static string ComputeHash(byte[] bytes)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hashBytes.Length * 2);
                foreach (byte hashByte in hashBytes)
                {
                    builder.Append(hashByte.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static bool ShouldWarnMissingDependency(string dependencyPath)
        {
            if (string.IsNullOrEmpty(dependencyPath))
            {
                return false;
            }

            string extension = Path.GetExtension(dependencyPath).ToLowerInvariant();
            return extension != ".cs" && extension != ".dll";
        }
    }
}
