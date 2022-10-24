using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Link.Editor.BundleBuilds
{
    [Serializable]
    public struct ScriptableAssetBundleData
    {
        public string fold;
        public string assetName;
        public string assetBundle;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AssetBundleData : { ");

            sb.AppendFormat("fold: {0}, ", fold);
            sb.AppendFormat("assetName: {0}, ", assetName);
            sb.AppendFormat("assetBundle: {0}, ", assetBundle);
            sb.Append(" }");
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScriptableAssetBundleData == false) return false;

            var o = (ScriptableAssetBundleData)obj;

            if (fold != o.fold) return false;
            if (assetName != o.assetName) return false;
            if (assetBundle != o.assetBundle) return false;

            return true;
        }

        public static bool operator ==(ScriptableAssetBundleData lhs, ScriptableAssetBundleData rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ScriptableAssetBundleData lhs, ScriptableAssetBundleData rhs)
        {
            return !lhs.Equals(rhs);
        }
    }

    [Serializable]
    public class ScriptableAssetBundle : ScriptableObject
    {
        public List<ScriptableAssetBundleData> source = new List<ScriptableAssetBundleData>();
    }
}