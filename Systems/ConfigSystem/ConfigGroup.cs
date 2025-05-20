using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace PowerCellStudio
{
    public delegate void OnLoadCompleted(AssetLoadStatus status);
    
    public class ConfigGroup<T> where T : ConfAsyncLoadHandle, new ()
    {
        private List<ConfBaseCollections> _configs;
        private AssetLoadStatus _loadStatus;
        public AssetLoadStatus loadStatus => _loadStatus;


        public OnLoadCompleted onLoadCompleted;

        public string[] failLoadConfigs
        {
            get
            {
                return _configs.Where(o => o.loadStatus == AssetLoadStatus.Unload).Select(o => o.GetType().Name).ToArray();
            }
        }

        public ConfigGroup(params ConfBaseCollections[] configs)
        {
            _loadStatus = AssetLoadStatus.Unload;
            _configs = new List<ConfBaseCollections>();
            if (configs == null || configs.Length == 0) return;
            foreach (var confBaseCollections in configs)
            {
                _configs.Add(confBaseCollections);
            }
        }

        public void Append(ConfBaseCollections confBaseCollections)
        {
            _configs.Add(confBaseCollections);
        }

        public Coroutine LoadAll()
        {
            if (_configs.Count == 0)
            {
                _loadStatus = AssetLoadStatus.Loaded;
                onLoadCompleted?.Invoke(_loadStatus);
                return null;
            }

            _loadStatus = AssetLoadStatus.Loading;
            foreach (var confBaseCollections in _configs)
            {
                if (confBaseCollections.loadStatus == AssetLoadStatus.Loaded) continue;
                var handle = new T();
                confBaseCollections.LoadConfAsync(handle);
            }

            return ApplicationManager.instance.StartCoroutine(MonitoringLoadStatus());
        }

        public bool ReleaseAll()
        {
            if (_loadStatus == AssetLoadStatus.Loading) return false;
            foreach (var confBaseCollections in _configs)
            {
                confBaseCollections.Release();
            }
            return true;
        }

        private IEnumerator MonitoringLoadStatus()
        {
            while (_configs.Any(o => o.loadStatus == AssetLoadStatus.Loading))
            {
                yield return null;
            }
            _loadStatus = _configs.All(o => o.loadStatus == AssetLoadStatus.Loaded)
                ? AssetLoadStatus.Loaded
                : AssetLoadStatus.Unload;
            onLoadCompleted?.Invoke(_loadStatus);
        }
    }
}