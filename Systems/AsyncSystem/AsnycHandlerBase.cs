using System;
using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class AsyncHandlerBase : CustomYieldInstruction, IAsyncHandler
    {
        protected Action _onComplete;
        public abstract void Cancel();

        public void OnComplete(Action callback)
        {
            _onComplete += callback;
        }
    }

    public interface IAsyncHandler : IEnumerator, ICancelable
    {
        void OnComplete(Action callback);
    }
}