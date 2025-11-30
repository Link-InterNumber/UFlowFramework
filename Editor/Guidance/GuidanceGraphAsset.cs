using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class GuidanceGraphAsset : ScriptableObject
    {
        [SerializeField]
        public List<int> guidanceIds = new List<int>();

        [SerializeField]
        public List<string> prefabGuids = new List<string>();

        [SerializeField]
        public List<string> targetNodes = new List<string>();
    }
}