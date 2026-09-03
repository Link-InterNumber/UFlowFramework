using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class LoadSamplePool : IDisposable
    {
        private Stack<LoadSample> _loadSamples ;
        private int _maxPoolSize = 100;

        public LoadSamplePool(int maxSize = 100)
        {
            _maxPoolSize = Mathf.Max(1, maxSize);
            _loadSamples = new Stack<LoadSample>();
        }

        public void Dispose()
        {
            foreach (var sample in _loadSamples)
            {
                sample.Reset();
            }
            _loadSamples.Clear();
            _loadSamples = null;
        }

        public LoadSample Get()
        {
            if (_loadSamples.Count > 0)
            {
                return _loadSamples.Pop();
            }
            return new LoadSample();
        }

        public void Release(LoadSample sample)
        {
            if (sample == null)
            {
                return;
            }
            sample.Reset();
            if (_loadSamples.Count >= _maxPoolSize)
                return;
            _loadSamples.Push(sample);
        }
    }
}