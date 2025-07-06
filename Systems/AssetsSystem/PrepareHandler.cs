using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class PrepareHandler : CustomYieldInstruction, IDisposable
    {
        private List<object> _successLable;
        public List<object> successLable => _successLable;
        private Action _onComplete;
        private float _processValue;

        private bool _isDone;
        public bool isDone => _isDone;

        public override bool keepWaiting => !isDone;

        public PrepareHandler()
        {
            _successLable = new List<object>();
            _isDone = false;
        }

        public void Append(object lable)
        {
            _successLable.Add(lable);
        }

        public void SetProcessValue(float v)
        {
            _processValue = Mathf.Clamp01(v);
        }

        public void OnComplete(Action onComplete)
        {
            _onComplete = onComplete;
        }

        public void SetComplete()
        {
            _isDone = true;
            _processValue = 1f;
            _onComplete?.Invoke();
        }

        public void Dispose()
        {
            _isDone = true;
            _successLable = null;
            _onComplete = null;
            _processValue = 0;
        }
    }
}