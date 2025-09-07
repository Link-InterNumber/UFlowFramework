using System;

namespace PowerCellStudio
{
    public abstract class UIVariableWindow : UIWindow
    {
        private IUIVariableCtrl _ctrl;
        protected IUIVariableCtrl UICtrl => _ctrl;
        private IUIVariableUpdateCtrl _updateCtrl;
        private bool _needUpdate;

        /// <summary>
        /// 根据输入数据获取控制器类型
        /// </summary>
        /// <param name="data">输入数据</param>
        /// <returns>控制器类型</returns>
        protected abstract Type GetCtrlType(object data);

        /// <summary>
        /// 根据输入数据创建控制器
        /// </summary>
        /// <param name="ctrlType">控制器类型</param>
        /// <returns>创建的控制器实例</returns>
        protected virtual IUIVariableCtrl CreateCtrl(Type ctrlType)
        {
            // CN:使用反射创建控制器实例，在构造函数中传入当前UI窗口实例
            // EN: Use reflection to create a controller instance, passing the current UI window instance in the constructor
            var constructor = ctrlType.GetConstructor(new Type[] { typeof(IUIComponent) });
            if (constructor != null)
            {
                return constructor.Invoke(new object[] { this }) as IUIVariableCtrl;
            }
            UILog.LogError($"Unable to create controller instance, type: {ctrlType.Name}, please ensure that the type has a constructor that accepts an IUIComponent parameter.");
            return null;
        }

        public override void OnOpen(object data)
        {
            // CN:如果已有控制器且类型不匹配，则释放旧控制器
            // EN: If there is already a controller and the type does not match, release the old controller
            var ctrlType = GetCtrlType(data);
            if (_ctrl != null && ctrlType != _ctrl.GetType())
            {
                _ctrl.DisbindUIEvent();
                _ctrl.Dispose();
                _ctrl = null;
                _updateCtrl = null;
            }

            // CN:如果控制器为空，则创建新的控制器
            // EN: If the controller is null, create a new controller
            if (_ctrl == null)
            {
                _ctrl = CreateCtrl(ctrlType);
                _ctrl?.BindUIEvent();
                _updateCtrl = _ctrl as IUIVariableUpdateCtrl;
                _needUpdate = _updateCtrl != null;
            }

            // CN:执行控制器的打开逻辑
            // EN: Execute the controller's open logic
            _ctrl?.OnOpen(data);
        }

        public override void OnClose()
        {
            if (_ctrl == null) return;
            _ctrl.OnClose();
        }

        public override void OnFocus()
        {
            _ctrl?.OnFocus();
        }

        public override void OnUIDestroy()
        {
            _ctrl?.DisbindUIEvent();
            _ctrl?.Dispose();
            _ctrl = null;
            _updateCtrl = null;
            base.OnUIDestroy();
        }

        protected void Update()
        {
            if (!_needUpdate) return;
            _updateCtrl.Update();
        }
    }
}