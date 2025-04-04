using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class ConfigTypeInfo
    {
        public List<int> columns = new List<int>();
        public string fieldName;
        public string comment;
        public string typeName;
        public string refTypeName;
        public bool IsList => columns.Count > 1;
        public bool isKey;
    }
    
    [Serializable]
    public class ConfBase
    {
    }

    public abstract class ConfBaseCollections
    {
        public AssetLoadStatus loadStatus => _loadStatus;
        protected AssetLoadStatus _loadStatus = AssetLoadStatus.Unload;
        protected string _assetPath;
        public string assetPath => _assetPath;
        protected int _refCount = 0;
        public abstract void LoadConfAsync<T>(T handle) where T : ConfAsyncLoadHandle;
        public abstract void Release();
        
    }

    [Serializable]
    public abstract class ConfBaseData 
#if SCRIPTABLE_OBJECT_CONFIG
        : ScriptableObject
#endif
    {
    }
}