using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PowerCellStudio
{
    public class TapToClose : MonoBehaviour
    {
        public GameObject[] goExcludes;
        
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Application.isMobilePlatform)
            {
                var touchCount = Touchscreen.current.touches.Count;
                for(int i = 0; i < touchCount; ++i)
                {
                    var touch = Touchscreen.current.touches[i];
                    if (!touch.isInProgress) continue;
                    if (goExcludes == null || goExcludes.Length == 0)
                    {
                        gameObject.SetActive(false);
                        break;
                    }
                    var results = GetPointerOverUIObjects(touch.position.ReadValue());
                    if (results.Count <= 0) continue;
                    var firstUI = results[0];
                    if (goExcludes != null && goExcludes.Any(o=>o && o == firstUI.gameObject)) continue;
                    gameObject.SetActive(false);
                    break;
                }
                return;
            }

            if (Mouse.current != null)
            {
                if (!Mouse.current.leftButton.isPressed) return;
                if (goExcludes == null || goExcludes.Length == 0)
                {
                    gameObject.SetActive(false);
                    return;
                }
                var res = GetPointerOverUIObjects(Mouse.current.position.ReadValue());
                if (res.Count <= 0) return;
                var fUI = res[0];
                if (goExcludes != null && goExcludes.Any(o=>o && o == fUI.gameObject)) return;
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
                        if (touch.phase != TouchPhase.Began && touch.phase != TouchPhase.Moved && touch.phase != TouchPhase.Stationary)
                            continue;
                        if (goExcludes == null || goExcludes.Length == 0)
                        {
                            gameObject.SetActive(false);
                            break;
                        }
                        var results = GetPointerOverUIObjects(touch.position);
                        if (results.Count <= 0) continue;
                        var firstUI = results[0];
                        if (goExcludes != null && goExcludes.Any(o => o && o == firstUI.gameObject)) continue;
                        gameObject.SetActive(false);
                        break;
                    }
                    return;
                }
            }
            else
            {
                if (!Input.GetMouseButton(0)) return;
                if (goExcludes == null || goExcludes.Length == 0)
                {
                    gameObject.SetActive(false);
                    return;
                }
                var res = GetPointerOverUIObjects(Input.mousePosition);
                if (res.Count <= 0) return;
                var fUI = res[0];
                if (goExcludes != null && goExcludes.Any(o => o && o == fUI.gameObject)) return;
                gameObject.SetActive(false);
            }
#endif
        }
        
        private List<RaycastResult> GetPointerOverUIObjects(Vector2 screenPosition)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results;
        }
    }
}