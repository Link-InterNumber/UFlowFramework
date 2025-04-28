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
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if(go == current) continue;
                if (target && go == target)
                {
                    ExecuteEvents.Execute(go, eventData, function);
                    break;
                }
                if(target)
                {
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
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);
            return GetAllGameObjectsByPointerEventData(eventDataCurrentPosition);
        }
        
        public static void InitCanvas(IUIComponent uiChild, bool ignoreRaycaster, RenderMode canvasRenderMode)
        {
            uiChild.transform.gameObject.SetLayerRecursively("UI");
            var canvas =  uiChild.rectTransform.gameObject.GetComponent<Canvas>();
            if(!canvas)  canvas = uiChild.rectTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = canvasRenderMode;
            canvas.planeDistance = 10;
            uiChild.rectTransform.localScale = Vector3.one;
            uiChild.rectTransform.Adapt2Parent();
            if (canvasRenderMode != RenderMode.ScreenSpaceOverlay) canvas.worldCamera = UICamera.instance.cameraCom;
            if (uiChild is IUIParent)
            {
                var canvasScale = uiChild.rectTransform.gameObject.TryAddComponent<CanvasScaler>();
                canvasScale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                var screenHeight = ConstSetting.DefaultUISize.y;
                var screenWidth = ConstSetting.DefaultUISize.x;
                // var newRes = Vector2Int.zero;
                if (screenHeight < screenWidth)
                {
                    canvasScale.matchWidthOrHeight = 1;
                }
                else
                {
                    canvasScale.matchWidthOrHeight = 0;
                }
                canvasScale.referenceResolution = ConstSetting.DefaultUISize;
            }
            uiChild.rectTransform.gameObject.TryAddComponent<GraphicRaycaster>().enabled = uiChild is IUIParent || !ignoreRaycaster;
        }

        #region Page

        public static T CreatePage<T>(Transform parent, RenderMode canvasRenderMode)
            where T : UIBehaviour, IUIParent
        {
            var newPage = new GameObject(typeof(T).Name).AddComponent<T>();
            newPage.transform.SetParent(parent);
            newPage.gameObject.AddComponent<RectTransform>();
            InitUI(newPage, true, canvasRenderMode);
            return newPage;
        }
        
        public static void ClosePage<T>(T page, bool destroy, Action callback, IUIParent poolParent) where T : IUIParent
        {
            CloseUI(page, null);
            if (destroy)
            {
                foreach (var keyValuePair in page.children)
                {
                    var child = keyValuePair.Value;
                    CloseUI(child, null);
                    if (child is IUIPoolable 
                        && !poolParent.children.ContainsKey(child.GetType()) 
                        && !poolParent.windowRequests.IsUIGoingToOpen(keyValuePair.Key, out _))
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
            if(child.parent != null) RemoveChild(child);
            child.transform.SetParent(parent.transform);
            child.parent = parent;
            child.transform.SetAsLastSibling();
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = Vector3.one;
            var childType = child.GetType();
            parent.children[childType] = child;
        }
        
        public static void InitUI<T>(T ui, bool ignoreRaycaster, RenderMode renderMode) where T : IUIComponent
        {
            InitCanvas(ui, ignoreRaycaster, renderMode);
            ui.RegisterEvent();
        }
        
        public static void OpenUI<T>(T ui, object data) where T : IUIComponent
        {
            if (ui == null) return;
            ui.transform.SetAsLastSibling();
            ui.transform.gameObject.SetActive(true);
            if(ui is IUIChild child)
            {
                child.parent.openedUIs.Push(child);
                child.Open(data);
                child.OnFocus();
                EventManager.instance.onUIOpen.Invoke(child);
            }
            else if(ui is IUIParent parent)
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

        public static bool CloseUI<T>(T ui, Action onClose, bool force = false) where T : IUIComponent
        {
            if (ui == null || !ui.isOpened) return false;
            if (force)
            {
                ui.OnClose();
            }
            else if (!ui.Close())
            {
                return false;
            }
            ui.transform.gameObject.SetActive(false);
            onClose?.Invoke();
            if(ui is IUIParent parent)
            {
                EventManager.instance.onPageClose.Invoke(parent);
            }
            if(ui is IUIChild child)
            {
                child.parent.openedUIs.Remove(child);
                EventManager.instance.onUIClose.Invoke(child);
            }
            return true;
        }

        public static void RemoveChild<T>(T ui) where T : IUIChild
        {
            var type = ui.GetType();
            ui.parent.children.Remove(type);
            ui.parent.openedUIs.Remove(ui);
        }
        
        public static void DestroyUI<T>(T ui, Action onClose) where T : IUIComponent
        {
            ui.DeregisterEvent();
            ui.OnUIDestroy();
            GameObject.Destroy(ui.transform.gameObject);
            onClose?.Invoke();
        }
    }
}