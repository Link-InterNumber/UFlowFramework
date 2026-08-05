using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class AssetBatchLoader : PoolObject
    {
        private IAssetLoader _assetLoader;

        private int _totalCount;
        public float progress => _labels != null && _labels.Count > 0 ? _totalCount / (float)_labels.Count : 0f;

        private IList<string> _labels;
        private bool _canceled;
        private HashSet<string> _loadedLabels;

        /// <summary>
        /// 按标签批量准备资源。
        /// <para>Prepare assets in batch by labels.</para>
        /// </summary>
        /// <param name="labels">资源标签数组。<para>Asset label array.</para></param>
        /// <param name="onComplete">准备完成回调。<para>Callback when preparation is complete.</para></param>
        /// <param name="isConcurrent">是否并发加载。<para>Whether to load concurrently.</para></param>
        /// <returns>准备处理句柄。<para>Prepare handler.</para></returns>
        public void Prepare(IAssetLoader assetLoader, IList<string> labels, Action onComplete, bool isConcurrent = false)
        {
            if (assetLoader == null)
            {
                AssetLogger.LogError("AssetLoader is null, cannot prepare assets.");
                return;
            }
            if (labels == null)
            {
                AssetLogger.LogError("Labels array is null, cannot prepare assets.");
                return;
            }
            if (labels.Count == 0)
            {
                AssetLogger.LogWarning("Labels array is empty, nothing to prepare.");
                onComplete?.Invoke();
                return;
            }
            if (_assetLoader != null)
            {
                AssetLogger.LogError("This AssetPreLoader is already in use, cannot prepare assets.");
                return;
            }
            _assetLoader = assetLoader;
            _labels = labels;
            _totalCount = 0;
            _canceled = false;
            _loadedLabels = HashSetPool<string>.Get();
            if (isConcurrent)
            {
                for (int i = 0; i < labels.Count; i++)
                {
                    var label = labels[i];
                    _assetLoader.LoadAsync<UnityEngine.Object>(label, _ =>
                    {
                        if (_canceled)
                        {
                            _assetLoader.Release(label);
                            return;
                        }
                        _totalCount++;
                        _loadedLabels.Add(label);
                        if (labels != null && _totalCount >= labels.Count)
                        {
                            onComplete?.Invoke();
                        }
                    }, () =>
                    {
                        if (_canceled) return;
                        _totalCount++;
                        if (labels != null && _totalCount >= labels.Count)
                        {
                            onComplete?.Invoke();
                        }
                    });
                }
            }
            else
            {
                LoadNext(0, onComplete);
            }
        }

        private void LoadNext(int index, Action onComplete)
        {
            if (_canceled || _labels == null) return;
            if (_assetLoader == null || !_assetLoader.spawned)
            {
                _canceled = true;
                return;
            }
            if (index >= _labels.Count)
            {
                onComplete?.Invoke();
                return;
            }
            var label = _labels[index];
            _assetLoader.LoadAsync<UnityEngine.Object>(label, _ =>
            {
                _totalCount++;
                _loadedLabels.Add(label);
                LoadNext(index + 1, onComplete);
            }, () =>
            {
                _totalCount++;
                LoadNext(index + 1, onComplete);
            });
        }

        /// <summary>
        /// 卸载准备好的资源。
        /// <para>Cancel asset preparation.</para>
        /// </summary>
        public void Unprepare()
        {
            if (_canceled) return;
            _canceled = true;
            _labels = null;
            _totalCount = 0;
            if (_assetLoader != null && _assetLoader.spawned)
            {
                foreach (var label in _loadedLabels)
                {
                    _assetLoader.Release(label);
                }
            }
            _assetLoader = null;
            HashSetPool<string>.Release(_loadedLabels);
            _loadedLabels = null;
        }

        public IEnumerator WaitForCompletion()
        {
            while (!_canceled && _labels != null && _totalCount < _labels.Count)
            {
                yield return null;
            }
        }

        public override void OnSpawn()
        {
            if (_loadedLabels != null) return;
            _loadedLabels = HashSetPool<string>.Get();
        }

        public override void OnDeSpawn()
        {
            _assetLoader = null;
            if (_loadedLabels == null) return;
            HashSetPool<string>.Release(_loadedLabels);
            _loadedLabels = null;
        }
    }
}