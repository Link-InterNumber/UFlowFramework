using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceData : IDisposable, IReferenceDataTemp
    {
        // 分包名
        public string bundleName;
        
        private HashSet<string> _bundleDependent;
        private HashSet<string> _bundleReferenced;
        
        // 依赖分包
        public HashSet<string> bundleDependent => _bundleDependent;
        // 引用这个分包的分包
        public HashSet<string> bundleReferenced => _bundleReferenced;
        // 资源名称列表
        public List<AssetReferenceData> assets;

        public DefectLevel defectLevel = DefectLevel.None;

        public List<string> tags;
        
        public void Dispose()
        {
            bundleName = null;
            // 资产对象由 BundleReferenceQueryer 的全局资产字典统一释放，
            // 避免同一 AssetReferenceData 被 Bundle 和全局字典重复归还对象池。
            assets?.Clear();
            assets = null;
            Inactivate();
        }

        public void Activate()
        {
            if (bundleDependent == null) _bundleDependent = HashSetPool<string>.Get();
            if (bundleReferenced == null) _bundleReferenced = HashSetPool<string>.Get();
            if (tags == null) tags = ListPool<string>.Get();
        }

        public void Inactivate()
        {
            if (_bundleDependent != null) HashSetPool<string>.Release(_bundleDependent);
            _bundleDependent = null;
            if (_bundleReferenced != null) HashSetPool<string>.Release(_bundleReferenced);
            _bundleReferenced = null;
            if (tags != null) ListPool<string>.Release(tags);
            tags = null;
        }
    }
}