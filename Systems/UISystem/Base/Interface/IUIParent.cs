using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public interface IUIParent : IUIComponent
    {
        internal HashStack<IUIChild> openedUIs { get; }
        
        internal Dictionary<Type, IUIChild> children { get; }
        
        internal OpenWindowRequestHolder windowRequests { get; set; }

        /// <summary>
        /// 在Page中打开UI
        /// </summary>
        /// <param name="data">打开UI时传入的数据</param>
        /// <param name="beforeOpen">UI开启前执行的回调</param>
        /// <typeparam name="T">UI类</typeparam>
        public void OpenUI<T>(object data, Action beforeOpen = null) where T : UIBehaviour, IUIChild;
        
        /// <summary>
        /// 预加载界面
        /// </summary>
        /// <typeparam name="T">UI类</typeparam>
        public void PreloadUI<T>() where T : UIBehaviour, IUIChild;
        
        /// <summary>
        /// 关闭界面
        /// </summary>
        /// <param name="onClosed"></param>
        /// <typeparam name="T">关闭后执行的回调</typeparam>
        /// <returns>是否成功关闭界面</returns>
        public bool CloseUI<T>(Action onClosed = null) where T : UIBehaviour, IUIChild;
        
        internal bool CloseUI<T>(T uiChild, Action onClosed = null) where T : UIBehaviour, IUIChild;
        
        /// <summary>
        /// 获取已经加载的UI
        /// </summary>
        T GetUI<T>() where T : UIBehaviour, IUIChild;
        
        /// <summary>
        /// 获取打开的UI
        /// </summary>
        T GetOpenedUI<T>() where T : UIBehaviour, IUIChild;

        /// <summary>
        /// 界面是否在加载中
        /// </summary>
        public bool IsUIGoingToOpen<T>(out IOpenWindowRequest request) where T : UIBehaviour, IUIChild;
        
        /// <summary>
        /// 获取最上层的UI
        /// </summary>
        public IUIChild GetTopUI();
    }
}