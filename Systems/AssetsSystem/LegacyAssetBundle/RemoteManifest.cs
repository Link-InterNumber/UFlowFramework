using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace PowerCellStudio
{
    [System.Serializable]
    public class RemoteManifest
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
        public string name;
        public string md5;
        public long size;
    }
}

