using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class ChainedLoadingLoader : CustomYieldInstruction, IDisposable
    {
        private IAssetLoader _loader;

        private Queue<Action> _loadTaskChain;

        private event Action _onChainComplete;
        private bool _isDone;
        private bool _isLoading ;
        private bool _isCanceled;

        public override bool keepWaiting => !_isCanceled && !_isDone;

        public ChainedLoadingLoader(IAssetLoader loader)
        {
            _loader = loader;
            _loadTaskChain = new Queue<Action>();
            _isCanceled = false;
            _isLoading = false;
            _isDone = false;
        }

        public void PushLoadTask<T>(string address, OnLoadSuccess<T> onSuccess, OnLoadFailed onFail = null) where T : UnityEngine.Object
        {
            var loadTask = new Action(() =>
            {
                _loader.LoadAsync<T>(address, 
                asset =>
                {
                    onSuccess?.Invoke(asset);
                    TriggerNext();
                }, 
                () =>
                {
                    onFail?.Invoke();
                    AssetLog.LogError($"Chained Loading failed at loading asset at address: {address}");
                    TriggerNext();
                });
            });
            _loadTaskChain.Enqueue(loadTask);
            _isDone = false;
            if (!_isLoading)
            {
                _isLoading = true;
                TriggerNext();
            }
        }

        public void TriggerNext()
        {
            if (_isCanceled) return;
            if (_loadTaskChain.Count > 0)
            {
                var nextTask = _loadTaskChain.Dequeue();
                nextTask.Invoke();
            }
            else
            {
                _isLoading = false;
                _isDone = true;
                _onChainComplete?.Invoke();
            }
        }

        public void OnComplete(Action onComplete)
        {
            _onChainComplete += onComplete;
        }

        public void Cancel()
        {
            _isCanceled = true;
        }

        public void Dispose()
        {
            _loader = null;
            _isCanceled = true;
            _onChainComplete = null;
            _loadTaskChain.Clear();
            _loadTaskChain = null;
        }
    }
}