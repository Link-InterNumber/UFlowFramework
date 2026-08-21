using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class AssetReferenceData : IDisposable, IReferenceDataTemp
    {
        public string assetPath;
        public string bundleName;
        private HashSet<string> _assetDependent;
        private HashSet<string> _bundleReferenced;
        public HashSet<string> assetDependent => _assetDependent;
        public HashSet<string> bundleReferenced => _bundleReferenced;
        public DefectLevel defectLevel = DefectLevel.None;

        public AssetReferenceData()
        {
            Activate();
        }

        public void Dispose()
        {
            assetPath = null;
            bundleName = null;
            Inactivate();
        }

        public void Activate()
        {
            if (assetDependent == null) _assetDependent = HashSetPool<string>.Get();
            if (bundleReferenced == null) _bundleReferenced = HashSetPool<string>.Get();
        }

        public void Inactivate()
        {
            if (_assetDependent != null) HashSetPool<string>.Release(_assetDependent);
            _assetDependent = null;
            if (_bundleReferenced != null) HashSetPool<string>.Release(_bundleReferenced);
            _bundleReferenced = null;
        }
    }
}