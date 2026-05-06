using System.Collections.Generic;

namespace PowerCellStudio
{
    [System.Serializable]
    public class RemoteManifest : IPersistenceData
    {
        public List<BundleInfo> bundles = new List<BundleInfo>();

        public Dictionary<string, BundleInfo> GetMap()
        {
            var map = new Dictionary<string, BundleInfo>();
            foreach (var info in bundles)
            {
                map.Add(info.name, info);
            }
            return map;
        }
    }

    [System.Serializable]
    public class BundleInfo
    {
        public bool isRemote;
        public string name;
        public string md5;
        public long size;
    }
}

