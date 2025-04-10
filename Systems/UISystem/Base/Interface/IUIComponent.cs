using UnityEngine;

namespace PowerCellStudio
{
    public interface IUIComponent
    {
        /// <summary>
        /// 资源加载器，会在UI初始化时创建，在UI销毁时自动回收加载都资源
        /// </summary>
        public IAssetLoader assetsLoader { get;}
        
        public Transform transform { get; }
        
        public RectTransform rectTransform { get; }

        /// <summary>
        /// 当前UI是否显示
        /// </summary>
        public bool isOpened { get;}
        
        /// <summary>
        /// 在UI销毁时执行
        /// </summary>
        public void OnUIDestroy();
        
        internal void Open(object data);
        
        /// <summary>
        /// 当UI组件打开时调用。
        /// </summary>
        /// <param name="data">打开UI组件时使用的数据。</param>
        public void OnOpen(object data);
        
        internal bool Close();
        
        /// <summary>
        /// 当UI组件关闭时调用。
        /// </summary>
        public void OnClose();
        
        /// <summary>
        /// 当UI组件获得焦点时调用。
        /// </summary>
        public void OnFocus();
        
        /// <summary>
        /// 注册UI组件的事件。
        /// </summary>
        public void RegisterEvent();
        
        /// <summary>
        /// 注销UI组件的事件。
        /// </summary>
        public void DeregisterEvent();
    }
}