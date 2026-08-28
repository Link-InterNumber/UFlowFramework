using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class AssetReferenceData : IDisposable
    {
        private string _assetPath;
        public string assetPath => _assetPath;
        private string _bundleName;
        public string bundleName => _bundleName;
        // 依赖资源
        private HashSet<string> _assetDependent;
        // 引用这个资源的资源
        private HashSet<string> _bundleReferenced;
        public HashSet<string> assetDependent => _assetDependent;
        public HashSet<string> bundleReferenced => _bundleReferenced;
        public DefectLevel defectLevel = DefectLevel.None;

        public AssetReferenceData(string assetPath, string bundleName)
        {
            _assetPath = assetPath;
            _bundleName = bundleName;
            _assetDependent = HashSetPool<string>.Get();
            _bundleReferenced = HashSetPool<string>.Get();
        }

        public void Dispose()
        {
            _assetPath = null;
            _bundleName = null;
            if (_assetDependent != null) HashSetPool<string>.Release(_assetDependent);
            _assetDependent = null;
            if (_bundleReferenced != null) HashSetPool<string>.Release(_bundleReferenced);
            _bundleReferenced = null;
        }
    }
}