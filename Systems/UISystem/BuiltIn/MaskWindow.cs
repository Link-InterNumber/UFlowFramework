using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [WindowInfo("Assets/Res/UI/MaskWindow.prefab")]
    public class MaskWindow : UIWindow, IUIStandAlone, IUIComponent
    {
        public class MaskWindowData
        {
            public MaskWindowData(bool showWaiting, Func<bool> canClose, YieldInstruction yieldInstruction)
            {
                this.showWaiting = showWaiting;
                this.canClose = canClose;
                this.yieldInstruction = yieldInstruction;
            }
            
            public bool showWaiting;
            public Func<bool> canClose;
            public YieldInstruction yieldInstruction;
        }
        
        public GameObject goWaiting;
        
        private RefCountBool _waitingCount = new RefCountBool();
        private RefCountBool _emptyCount = new RefCountBool();
        private bool _showWaiting = false;

        private Queue<Func<bool>> _waitingQueue = new Queue<Func<bool>>();

        public override void OnOpen(object data)
        {
            var maskWindowData = data as MaskWindowData;
            if (maskWindowData == null)
            {
                _emptyCount++;
                return;
            }
            AddWaitingCount();
            var showWaiting = _showWaiting || (maskWindowData?.showWaiting ?? false);
            goWaiting.SetActive(showWaiting);
            if (maskWindowData.canClose != null)
            {
                _waitingQueue.Enqueue(maskWindowData.canClose);
            }
            if (maskWindowData.yieldInstruction != null)
            {
                ApplicationManager.instance.StartCoroutine(Wait(maskWindowData.yieldInstruction));
            }
        }
        
        private void AddWaitingCount()
        {
            _waitingCount++;
        }
        
        private void DeWaitingCount()
        {
            _waitingCount--;
            if (_waitingCount) return;
            CloseUI(null);
        }

        private IEnumerator Wait(YieldInstruction yieldInstruction)
        {
            yield return yieldInstruction;
            DeWaitingCount();
        }

        private void Update()
        {
            if (_waitingQueue.Count == 0) return;
            if (!_waitingQueue.Peek()()) return;
            _waitingQueue.Dequeue();
            DeWaitingCount();
        }

        public void ForceClose()
        {
            _waitingCount.Clear();
            _emptyCount.Clear();
            CloseUI(null);
        }

        public override void OnClose()
        {
            _waitingCount.Clear();
            _emptyCount.Clear();
            _showWaiting = false;
            _waitingQueue.Clear();
        }

        public override void OnFocus()
        {
            
        }

        bool IUIComponent.Close()
        {
            _emptyCount--;
            if (_waitingCount || _emptyCount > 0)
            {
                return false;
            }
            OnClose();
            return true;
        }
    }
}