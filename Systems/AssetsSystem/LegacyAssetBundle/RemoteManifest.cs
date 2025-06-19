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
        public RemoteManifest()
        {
            bundles = new List<BundleInfo>();
        }

        public List<BundleInfo> bundles;

        public Dictionary<sting, BundleInfo> GetMap()
        {
            var map = new Dictionary<sting, BundleInfo>();
            foreach (var info in bundles)
            {
                map.Add(bundles.name, bundles);
            }
            return map;
        }
    }

    [System.Serializable]
    public class BundleInfo
    {
        public string name;
        public string md5;
        public int size;
    }
}

