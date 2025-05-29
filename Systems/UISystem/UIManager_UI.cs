using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public partial class UIManager
    {
        private HashStack<IUIParent> _pageStack = new HashStack<IUIParent>();
        private PoolWindowPage _poolPage;
        private IUIParent _standAlonePage;

        /// <summary>
        /// 获取当前页面。
        /// Get the current page.
        /// </summary>
        public IUIParent currentPage => _pageStack.Count > 0 ? _pageStack.Peek() : null;

        private RenderMode _canvasRenderMode = RenderMode.ScreenSpaceCamera;

        /// <summary>
        /// 获取或设置画布渲染模式。
        /// Get or set the canvas render mode.
        /// </summary>
        public RenderMode canvasRenderMode
        {
            get => _canvasRenderMode;
            set
            {
                if (_canvasRenderMode == value) return;
                _canvasRenderMode = value;
                UICamera.instance.cameraCom.gameObject.SetActive(_canvasRenderMode != RenderMode.ScreenSpaceOverlay);
                foreach (var uiPage in _pageStack)
                {
                    var page = uiPage as IUIParent;
                    if (page == null) continue;
                    foreach (var uiSystemChild in page.openedUIs)
                    {
                        var canvas = uiSystemChild.rectTransform.GetComponent<Canvas>();
                        if (!canvas)
                            continue;
                        canvas.renderMode = _canvasRenderMode;
                        if (_canvasRenderMode != RenderMode.ScreenSpaceOverlay)
                            canvas.worldCamera = UICamera.instance.cameraCom;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化UI管理器。
        /// Initialize the UI Manager.
        /// </summary>
        private void Init()
        {
            _standAlonePage = StandAlonePage.Create(transform, canvasRenderMode);
            _poolPage = UIUtils.CreatePage<PoolWindowPage>(transform, canvasRenderMode);
            _poolPage.transform.gameObject.SetActive(false);
        }

        /// <summary>
        /// 获取页面。
        /// Get a page.
        /// </summary>
        /// <typeparam name="T">页面类型 / Type of page</typeparam>
        /// <returns>页面实例 / Instance of the page, return null if not found</returns>
        public T GetPage<T>() where T : UIBehaviour, IUIParent
        {
            foreach (var page in _pageStack)
            {
                if (page is T casedPage) return casedPage;
            }
            return null;
        }

        /// <summary>
        /// 获取或创建页面。
        /// Get or create a page.
        /// </summary>
        /// <typeparam name="T">页面类型 / Type of page</typeparam>
        /// <returns>页面实例 / Instance of the page</returns>
        private T GetOrCreatePage<T>() where T : UIBehaviour, IUIParent
        {
            var page = GetPage<T>();
            if (page == null || typeof(T) == typeof(TempPage))
            {
                return UIUtils.CreatePage<T>(transform, canvasRenderMode);
            }
            return page as T;
        }

        /// <summary>
        /// 检查是否有窗口正在打开。
        /// Check if any window is opening.
        /// </summary>
        /// <param name="page">页面实例 / Page instance</param>
        /// <returns>是否有窗口正在打开 / Whether any windows are opening</returns>
        private bool IsAnyWindowOpening(IUIParent page)
        {
            if (page == null) return false;
            if (page.windowRequests != null && page.windowRequests.Count > 0)
            {
                UILog.LogError($"[{page.transform.name}] is opening window(s), please wait!");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 在page堆顶层推入页面。
        /// Push a page onto the top of the stack.
        /// </summary>
        /// <typeparam name="T">页面类型 / Type of page</typeparam>
        /// <param name="data">页面数据 / Page data</param>
        /// <param name="pushMode">页面的开启方式 / Page opening mode: CloseOther, Replace, or Overlap</param>
        /// <returns>页面实例 / Instance of the page</returns>
        public T PushPage<T>(object data = null, PagePushMode pushMode = PagePushMode.CloseOther) where T : UIBehaviour, IUIParent
        {
            if (IsAnyWindowOpening(currentPage))
            {
                return null;
            }
            var page = GetOrCreatePage<T>();
            if (page == null) return null;
            page.pushMode = pushMode;
            if (currentPage != null && currentPage.GetHashCode() == page.GetHashCode())
            {
                UIUtils.OpenUI(currentPage, data);
                return currentPage as T;
            }
            if (pushMode == PagePushMode.Replace && _pageStack.Count > 1)
            {
                var pageToClose = _pageStack.Pop();
                UIUtils.ClosePage(pageToClose, true, null, _poolPage);
            }
            _pageStack.Push(page);
            UIUtils.OpenUI(page, data);
            return page;
        }

        /// <summary>
        /// 弹出当前页面。
        /// Pop the current page.
        /// </summary>
        /// <param name="callback">回调函数 / Callback function</param>
        public void PopPage(Action callback = null)
        {
            if (IsAnyWindowOpening(currentPage))
            {
                return;
            }
            if (_pageStack.Count < 2) return;
            var page = _pageStack.Pop();
            if (currentPage.pushMode == PagePushMode.Overlap)
            {
                var tempStack = new Stack<IUIParent>();
                tempStack.Push(_pageStack.Pop());

                while (_pageStack.Count > 0)
                {
                    var tempPage = _pageStack.Peek();
                    if (tempPage.pushMode != PagePushMode.Overlap)
                        break;
                    tempStack.Push(_pageStack.Pop());
                }
                tempStack.Push(_pageStack.Pop());
                
                while (tempStack.Count > 0)
                {
                    var tempPage = tempStack.Pop();
                    tempPage.transform.gameObject.SetActive(true);
                    tempPage.OnFocus();
                    foreach (var parentOpenedUI in tempPage.openedUIs)
                    {
                        parentOpenedUI.OnFocus();
                    }
                    _pageStack.Push(tempPage);
                }
            }
            else
            {
                currentPage.transform.gameObject.SetActive(true);
                currentPage.OnFocus();
                foreach (var parentOpenedUI in currentPage.openedUIs)
                {
                    parentOpenedUI.OnFocus();
                }
            }
            UIUtils.ClosePage(page, true, callback, _poolPage);
        }

        /// <summary>
        /// 关闭页面。
        /// Close a page.
        /// </summary>
        /// <typeparam name="T">页面类型 / Type of page</typeparam>
        /// <param name="destroy">是否销毁 / Whether to destroy</param>
        /// <param name="callback">回调函数 / Callback function</param>
        public void ClosePage<T>(bool destroy = true, Action callback = null) where T : UIBehaviour, IUIParent
        {
            if (_pageStack.Count < 2)
            {
                UILog.LogError("You must keep at least one page!");
                return;
            }
            if (currentPage.GetType() == typeof(T))
            {
                PopPage(callback);
            }
            else
            {
                var page = GetPage<T>();
                if (page == null) return;
                if (IsAnyWindowOpening(page))
                {
                    return;
                }
                _pageStack.Remove(page);
                UIUtils.ClosePage(page, destroy, callback, _poolPage);
            }
        }

        /// <summary>
        /// 获取当前页面上的窗口。
        /// Get a window from the current page.
        /// </summary>
        /// <typeparam name="T">窗口类型 / Type of window</typeparam>
        /// <param name="includeClosed">是否包括关闭的界面，默认包括 / Include closed windows, default is true</param>
        /// <returns>窗口实例, 如果没有则返回null / Instance of the window, or null if not found</returns>
        public T GetWindow<T>(bool includeClosed = true) where T : UIBehaviour, IUIChild
        {
            var windowType = typeof(T);
            if (typeof(IUIStandAlone).IsAssignableFrom(windowType))
            {
                return includeClosed ? _standAlonePage.GetUI<T>() : _standAlonePage.GetOpenedUI<T>();
            }
            return includeClosed ? currentPage.GetUI<T>() : currentPage.GetOpenedUI<T>();
        }

        /// <summary>
        /// 获取当前窗口上的资源加载器。
        /// Get the assetsLoader from window.
        /// </summary>
        /// <typeparam name="T">窗口类型 / Type of window</typeparam>
        /// <returns>资源加载器, 如果没有则返回null / Instance of the assetsLoader of window, or null if not found</returns>
        public IAssetLoader GetAssetLoader<T>() where T : UIBehaviour, IUIChild
        {
            return GetWindow<T>()?.assetsLoader ?? null;
        }

        /// <summary>
        /// 在当前页面打开窗口。
        /// Open a window on the current page.
        /// </summary>
        /// <typeparam name="T">窗口类型 / Type of window</typeparam>
        /// <param name="data">窗口数据 / Window data</param>
        /// <param name="beforeOpen">打开前的操作 / Actions before opening</param>
        public void OpenWindow<T>(object data = null, Action beforeOpen = null) where T : UIBehaviour, IUIChild
        {
            var windowType = typeof(T);
            if (typeof(IUIStandAlone).IsAssignableFrom(windowType))
            {
                _standAlonePage.OpenUI<T>(data, beforeOpen);
                return;
            }
            if (!currentPage.GetUI<T>() && typeof(IUIPoolable).IsAssignableFrom(windowType))
            {
                _poolPage.OpenUI<T>(currentPage, data, beforeOpen);
                return;
            }
            currentPage.OpenUI<T>(data, beforeOpen);
        }

        /// <summary>
        /// 关闭当前页面上的窗口。
        /// Close a window on the current page.
        /// </summary>
        /// <typeparam name="T">窗口类型 / Type of window</typeparam>
        /// <param name="onClosed">关闭后的操作 / Actions after closing</param>
        /// <param name="destroy">是否关闭后销毁 / Whether to destroy after closing</param>
        public void CloseWindow<T>(Action onClosed = null, bool destroy = false) where T : UIBehaviour, IUIChild
        {
            if (typeof(IUIStandAlone).IsAssignableFrom(typeof(T)))
            {
                if (_standAlonePage.CloseUI<T>(onClosed) && destroy)
                {
                    var window = _standAlonePage.GetUI<T>();
                    UIUtils.RemoveChild(window);
                    UIUtils.DestroyUI(window, null);
                }
            }
            else if (currentPage.CloseUI<T>(onClosed) && destroy)
            {
                var window = currentPage.GetUI<T>();
                UIUtils.RemoveChild(window);
                UIUtils.DestroyUI(window, null);
            }
        }

        /// <summary>
        /// 当UI窗口打开时，将之前没关闭的Page关闭。
        /// When a UI window opens, close any previous pages that weren't closed.
        /// </summary>
        /// <param name="data">窗口数据 / Window data</param>
        private void OnUIWindowOpened(IUIChild data)
        {
            if (_pageStack.Count < 2 || currentPage.pushMode == PagePushMode.Overlap) return;
            var index = 0;
            foreach (var uiParent in _pageStack)
            {
                if (index > 0 && uiParent.isOpened && !IsAnyWindowOpening(uiParent))
                {
                    UIUtils.ClosePage(uiParent, false, null, _poolPage);
                }
                index++;
            }
        }

        /// <summary>
        /// 关闭并销毁未打开的UI。
        /// Close and destroy any unused UI.
        /// </summary>
        public void Clear()
        {
            var pages = _pageStack.Where(o => o != currentPage).ToArray();
            foreach (var uiParent in pages)
            {
                if (uiParent.isOpened || currentPage == uiParent) continue;
                _pageStack.Remove(uiParent);
                UIUtils.ClosePage(uiParent, true, null, _poolPage);
            }

            var poolParent = (_poolPage as IUIParent);
            foreach (var uiChild in poolParent.children)
            {
                UIUtils.RemoveChild(uiChild.Value);
                UIUtils.DestroyUI(uiChild.Value, null);
            }
        }
    }
}