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
        
        public int bundleCount => _bundleReferenceDict?.Count??0;

        public BundleReferenceQueryer()
        {
            _bundleReferenceDict = new Dictionary<string, BundleReferenceData>();
            _bundleReferenceGroupDict = new Dictionary<string, BundleReferenceGroup>();
            _bundleGroupMap = new Dictionary<string, string>();
            _assetReferenceDict = new Dictionary<string, AssetReferenceData>();
        }

        public void Dispose()
        {
            if (_bundleReferenceDict != null)
            {
                foreach (var keyValuePair in _bundleReferenceDict)
                    keyValuePair.Value.Dispose();
                _bundleReferenceDict.Clear();
                _bundleReferenceDict = null;
            }

            if (_bundleReferenceGroupDict != null)
            {
                foreach (var keyValuePair in _bundleReferenceGroupDict)
                    keyValuePair.Value.Dispose();
                _bundleReferenceGroupDict.Clear();
                _bundleReferenceGroupDict = null;
            }
            
            if (_assetReferenceDict != null)
            {
                // foreach (var keyValuePair in _assetReferenceDict)
                //     keyValuePair.Value.Dispose();
                _assetReferenceDict.Clear();
                _assetReferenceDict = null;
            }

            _bundleGroupMap?.Clear();
            _bundleGroupMap = null;
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
                data.Activate();
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
                exiteTemp.Activate();
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

            data = new BundleReferenceData()
            {
                bundleName = bundleName
            };
            data.Activate();
            return data;
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
                existingData.bundleName = assetData.bundleName;
                existingData.assetDependent.UnionWith(assetData.assetDependent);
                _assetReferenceDict[assetData.assetPath] = existingData;
            }
            else
            {
                assetData.Activate();
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
                    dependentData = new AssetReferenceData
                    {
                        assetPath = se,
                        bundleName = bundleName
                    };
                    dependentData.Activate();
                    dependentData.bundleReferenced.Add(assetData.assetPath);
                    _assetReferenceDict[se] = dependentData;
                }
            }
        }

        public void SetAssets(string bundleName, List<AssetReferenceData> assets)
        {
            if (!_bundleReferenceDict.TryGetValue(bundleName, out var data))
                return;
            data.assets = assets;
            for (var i = 0; i < assets.Count; i++)
            {
                AddAsset(assets[i]);
            }
        }

        public void EnsureAssets(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName) ||
                !_bundleReferenceDict.TryGetValue(bundleName, out var data) ||
                data.assets != null)
                return;

            var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            var assets = new List<AssetReferenceData>(assetPaths.Length);
            for (var i = 0; i < assetPaths.Length; i++)
            {
                var asset = AssetReferenceCollector.FindDirectReferences(bundleName, assetPaths[i]);
                if (asset != null)
                    assets.Add(asset);
            }

            SetAssets(bundleName, assets);
        }
        
        public AssetReferenceData GetAsset(string assetPath)
        {
            if (_assetReferenceDict.TryGetValue(assetPath, out var data))
                return data;
            data = new AssetReferenceData()
            {
                assetPath = assetPath
            };
            data.Activate();
            return data;
        }

        private void EnsureGroups()
        {
            if (_bundleReferenceGroupDict.Count > 0 || _bundleReferenceDict.Count == 0)
                return;

            var unassigned = HashSetPool<string>.Get();
            unassigned.UnionWith(_bundleReferenceDict.Keys);
            var groupIndex = 1;
            while (unassigned.Count > 0)
            {
                var first = string.Empty;
                foreach (var bundleName in unassigned)
                {
                    first = bundleName;
                    break;
                }

                var groupName = $"Group {groupIndex:000}";
                var group = new BundleReferenceGroup
                {
                    groupName = groupName,
                    bundleNames = HashSetPool<string>.Get()
                };
                var queue = new Queue<string>();
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
    }
}