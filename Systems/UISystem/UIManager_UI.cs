using System;
using System.Collections;
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
        private Dictionary<Type, IUIComponent> _cachedUIs = new Dictionary<Type, IUIComponent>();

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
            _poolPage.OnOpen(null);
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
                if (_cachedUIs.TryGetValue(typeof(T), out var cachedPage))
                {
                    _cachedUIs.Remove(typeof(T));
                    return cachedPage as T;
                }
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
        public bool IsAnyWindowOpening(IUIParent page)
        {
            if (page == null) return false;
            if (page.windowRequests != null && page.windowRequests.Count > 0)
            {
                return true;
            }
            return false;
        }

        private void SortingPage()
        {
            foreach (var page in _pageStack)
            {
                page.canvasCom.sortingOrder = (page.rectTransform.GetSiblingIndex() + 1) * 100;
            }
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
            var page = GetOrCreatePage<T>();
            if (page == null) return null;
            page.pushMode = pushMode;
            UIUtils.OpenUI(page, data);
            if (currentPage != null && currentPage.GetHashCode() == page.GetHashCode())
            {
                SortingPage();
                return currentPage as T;
            }
            _pageStack.Push(page);
            SortingPage();
            return page;
        }

        /// <summary>
        /// 弹出当前页面。
        /// Pop the current page.
        /// </summary>
        /// <param name="callback">回调函数 / Callback function</param>
        public void PopPage(Action callback = null)
        {
            // if (IsAnyWindowOpening(currentPage))
            // {
            //     UILog.LogError($"[{currentPage.transform.name}] is opening window(s), please wait!");
            //     return;
            // }
            if (_pageStack.Count < 2)
            {
                UILog.LogError("You must keep at least one page!");
                return;
            }
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
            TryCachePage(page, true, callback);
        }

        /// <summary>
        /// 关闭页面。
        /// Close a page.
        /// </summary>
        /// <typeparam name="T">页面类型 / Type of page</typeparam>
        /// <param name="destroy">是否销毁 / Whether to destroy</param>
        /// <param name="callback">回调函数 / Callback function</param>
        public void ClosePage<T>(bool destroy = true, Action callback = null) 
            where T : UIBehaviour, IUIParent
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
                // if (IsAnyWindowOpening(page))
                // {
                //     UILog.LogError($"[{page.transform.name}] is opening window(s), please wait!");
                //     return;
                // }
                _pageStack.Remove(page);
                TryCachePage(page, destroy, callback);
                SortingPage();
            }
        }

        private void TryCachePage<T>(T page, bool destroy, Action callback) 
            where T :  IUIParent
        {
            if (destroy && page is ICacheablePage cacheable && !_cachedUIs.ContainsKey(page.GetType()))
            {
                var retainTime = cacheable.retainTime;
                if (retainTime > 0)
                {
                    UIUtils.ClosePageInstance(page, false, callback, _poolPage);
                    var pageType = page.GetType();
                    _cachedUIs.Add(pageType, page);
                    AsyncManager.Run(WaitForRemoveCacheUI(pageType, retainTime));
                    return;
                }
            }
            UIUtils.ClosePageInstance(page, destroy, callback, _poolPage);
        }
        
        private IEnumerator WaitForRemoveCacheUI(Type uiType, float time)
        {
            yield return new WaitForSecondsRealtime(time);
            if (!_cachedUIs.TryGetValue(uiType, out var cachedPage)) yield break;
            _cachedUIs.Remove(uiType);
            var page = cachedPage as IUIParent;
            // while (IsAnyWindowOpening(page))
            // {
            //     yield return null;
            // }
            UIUtils.ClosePageInstance(cachedPage as IUIParent, true, null, _poolPage);
        }

        /// <summary>
        /// 获取当前页面上的窗口。
        /// Get a window from the current page.
        /// </summary>
        /// <typeparam name="T">窗口类型 / Type of window</typeparam>
        /// <param name="includeClosed">是否包括关闭的界面，默认包括 / Include closed windows, default is true</param>
        /// <returns>窗口实例, 如果没有则返回null / Instance of the window, or null if not found</returns>
        public T GetWindow<T>(bool includeClosed = true) where T : class, IUIChild
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
        public IAssetLoader GetAssetLoader<T>() where T : class, IUIChild
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
        public void OpenWindow<T>(object data = null, Action beforeOpen = null) where T : class, IUIChild
        {
            var windowType = typeof(T);
            if (typeof(IUIStandAlone).IsAssignableFrom(windowType))
            {
                _standAlonePage.OpenUI<T>(data, beforeOpen);
                return;
            }
            if (currentPage.GetUI<T>() == null && typeof(IUIPoolable).IsAssignableFrom(windowType))
            {
                _poolPage.OpenUI<T>(currentPage, data, () => BeforeOpenWindow(beforeOpen));
                return;
            }
            currentPage.OpenUI<T>(data, () => BeforeOpenWindow(beforeOpen));
        }

        private void BeforeOpenWindow(Action beforeOpen)
        {
            switch (currentPage.pushMode)
            {
                case PagePushMode.CloseOther:
                    foreach (var uiParent in _pageStack)
                    {
                        if (uiParent == currentPage) continue;
                        if (uiParent.isOpened)
                        {
                            UIUtils.ClosePageInstance(uiParent, false, null, _poolPage);
                        }
                    }
                    break;
                case PagePushMode.Replace:
                    if (_pageStack.Count > 0)
                    {
                        var top = _pageStack.Pop();
                        var second = _pageStack.Pop();
                        _pageStack.Push(top);
                        TryCachePage(second, true, null);
                    }
                    break;
                case PagePushMode.Overlap:
                    break;
            }
            beforeOpen?.Invoke();
        }

        /// <summary>
        /// 关闭当前页面上的窗口。
        /// Close a window on the current page.
        /// </summary>
        /// <typeparam name="T">窗口类型 / Type of window</typeparam>
        /// <param name="onClosed">关闭后的操作 / Actions after closing</param>
        /// <param name="destroy">是否关闭后销毁 / Whether to destroy after closing</param>
        public void CloseWindow<T>(Action onClosed = null, bool destroy = false) where T : class, IUIChild
        {
            var windowType = typeof(T);
            if (typeof(IUIStandAlone).IsAssignableFrom(windowType))
            {
                if (_standAlonePage.CloseUI<T>(onClosed) && destroy)
                {
                    var window = _standAlonePage.GetUI<T>();
                    UIUtils.RemoveChild(window);
                    UIUtils.DestroyUI(window, null);
                }
            }
            else if (currentPage.CloseUI<T>(onClosed))
            {
                if (destroy)
                {
                    var window = currentPage.GetUI<T>();
                    UIUtils.RemoveChild(window);
                    UIUtils.DestroyUI(window, null);
                }
                else if (typeof(IUIPoolable).IsAssignableFrom(windowType)
                         && _poolPage.GetUI<T>() == null 
                         && !_poolPage.IsUIGoingToOpen<T>(out _))
                {
                    var window = currentPage.GetUI<T>();
                    UIUtils.RemoveChild(window);
                    UIUtils.SetUIChildToParent(window, _poolPage);
                }
            }
        }

        /// <summary>
        /// 销毁关闭的UI。
        /// Close and destroy any unused UI.
        /// </summary>
        public void Clear()
        {
            var pages = _pageStack.ToArray();
            foreach (var uiParent in pages)
            {
                ClearClosedWindow(uiParent);

                // if (uiParent.isOpened) 
                // {
                //     ClearClosedWindow(uiParent);
                //     continue;
                // }
                // _pageStack.Remove(uiParent);
                // UIUtils.ClosePage(uiParent, true, null, _poolPage);
            }
            ClearClosedWindow(_poolPage);
            ClearClosedWindow(_standAlonePage);
        }

        /// <summary>
        /// 销毁关闭的UI。
        /// Close and destroy any unused UI.
        /// </summary>
        /// <param name="page">页面对象 / Page instance</param>
        public void ClearClosedWindow(IUIParent page)
        {
            if (page == null) return;
            var windows = page.children.Values.ToArray();
            foreach (var uiChild in windows)
            {
                if (uiChild.isOpened) continue;
                UIUtils.RemoveChild(uiChild);
                UIUtils.DestroyUI(uiChild, null);
            }
        }
    }
}