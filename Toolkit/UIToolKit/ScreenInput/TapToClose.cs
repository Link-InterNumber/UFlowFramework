using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PowerCellStudio
{
    /// <summary>
    /// 根据当前触摸或鼠标输入自动关闭自身的 UI 组件。
    /// UI component that automatically deactivates itself according to the current touch or mouse input.
    /// </summary>
    public class TapToClose : MonoBehaviour
    {
        /// <summary>
        /// 不应触发关闭的 UI 游戏对象；指针射线检测的首个对象与其中任一对象相同时保持开启。
        /// UI GameObjects that should not trigger closing; remains active when the first raycast hit matches any of these objects.
        /// </summary>
        public GameObject[] goExcludes;
        
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Application.isMobilePlatform)
            {
                if (Touchscreen.current == null) return;
                var touchCount = Touchscreen.current.touches.Count;
                if (touchCount <= 0) return;
                for (var i = 0; i < touchCount; ++i)
                {
                    var touch = Touchscreen.current.touches[i];
                    if (!touch.press.wasPressedThisFrame) continue;
                    if (goExcludes == null || goExcludes.Length == 0)
                    {
                        gameObject.SetActive(false);
                        break;
                    }
                    var results = GetPointerOverUIObjects(touch.position.ReadValue());
                    if (results.Count <= 0)
                    {
                        gameObject.SetActive(false);
                        break;
                    }
                    var firstUI = results[0];
                    if (IsExcludedUI(firstUI.gameObject)) continue;
                    gameObject.SetActive(false);
                    break;
                }
                return;
            }

            if (Mouse.current != null)
            {
                if (!Mouse.current.leftButton.wasPressedThisFrame) return;
                if (goExcludes == null || goExcludes.Length == 0)
                {
                    gameObject.SetActive(false);
                    return;
                }
                var res = GetPointerOverUIObjects(Mouse.current.position.ReadValue());
                if (res.Count <= 0)
                {
                    gameObject.SetActive(false);
                    return;
                }
                var firstUI = res[0];
                if (IsExcludedUI(firstUI.gameObject)) return;
                gameObject.SetActive(false);
            }
#else
            if (Application.isMobilePlatform)
            {
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; ++i)
                    {
                        var touch = Input.GetTouch(i);
                        if (touch.phase != TouchPhase.Began)
                            continue;
                        if (goExcludes == null || goExcludes.Length == 0)
                        {
                            gameObject.SetActive(false);
                            break;
                        }
                        var results = GetPointerOverUIObjects(touch.position);
                        if (results.Count <= 0)
                        {
                            gameObject.SetActive(false);
                            break;  
                        }
                        var firstUI = results[0];
                        if (IsExcludedUI(firstUI.gameObject)) return;
                        gameObject.SetActive(false);
                        break;
                    }
                    return;
                }
            }
            else
            {
                if (!Input.GetMouseButtonDown(0)) return;
                if (goExcludes == null || goExcludes.Length == 0)
                {
                    gameObject.SetActive(false);
                    return;
                }
                var res = GetPointerOverUIObjects(Input.mousePosition);
                if (res.Count <= 0) 
                {
                    gameObject.SetActive(false);
                    return;
                }
                var firstUI = res[0];
                if (IsExcludedUI(firstUI.gameObject)) return;
                gameObject.SetActive(false);
            }
#endif
        }
        
        private bool IsExcludedUI(GameObject firstUI)
        {
            if (!firstUI || goExcludes == null)
                return false;
            var hitTransform = firstUI.transform;
            for (var i = 0; i < goExcludes.Length; i++)
            {
                var exclude = goExcludes[i];
                if (!exclude)
                    continue;

                if (firstUI == exclude || hitTransform.IsChildOf(exclude.transform))
                    return true;
            }
            return false;
        }

        private List<RaycastResult> _resultsBuffer;
        private PointerEventData _eventDataCurrentPosition;

        private void Awake()
        {
            _resultsBuffer = ListPool<RaycastResult>.Get();
        }

        private void OnDestroy()
        {
            ListPool<RaycastResult>.Release(_resultsBuffer);
            _eventDataCurrentPosition = null;
        }

        private List<RaycastResult> GetPointerOverUIObjects(Vector2 screenPosition)
        {
            _resultsBuffer.Clear();
            if (!EventSystem.current)
            {
                UILogger.LogError("EventSystem is not found in the scene. Please add an EventSystem to use TapToClose.");
                return _resultsBuffer;
            }
            if (_eventDataCurrentPosition == null)
                _eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            _eventDataCurrentPosition.Reset();
            _eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);
            EventSystem.current.RaycastAll(_eventDataCurrentPosition, _resultsBuffer);
            return _resultsBuffer;
        }
    }
}