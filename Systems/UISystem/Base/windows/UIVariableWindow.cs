using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public abstract class UIVariableWindow : UIWindow
    {

    }

    public abstract class UIVariableWindow<T> : UIVariableWindow
        where T : UIVariableWindow
    {
        private IUIVariableCtrl<T> _ctrl;
        private IUIVariableUpdateCtrl<T> _updateCtrl;

        protected IUIVariableCtrl<T> ctrl => _ctrl;
        private bool _needUpdate;

        protected abstract Type GetCtrlType(object data);
        protected abstract IUIVariableCtrl<T> CreateCtrl(object data);

        public override void OnOpen(object data)
        {
            if (ctrl != null && !GetCtrlType(data).Equals(typeof(_ctrl)))
            {
                _ctrl.DisbindUIEvent(this);
                _ctrl.Dispose();
                _ctrl = null;
                _updateCtrl = null;
            }
            if (ctrl == null)
            {
                _ctrl = CreateCtrl(data);
                _ctrl?.BindUIEvent(this);
                _updateCtrl = _ctrl as IUIVariableUpdateCtrl<T>;
                _needUpdate = _updateCtrl != null;
            }
            _ctrl?.OnOpen(data);
        }

        public override void OnClose()
        {
            _ctrl?.DisbindUIEvent(this);
            _ctrl?.OnClose(this);
        }
        
        public override void OnFocus()
        {
            _ctrl?.OnFocus(this);
        }

        public override void OnUIDestroy()
        {
            _ctrl?.Dispose();
            _ctrl = null;
            _updateCtrl = null;
            base.OnUIDestroy();
        }

        protected void Update()
        {
            if (!_needUpdate) return;
            _updateCtrl.Update(this);
        }
    }
}