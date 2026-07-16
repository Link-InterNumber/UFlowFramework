using System;

namespace PowerCellStudio
{
    
    /// <summary>
    /// 定义一个UI变量控制器接口，用于管理UI事件绑定、解绑以及生命周期方法。
    /// Defines an interface for UI variable controllers to manage UI event binding, unbinding, and lifecycle methods.
    /// </summary>
    public interface IUIVariableCtrl : IDisposable
    {
        /// <summary>
        /// 绑定UI事件。
        /// Binds UI events.
        /// </summary>
        /// <param name="eventHost"></param>
        void BindUIEvent(UIEventHost eventHost);

        /// <summary>
        /// 解绑UI事件。
        /// Unbinds UI events.
        /// </summary>
        /// <param name="eventHost"></param>
        void DisbindUIEvent(UIEventHost eventHost);

        /// <summary>
        /// 当UI打开时调用。
        /// Called when the UI is opened.
        /// </summary>
        /// <param name="data">传递给UI的数据。/Data passed to the UI.</param>
        void OnOpen(object data);

        /// <summary>
        /// 当UI关闭时调用。
        /// Called when the UI is closed.
        /// </summary>
        void OnClose();

        /// <summary>
        /// 当UI获取焦点时调用。
        /// Called when the UI gains focus.
        /// </summary>
        void OnFocus();
    }
    
    public abstract class UIVariableCtrl<T> :IUIVariableCtrl where T : UIVariableWindow
    {
        private T _ui;
        /// <summary>
        /// 当前控制的UI组件实例。
        /// </summary>
        protected T ctrlUI { get => _ui; private set => _ui = value; }

        public UIVariableCtrl(IUIComponent ui)
        {
            _ui = ui as T;
        }
        
        /// <summary>
        /// 注销控制器、清理资源、断开引用
        /// </summary>
        public virtual void Dispose()
        {
            _ui = null;
        }

        public abstract void BindUIEvent(UIEventHost eventHost);

        public abstract void DisbindUIEvent(UIEventHost eventHost);

        public abstract void OnOpen(object data);

        public abstract void OnClose();

        public abstract void OnFocus();
    }

    public interface IUIVariableUpdateCtrl : IUIVariableCtrl
    {
        void Update();
    }
}