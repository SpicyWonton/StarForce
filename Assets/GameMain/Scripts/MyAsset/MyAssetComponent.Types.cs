using System.Collections;
using UnityEngine;

namespace StarForce
{
    public partial class MyAssetComponent
    {
        private sealed class LoadedBundleInfo
        {
            public AssetBundle Bundle;
            public int ReferenceCount;
        }

        private sealed class LoadedAssetInfo
        {
            public UnityEngine.Object Asset;
            public int ReferenceCount;
        }

        private sealed class LoadingAssetInfo
        {
            public UnityEngine.Object Asset;
            public int ReferenceCount;
            public bool IsDone;
            public bool IsSuccess;
            public string ErrorMessage;
        }

        private sealed class LoadingBundleInfo
        {
            public int ReferenceCount;
        }

        private sealed class MyAssetLoadTask
        {
            public int Priority;
            public long SerialId;
            public MyAssetLoadHandle Handle;
            public IEnumerator Routine;
        }
    }
}
