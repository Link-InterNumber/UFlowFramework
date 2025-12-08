using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class OpenWindowRequestHolder
    {
        private List<IOpenWindowRequest> _windowRequests = new List<IOpenWindowRequest>();

        public int Count => _windowRequests.Count;

        public void AddWindowRequest(IOpenWindowRequest request)
        {
            if (request == null) return;
            _windowRequests.Add(request);
            TriggerRequest();
        }

        private void TriggerRequest()
        {
            if (_windowRequests.Count == 0) return;
            while (_windowRequests.Count > 0)
            {
                var request = _windowRequests[0];
                if (request.assetLoadStatus == AssetLoadStatus.Loaded)
                {
                    _windowRequests.RemoveAt(0);
                    continue;
                }
                if (request.assetLoadStatus == AssetLoadStatus.Loading)
                {
                    break;
                }
                request.OnLoaded(TriggerRequest);
                request.Load();
                break;
            }
        }

        public bool IsUIGoingToOpen(Type windowType, out IOpenWindowRequest request)
        {
            request = null;
            if (_windowRequests == null || _windowRequests.Count == 0)
            {
                return false;
            }
            for (var i = 0; i < _windowRequests.Count; i++)
            {
                var req = _windowRequests[i];
                if (req.currentWindowType == windowType)
                {
                    request = req;
                    return true;
                }
            }
            return false;
        }

        public bool IsUIGoingToOpen<T>(out IOpenWindowRequest request) where T : class, IUIChild
        {
            request = null;
            if (_windowRequests == null || _windowRequests.Count == 0)
            {
                return false;
            }
            var type = typeof(T);
            for (var i = 0; i < _windowRequests.Count; i++)
            {
                var req = _windowRequests[i];
                if (req.currentWindowType == type)
                {
                    request = req;
                    return true;
                }
            }
            return false;
        }
    }
}