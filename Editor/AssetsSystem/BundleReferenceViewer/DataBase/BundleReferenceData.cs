using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceData : IDisposable
    {
        // 分包名
        public string bundleName;
        public DefectLevel defectLevel = DefectLevel.None;
        
        private HashSet<string> _bundleDependent;
        private HashSet<string> _bundleReferenced;
        private List<string> _assets;
        private List<string> _tags;
        private List<string> _defectDetail;
        
        // 依赖分包
        public HashSet<string> bundleDependent => _bundleDependent;
        // 引用这个分包的分包
        public HashSet<string> bundleReferenced => _bundleReferenced;
        // 资源名称列表
        public List<string> assets => _assets;
        // 缺陷
        public List<string> tags => _tags;

        public List<string> defectDetail => _defectDetail;

        public BundleReferenceData()
        {
            _bundleDependent = new HashSet<string> ();
            _bundleReferenced = new HashSet<string> ();
            _tags = new List<string>();
            _assets = new List<string>();
            _defectDetail = new List<string>();
        }
        
        public void Dispose()
        {
            bundleName = null;
            _bundleDependent = null;
            _bundleReferenced = null;
            _tags = null;
            _assets = null;
            _defectDetail = null;
        }
    }
}