using System;
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
        private PagePushMode _currentPagePushMode;

        /// <summary>
        /// 获取当前页面。
        /// </summary>
        public IUIParent currentPage => _pageStack.Count > 0 ? _pageStack.Peek() : null;

        private RenderMode _canvasRenderMode = RenderMode.ScreenSpaceCamera;

        /// <summary>
        /// 获取或设置画布渲染模式。
        /// </summary>
        public RenderMode canvasRenderMode
        {
            get => _canvasRenderMode;
            set
            {
                if(_canvasRenderMode == value) return;
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
        /// </summary>
        private void Init()
        {
            _standAlonePage = StandAlonePage.Create(transform, canvasRenderMode);
            _poolPage = UIUtils.CreatePage<PoolWindowPage>(transform, canvasRenderMode);
            _poolPage.transform.gameObject.SetActive(false);
        }

        /// <summary>
        /// 获取页面。
        /// </summary>
        /// <typeparam name="T">页面类型。</typeparam>
        /// <returns>页面实例，未找到则返回null。</returns>
        public T GetPage<T>() where T : UIBehaviour, IUIParent
        {
            var page = _pageStack.LastOrDefault(x => x is T);
            return page as T;
        }

        /// <summary>
        /// 获取或创建页面。
        /// </summary>
        /// <typeparam name="T">页面类型。</typeparam>
        /// <returns>页面实例。</returns>
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
        /// </summary>
        /// <param name="page">页面实例。</param>
        /// <returns>是否有窗口正在打开。</returns>
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
        /// </summary>
        /// <typeparam name="T">页面类型。</typeparam>
        /// <param name="data">页面数据。</param>
        /// <param name="pushMode">页面的开启方式。CloseOther：会在之后窗口打开时关闭之前的页面；Replace：会将当前Page销毁，Overlap：和之前的页面重叠打开</param>
        /// <returns>页面实例。</returns>
        public T PushPage<T>(object data = null, PagePushMode pushMode = PagePushMode.CloseOther) where T : UIBehaviour, IUIParent
        {
            if (IsAnyWindowOpening(currentPage))
            {
                return null;
            }
            var page = GetOrCreatePage<T>();
            if (page == null) return null;
            _currentPagePushMode = pushMode;
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
        /// </summary>
        /// <param name="callback">回调函数。</param>
        public void PopPage(Action callback = null)
        {
            if (IsAnyWindowOpening(currentPage))
            {
                return;
            }
            if (_pageStack.Count < 2) return;
            var page = _pageStack.Pop();
            currentPage.transform.gameObject.SetActive(true);
            currentPage.OnFocus();
            foreach (var parentOpenedUI in currentPage.openedUIs)
            {
                parentOpenedUI.OnFocus();
            }
            UIUtils.ClosePage(page, true, callback, _poolPage);
        }

        /// <summary>
        /// 关闭页面。
        /// </summary>
        /// <typeparam name="T">页面类型。</typeparam>
        /// <param name="destroy">是否销毁。</param>
        /// <param name="callback">回调函数。</param>
        public void ClosePage<T>(bool destroy = true, Action callback = null) where T : UIBehaviour, IUIParent
        {
            if (_pageStack.Count < 2)
            {
                UILog.LogError("Your must keep at least *one* page!");
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
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <param name="includeClosed">是否包括关闭的界面，默认包括。</param>
        /// <returns>窗口实例，当前界面上没有该类型窗口则返回null。</returns>
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
        /// 在当前页面打开窗口。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <param name="data">窗口数据。</param>
        /// <param name="beforeOpen">打开前的操作。</param>
        public void OpenWindow<T>(object data = null, Action beforeOpen = null) where T : UIBehaviour, IUIChild
        {
            var windowType = typeof(T);
            if (typeof(IUIStandAlone).IsAssignableFrom(windowType))
            {
                _standAlonePage.OpenUI<T>(data, beforeOpen);
                return;
            }
            // 如果T是IUIPoolable类型，且当前页面没有该UI，则从池中取出
            if (!currentPage.GetUI<T>() && typeof(IUIPoolable).IsAssignableFrom(windowType))
            {
                _poolPage.OpenUI<T>(currentPage, data, beforeOpen);
                return;
            }
            // if (IsAnyWindowOpening(currentPage)) return;
            currentPage.OpenUI<T>(data, beforeOpen);
        }

        /// <summary>
        /// 关闭当前页面上的窗口。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <param name="onClosed">关闭后的操作。</param>
        /// <param name="destroy">是否关闭后销毁</param>
        public void CloseWindow<T>(Action onClosed = null, bool destroy = false) where T : UIBehaviour, IUIChild
        {
            if(typeof(IUIStandAlone).IsAssignableFrom(typeof(T)))
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
        /// </summary>
        /// <param name="data">窗口数据。</param>
        private void OnUIWindowOpened(IUIChild data)
        {
            if (_pageStack.Count < 2 || _currentPagePushMode == PagePushMode.Overlap) return;
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

        // 关闭并销毁没开启的UI
        public void Clear()
        {
            var pages = _pageStack.Where(o=>o != currentPage).ToArray();
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