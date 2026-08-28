using System;
using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceGroup : IDisposable
    {
        public string _groupName;
        public string groupName => _groupName;
        private HashSet<string> _bundleNames;
        public HashSet<string> bundleNames => _bundleNames;
        public DefectLevel defectLevel = DefectLevel.None;
        private Dictionary<string, GroupDefectInfo> _defectInfos;
        public Dictionary<string, GroupDefectInfo> defectInfos => _defectInfos;

        public BundleReferenceGroup(string groupName)
        {
            _groupName = groupName;
            _bundleNames =new HashSet<string>();
            _defectInfos = new Dictionary<string, GroupDefectInfo>();
        }

        public void Dispose()
        {
            _groupName = null;
            _bundleNames = null;
            if (_defectInfos != null)
            {
                foreach (var defectInfo in _defectInfos.Values)
                    defectInfo.bundleNames?.Clear();
                _defectInfos.Clear();
            }
            _defectInfos = null;
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
