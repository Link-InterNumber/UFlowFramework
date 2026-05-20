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
            var showWaiting = _showWaiting || (maskWindowData?.showWaiting ?? false);
            goWaiting.SetActive(showWaiting);
            if (maskWindowData.canClose != null)
            {
                AddWaitingCount();
                _waitingQueue.Enqueue(maskWindowData.canClose);
            }
            if (maskWindowData.yieldInstruction != null)
            {
                AddWaitingCount();
                AsyncManager.Run(Wait(maskWindowData.yieldInstruction));
            }
            if (maskWindowData.canClose == null && maskWindowData.yieldInstruction == null)
                _emptyCount++;
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
            if (_waitingCount || _emptyCount)
            {
                return false;
            }
            return true;
        }

        public static void Open(Func<bool> canClose, bool showWaiting = true)
        {
            var maskWindowData = new MaskWindow.MaskWindowData(showWaiting, canClose, null);
            UIManager.instance.OpenWindow<MaskWindow>(maskWindowData);
        }
        
        public static void Open(YieldInstruction yieldInstruction, bool showWaiting = true)
        {
            var maskWindowData = new MaskWindow.MaskWindowData(showWaiting, null, yieldInstruction);
            UIManager.instance.OpenWindow<MaskWindow>(maskWindowData);
        }
        
        public static void Open(float realTime, bool showWaiting = true)
        {
            var newWaitForSeconds = new WaitForSeconds(realTime);
            var maskWindowData = new MaskWindow.MaskWindowData(showWaiting, null, newWaitForSeconds);
            UIManager.instance.OpenWindow<MaskWindow>(maskWindowData);
        }
    }
}