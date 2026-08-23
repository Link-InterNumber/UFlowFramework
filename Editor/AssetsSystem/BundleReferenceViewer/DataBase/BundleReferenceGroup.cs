using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceGroup : IDisposable
    {
        public string groupName;
        public HashSet<string> bundleNames;
        public DefectLevel defectLevel = DefectLevel.None;
        public Dictionary<string, GroupDefectInfo> defectInfos = new Dictionary<string, GroupDefectInfo>();

        public void Dispose()
        {
            groupName = null;
            if (bundleNames != null) HashSetPool<string>.Release(bundleNames);
            bundleNames = null;
            defectInfos = null;
        }
    }

    public struct GroupDefectInfo
    {
        public DefectLevel level;
        
        public int count;
        
        public string toolTips;
        
        public string tag;
        
        public List<string> bundleNames;
    }
}
