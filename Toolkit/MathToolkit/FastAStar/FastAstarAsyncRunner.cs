using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class FastAstarAsyncRunner : MonoBehaviour
    {
        private struct AsyncTask
        {
            public FastAStar.FastAStarJobContext context;
            public Action<Vector2Int[]> onCompleted;
        }
        
        private List<AsyncTask> _tasks;

        private HashSet<FastAStar> _refs = new HashSet<FastAStar>();
        
        internal void AddRef(FastAStar fastAStar)
        {
            _refs.Add(fastAStar);
        }
        
        internal void RemoveRef(FastAStar fastAStar)
        {
            _refs.Remove(fastAStar);
            
            TryDispose();
        }
        
        internal void Push(FastAStar.FastAStarJobContext context, Action<Vector2Int[]> onCompleted)
        {
            if (_tasks == null)
            {
                _tasks = new List<AsyncTask>();
            }
            _tasks.Add(new AsyncTask { context = context, onCompleted = onCompleted });
        }

        private void Update()
        {
            if (_tasks == null || _tasks.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _tasks.Count;)
            {
                var context = _tasks[i].context;
                if (context.WaitForCompletion(out var ints))
                {
                    _tasks[i].onCompleted?.Invoke(ints);
                    _tasks.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            TryDispose();
        }

        private void TryDispose()
        {
            if (_refs.Count == 0 && (_tasks == null || _tasks.Count == 0))
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (FastAStar._runner == this)
            {
                FastAStar._runner = null;
            }
        }
    }
}