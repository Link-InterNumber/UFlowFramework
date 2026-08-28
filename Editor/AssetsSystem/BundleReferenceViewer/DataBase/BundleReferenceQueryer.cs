using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceQueryer : IDisposable
    {
        private Dictionary<string, BundleReferenceData> _bundleReferenceDict;
        private Dictionary<string, BundleReferenceGroup> _bundleReferenceGroupDict;
        private Dictionary<string, string> _bundleGroupMap;
        private Dictionary<string, AssetReferenceData> _assetReferenceDict;

        private int _bundleCount;
        public int bundleCount => _bundleCount;

        public BundleReferenceQueryer()
        {
            _bundleReferenceDict = new Dictionary<string, BundleReferenceData>();
            _bundleReferenceGroupDict = new Dictionary<string, BundleReferenceGroup>();
            _bundleGroupMap = new Dictionary<string, string>();
            _assetReferenceDict = new Dictionary<string, AssetReferenceData>();
        }

        public void Dispose()
        {
            if (_bundleReferenceGroupDict != null)
            {
                foreach (var keyValuePair in _bundleReferenceGroupDict)
                    keyValuePair.Value.Dispose();
                _bundleReferenceGroupDict.Clear();
                _bundleReferenceGroupDict = null;
            }

            _bundleGroupMap?.Clear();
            _bundleGroupMap = null;
            
            if (_bundleReferenceDict != null)
            {
                foreach (var keyValuePair in _bundleReferenceDict)
                    keyValuePair.Value.Dispose();
                _bundleReferenceDict.Clear();
                _bundleReferenceDict = null;
            }
            
            if (_assetReferenceDict != null)
            {
                foreach (var keyValuePair in _assetReferenceDict)
                    keyValuePair.Value.Dispose();
                _assetReferenceDict.Clear();
                _assetReferenceDict = null;
            }
        }

        public void ReleaseBundleAssetsData(string bundleName)
        {
            var bundleData = GetBundleData(bundleName);
            if (bundleData == null) return;
            
            for (var i = 0; i < bundleData.assets.Count; i++)
            {
                var assetName = bundleData.assets[i];
                if (_assetReferenceDict.Remove(assetName, out var current))
                    current.Dispose();
            }
        }

        public void AddBundleData(string bundleName, string[] bundleDependents)
        {
            if (_bundleReferenceDict.TryGetValue(bundleName, out var exiteData))
            {
                exiteData.bundleDependent.UnionWith(bundleDependents);
                _bundleReferenceDict[bundleName] = exiteData;
            }
            else
            {
                var data = new BundleReferenceData();
                data.bundleName = bundleName;
                data.bundleDependent.UnionWith(bundleDependents);
                _bundleReferenceDict.Add(bundleName, data);
            }

            for (var i = 0; i < bundleDependents.Length; i++)
            {
                var abName = bundleDependents[i];
                if (_bundleReferenceDict.TryGetValue(abName, out var exiteTemp))
                {
                    exiteTemp.bundleReferenced.Add(bundleName);
                    _bundleReferenceDict[abName] = exiteTemp;
                    continue;
                }
                exiteTemp = new BundleReferenceData();
                exiteTemp.bundleName = abName;
                exiteTemp.bundleReferenced.Add(bundleName);
                _bundleReferenceDict.Add(abName, exiteTemp);
            }
        }

        public void SetBundleDefects(string bundleName, ICollection<string> defects)
        {
            if (_bundleReferenceDict.TryGetValue(bundleName, out var data))
            {
                data.tags.AddRange(defects);
                _bundleReferenceDict[bundleName] = data;
            }
        }

        public BundleReferenceData GetBundleData(string bundleName)
        {
            if (_bundleReferenceDict.TryGetValue(bundleName, out var data))
            {
                return data;
            }
            return null;
        }

        public IReadOnlyDictionary<string, BundleReferenceData> GetAllBundleData()
        {
            return _bundleReferenceDict;
        }

        public IReadOnlyDictionary<string, BundleReferenceGroup> GetAllGroups()
        {
            EnsureGroups();
            return _bundleReferenceGroupDict;
        }

        private void AddAsset(AssetReferenceData assetData)
        {
            if (assetData == null || string.IsNullOrEmpty(assetData.assetPath))
                return;
            if (_assetReferenceDict.TryGetValue(assetData.assetPath, out var existingData))
            {
                if (ReferenceEquals(existingData, assetData))
                    return;
                assetData.assetDependent.UnionWith(existingData.assetDependent);
                assetData.bundleReferenced.UnionWith(existingData.bundleReferenced);
                existingData.Dispose();
                _assetReferenceDict[assetData.assetPath] = assetData;
            }
            else
            {
                _assetReferenceDict[assetData.assetPath] = assetData;
            }
            foreach (var se in assetData.assetDependent)
            {
                if (_assetReferenceDict.TryGetValue(se, out var dependentData))
                {
                    dependentData.bundleReferenced.Add(assetData.assetPath);
                }
                else
                {
                    var bundleName = AssetDatabase.GetImplicitAssetBundleName(se);
                    dependentData = new AssetReferenceData(se, bundleName);
                    dependentData.bundleReferenced.Add(assetData.assetPath);
                    _assetReferenceDict[se] = dependentData;
                }
            }
        }

        public void SetAssets(string bundleName, List<AssetReferenceData> assets)
        {
            if (assets == null || assets.Count == 0)
                return;
            if (!_bundleReferenceDict.TryGetValue(bundleName, out var data))
                return;
            data.assets.Clear();
            for (var i = 0; i < assets.Count; i++)
            {
                data.assets.Add(assets[i].assetPath);
                AddAsset(assets[i]);
            }
        }
        
        public AssetReferenceData GetAssetData(string assetPath)
        {
            if (_assetReferenceDict.TryGetValue(assetPath, out var data))
                return data;
            return null;
        }

        internal void SeBundleCount()
        {
            _bundleCount = _bundleReferenceDict.Count;
        }

        public void EnsureGroups()
        {
            if (_bundleReferenceGroupDict.Count > 0 || _bundleReferenceDict.Count == 0)
                return;
            var unassigned = HashSetPool<string>.Get();
            unassigned.UnionWith(_bundleReferenceDict.Keys);
            var groupIndex = 1;
            var queue = new Queue<string>();
            
            while (unassigned.Count > 0)
            {
                var first = string.Empty;
                foreach (var bundleName in unassigned)
                {
                    first = bundleName;
                    break;
                }

                var groupName = $"Group {groupIndex:000}";
                var group = new BundleReferenceGroup(groupName);
                queue.Enqueue(first);
                unassigned.Remove(first);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    group.bundleNames.Add(current);
                    var currentData = _bundleReferenceDict[current];
                    AddGroupNeighbors(currentData.bundleDependent, queue, unassigned);
                    AddGroupNeighbors(currentData.bundleReferenced, queue, unassigned);
                }

                _bundleReferenceGroupDict.Add(groupName, group);
                foreach (var bundleName in group.bundleNames)
                    _bundleGroupMap[bundleName] = groupName;
                groupIndex++;
            }
            HashSetPool<string>.Release(unassigned);
        }

        private static void AddGroupNeighbors(HashSet<string> neighbors, Queue<string> queue, HashSet<string> unassigned)
        {
            if (neighbors == null) return;
            
            foreach (var neighbor in neighbors)
            {
                if (!unassigned.Remove(neighbor)) continue;
                queue.Enqueue(neighbor);
            }
        }

        public BundleReferenceGroup GetGroupByBundle(string bundleName)
        {
            EnsureGroups();
            return (_bundleGroupMap.TryGetValue(bundleName, out var groupName) &&
                    _bundleReferenceGroupDict.TryGetValue(groupName, out var group)) ? group : default;
        }

        public BundleReferenceGroup GetGroupData(string groupName)
        {
            if (_bundleReferenceGroupDict.TryGetValue(groupName, out var group))
                return group;
            return null;
        }
    }
}