using System;

namespace PowerCellStudio
{
    public class AssetPreLoader
    {
        private IAssetLoader _assetLoader;

        public AssetPreLoader(IAssetLoader assetLoader)
        {
            _assetLoader = assetLoader;
        }

        private int _totalCount;
        public float progress => _labels != null && _labels.Length > 0 ? _totalCount / (float)_labels.Length : 0f;

        private string[] _labels;

        /// <summary>
        /// 按标签批量准备资源。
        /// <para>Prepare assets in batch by labels.</para>
        /// </summary>
        /// <param name="labels">资源标签数组。<para>Asset label array.</para></param>
        /// <param name="onComplete">准备完成回调。<para>Callback when preparation is complete.</para></param>
        /// <param name="isConcurrent">是否并发加载。<para>Whether to load concurrently.</para></param>
        /// <returns>准备处理句柄。<para>Prepare handler.</para></returns>
        public void Prepare(string[] labels, Action onComplete, bool isConcurrent = false)
        {
            _labels = labels;
            _totalCount = 0;
            if (isConcurrent)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    var label = labels[i];
                    _assetLoader.LoadAsync<UnityEngine.Object>(label, _ =>
                    {
                        _totalCount++;
                        if (_totalCount >= labels.Length)
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
            if (index >= _labels.Length)
            {
                onComplete?.Invoke();
                return;
            }
            var label = _labels[index];
            _assetLoader.LoadAsync<UnityEngine.Object>(label, _ =>
            {
                _totalCount++;
                LoadNext(index + 1, onComplete);
            });
        }

        /// <summary>
        /// 卸载准备好的资源。
        /// <para>Cancel asset preparation.</para>
        /// </summary>
        void Unprepare()
        {
            _labels = null;
            _totalCount = 0;
        }


    }
}