using UnityEngine;

namespace PowerCellStudio
{
    public interface IUIComponent
    {
        /// <summary>
        /// 资源加载器，会在UI初始化时创建，在UI销毁时自动回收加载的资源
        /// The assetsLoader is created when the UI is initialized and automatically reclaims the loaded resources when the UI is destroyed
        /// </summary>
        public IAssetLoader assetsLoader { get;}
        
        public Transform transform { get; }
        
        public RectTransform rectTransform { get; }

        /// <summary>
        /// 当前UI是否显示
        /// Whether the UI is displayed
        /// </summary>
        public bool isOpened { get;}
        
        /// <summary>
        /// 在UI销毁时执行
        /// Executed on UI destruction
        /// </summary>
        public void OnUIDestroy();
        
        internal void Open(object data);
        
        /// <summary>
        /// 当UI组件打开时调用。
        /// Called when the UI component is open.
        /// </summary>
        /// <param name="data">打开UI组件时使用的数据。/ the data passed</param>
        public void OnOpen(object data);
        
        internal bool Close();
        
        /// <summary>
        /// 当UI组件关闭时调用。
        /// Call when the UI component is closed.
        /// </summary>
        public void OnClose();
        
        /// <summary>
        /// 当UI组件获得焦点时调用。
        /// Called when the UI component gains focus.
        /// </summary>
        public void OnFocus();
        
        /// <summary>
        /// 注册UI组件的事件。在OnOpen前调用 /
        /// Register events for UI component. Call before OnOpen. 
        /// </summary>
        public void RegisterEvent();
        
        /// <summary>
        /// 注销UI组件的事件。在OnClose前调用
        /// Remove the UI component events. Call before OnClose. 
        /// </summary>
        public void DeregisterEvent();
    }
}