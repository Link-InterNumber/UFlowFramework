using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class ScriptableAssetBundle : ScriptableObject
    {
        public List<ScriptableAssetBundleData> source = new List<ScriptableAssetBundleData>();
    }
}