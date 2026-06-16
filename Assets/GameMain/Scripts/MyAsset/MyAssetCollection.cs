using System;
using System.Collections.Generic;

namespace StarForce
{
    [Serializable]
    public class MyBundleConfig
    {
        public string bundleName = string.Empty;
        public List<string> assetGuids = new List<string>();
    }

    [Serializable]
    public class MyAssetCollection
    {
        public List<MyBundleConfig> bundles = new List<MyBundleConfig>();
    }

    [Serializable]
    public class MyAssetRecord
    {
        public string assetPath = string.Empty;
        public string bundleName = string.Empty;
        public List<string> dependencyAssetPaths = new List<string>();
    }

    [Serializable]
    public class MyBundleRecord
    {
        public string bundleName = string.Empty;
        public string fileName = string.Empty;
        public long size;
        public string hash = string.Empty;
    }

    [Serializable]
    public class MyAssetVersion
    {
        public List<MyAssetRecord> assets = new List<MyAssetRecord>();
        public List<MyBundleRecord> bundles = new List<MyBundleRecord>();
    }

    public static class MyAssetPaths
    {
        public const string CollectionRelativePath = "Assets/GameMain/Configs/MyAssetCollection.json";
        public const string OutputRelativeFolder = "Assets/StreamingAssets/MyAssets";
        public const string VersionFileName = "version.json";
    }
}
