using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public enum PagePushMode
    {
        /// <summary>
        /// 关闭其他页面
        /// Close other pages
        /// </summary>
        CloseOther,
        /// <summary>
        /// 替换当前页面
        /// Replace current page
        /// </summary>
        Replace,
        /// <summary>
        /// 覆盖在当前页面之上
        /// Overlap on top of current page
        /// </summary>
        Overlap
    }

    public interface IUIParent : IUIComponent
    {
        public Canvas canvasCom { get; set; }

        internal HashStack<IUIChild> openedUIs { get; }
        
        internal Dictionary<Type, IUIChild> children { get; }
        
        internal OpenWindowRequestHolder windowRequests { get; set; }

        public PagePushMode pushMode {get; set;}

        /// <summary>
        /// 在Page中打开UI
        /// Open a UI in the Page
        /// </summary>
        /// <param name="data">打开UI时传入的数据 / Data passed when opening the UI</param>
        /// <param name="beforeOpen">UI开启前执行的回调 / Callback executed before UI opens</param>
        /// <typeparam name="T">UI类 / UI class</typeparam>
        public void OpenUI<T>(object data, Action beforeOpen = null) where T : class, IUIChild;
        
        /// <summary>
        /// 预加载界面
        /// Preload the UI
        /// </summary>
        /// <typeparam name="T">UI类 / UI class</typeparam>
        public void PreloadUI<T>() where T : class, IUIChild;
        
        /// <summary>
        /// 关闭界面
        /// Close the UI
        /// </summary>
        /// <param name="onClosed">关闭后执行的回调 / Callback after closing</param>
        /// <typeparam name="T">关闭后执行的回调 / UI class</typeparam>
        /// <returns>是否成功关闭界面 / Whether the UI was closed successfully</returns>
        public bool CloseUI<T>(Action onClosed = null) where T : class, IUIChild;
        
        internal bool CloseUI<T>(T uiChild, Action afterClosed = null) where T : class, IUIChild;
        
        /// <summary>
        /// 获取已经加载的UI
        /// Get the loaded UI
        /// </summary>
        T GetUI<T>() where T : class, IUIChild;
        
        /// <summary>
        /// 获取打开的UI
        /// Get the opened UI
        /// </summary>
        T GetOpenedUI<T>() where T : class, IUIChild;

        /// <summary>
        /// 界面是否在加载中
        /// Whether the UI is being loaded
        /// </summary>
        public bool IsUIGoingToOpen<T>(out IOpenWindowRequest request) where T : class, IUIChild;
        
        /// <summary>
        /// 获取最上层的UI
        /// Get the top UI
        /// </summary>
        public IUIChild GetTopUI();
    }
}