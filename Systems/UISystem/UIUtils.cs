using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class UIUtils
    {
        /// <summary>
        /// 传递UI事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="function">UI事件</param>
        /// <param name="target">传递对象</param>
        public static void PassEvent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> function, GameObject target)
            where T : IEventSystemHandler
        {
            var results = GetAllGameObjectsByPointerEventData(eventData);
            var current = eventData.pointerCurrentRaycast.gameObject;
            bool hasTarget = target;
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == current) continue;

                if (hasTarget)
                {
                    if (go == target)
                    {
                        ExecuteEvents.Execute(go, eventData, function);
                        break;
                    }

                    continue;
                }

                ExecuteEvents.Execute(go, eventData, function);
            }
        }
        
        /// <summary>
        /// 获取所有由事件数据指向的游戏对象。
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>由事件数据指向的游戏对象列表</returns>
        public static List<RaycastResult> GetAllGameObjectsByPointerEventData(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results;
        }

        /// <summary>
        /// 获取所有由屏幕位置指向的游戏对象。
        /// </summary>
        /// <param name="screenPosition">屏幕位置</param>
        /// <returns>由屏幕位置指向的游戏对象列表</returns>
        public static List<RaycastResult> GetAllGameObjectsByPoint(Vector2 screenPosition)
        {
            var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            return GetAllGameObjectsByPointerEventData(eventDataCurrentPosition);
        }
        
        public static void InitCanvas(IUIComponent uiChild, bool ignoreRaycaster, bool standaloneCanvas, RenderMode canvasRenderMode)
        {
            var rectTransform = uiChild.rectTransform;
            var gameObject = rectTransform.gameObject;

            gameObject.SetLayerRecursively("UI");
            rectTransform.localScale = Vector3.one;
            rectTransform.Adapt2Parent();

            if (uiChild is IUIParent uiparent)
            {
                var canvas = gameObject.GetComponent<Canvas>();
                if (!canvas) canvas = gameObject.AddComponent<Canvas>();
                uiparent.canvasCom = canvas;
                canvas.renderMode = canvasRenderMode;
                canvas.planeDistance = 10;
                if (canvasRenderMode != RenderMode.ScreenSpaceOverlay) canvas.worldCamera = UICamera.instance.cameraCom;
                var canvasScale = gameObject.TryAddComponent<CanvasScaler>();
                canvasScale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                var screenHeight = ConstSetting.DefaultUISize.y;
                var screenWidth = ConstSetting.DefaultUISize.x;
                var designRatio = screenHeight * 1f / screenWidth;
                var currentRatio = Screen.height * 1f / Screen.width;
                canvasScale.matchWidthOrHeight = currentRatio < designRatio ? 1 : 0;
                canvasScale.referenceResolution = ConstSetting.DefaultUISize;
                gameObject.TryAddComponent<GraphicRaycaster>().enabled = !ignoreRaycaster;
            }
            else
            {
                if (standaloneCanvas)
                {
                    var canvas = gameObject.GetComponent<Canvas>();
                    if (!canvas) gameObject.AddComponent<Canvas>();
                }

                if (ignoreRaycaster) gameObject.TryAddComponent<CanvasGroup>().blocksRaycasts = false;
            }
        }

        #region Page

        public static T CreatePage<T>(Transform parent, RenderMode canvasRenderMode)
            where T : UIBehaviour, IUIParent
        {
            var newPage = new GameObject(typeof(T).Name).AddComponent<T>();
            newPage.transform.SetParent(parent);
            newPage.gameObject.AddComponent<RectTransform>();
            InitUI(newPage, false, true, canvasRenderMode);
            return newPage;
        }
        
        public static void ClosePageInstance<T>(T page, bool destroy, Action callback, IUIParent poolParent) where T : IUIParent
        {
            CloseUI(page, null);
            if (destroy)
            {
                var keys = new Type[page.children.Count];
                page.children.Keys.CopyTo(keys, 0);
                foreach (var key in keys)
                {
                    var child = page.children[key];
                    if (child.isOpened)
                        CloseUI(child, null, true);
                    if (child is IUIPoolable && poolParent != null
                        && !poolParent.children.ContainsKey(child.GetType()) 
                        && !poolParent.windowRequests.IsUIGoingToOpen(key, out _))
                    {
                        SetUIChildToParent(child, poolParent);
                    }
                    else
                    {
                        DestroyUI(child, null);
                    }
                }
                DestroyUI(page, null);
            }
            callback?.Invoke();
        }

        #endregion

        public static void SetUIChildToParent<T>(T child, IUIParent parent) where T : IUIChild
        {
            if (child.parent != null) RemoveChild(child);

            var childTransform = child.transform;
            childTransform.SetParent(parent.transform);
            child.parent = parent;
            childTransform.SetAsLastSibling();
            childTransform.localPosition = Vector3.zero;
            childTransform.localScale = Vector3.one;
            parent.children[child.GetType()] = child;
        }
        
        public static void InitUI<T>(T ui, bool ignoreRaycaster, bool standaloneCanvas, RenderMode renderMode) where T : IUIComponent
        {
            InitCanvas(ui, ignoreRaycaster, standaloneCanvas, renderMode);
        }
        
        public static void OpenUI<T>(T ui, object data) where T : IUIComponent
        {
            if (ui == null) return;
            var uiTransform = ui.transform;
            var uiGameObject = uiTransform.gameObject;

            uiTransform.SetAsLastSibling();
            uiGameObject.SetActive(true);
            if (!ui.isOpened) ui.RegisterEvent();
            if (ui is IUIChild child)
            {
                child.parent.openedUIs.Push(child);
                child.Open(data);
                child.OnFocus();
                var widgets = uiGameObject.GetComponentsInChildren<IUIWidget>(true);
                for (var i = 0; i < widgets.Length; i++)
                {
                    widgets[i].OnWidgetEnable();
                }
                EventManager.instance.onUIOpen.Invoke(child);
            }
            else if (ui is IUIParent parent)
            {
                parent.Open(data);
                parent.OnFocus();
                foreach (var parentOpenedUI in parent.openedUIs)
                {
                    parentOpenedUI.OnFocus();
                }
                EventManager.instance.onPageOpen.Invoke(parent);
            }
        }

        public static bool CloseUI<T>(T ui, Action afterClosed, bool force = false) where T : IUIComponent
        {
            if (ui == null || !ui.isOpened) return false;
            if (!force && !ui.Close())
            {
                return false;
            }
            var uiGameObject = ui.transform.gameObject;

            uiGameObject.SetActive(false);
            if (ui is IUIParent parent)
            {
                foreach (var uiChild in parent.openedUIs)
                {
                    uiChild.OnHide();
                }
                EventManager.instance?.onPageClose.Invoke(parent);
            }
            if (ui is IUIChild child)
            {
                child.parent.openedUIs.Remove(child);
                var widgets = uiGameObject.GetComponentsInChildren<IUIWidget>(true);
                for (var i = 0; i < widgets.Length; i++)
                {
                    widgets[i].OnWidgetDisable();
                }
                EventManager.instance?.onUIClose.Invoke(child);
            }
            ui.OnClose();
            ui.DeregisterEvent();
            afterClosed?.Invoke();
            return true;
        }

        public static void RemoveChild<T>(T ui) where T : IUIChild
        {
            ui.parent.children.Remove(ui.GetType());
            ui.parent.openedUIs.Remove(ui);
        }
        
        public static void DestroyUI<T>(T ui, Action onDestroy) where T : IUIComponent
        {
            ui.OnUIDestroy();
            GameObject.Destroy(ui.transform.gameObject);
            onDestroy?.Invoke();
        }
    }
}