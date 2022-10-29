using System;
using System.Collections.Generic;
using UnityEngine;

namespace Link.EditorScript.BundleBuilds
{
    [Serializable]
    public class ScriptableAssetBundle : ScriptableObject
    {
        public List<ScriptableAssetBundleData> source = new List<ScriptableAssetBundleData>();
    }
}