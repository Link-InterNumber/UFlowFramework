using System;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class UIManager
    {
        /// <summary>
        /// 获取UI系统下的屏幕尺寸。
        /// Retrieve the screen size under the UI system.
        /// </summary>
        public static Vector2 ScreenSize
        {
            get
            {
                var screenHeight = ConstSetting.DefaultUISize.y;
                var screenWidth = ConstSetting.DefaultUISize.x;
                var designRatio = screenHeight * 1f / screenWidth;
                Vector2Int currentScreenSize;
                if (UICamera.instance)
                {
                    currentScreenSize = UICamera.instance.currentScreen;
                }
                else
                {
                    currentScreenSize = new Vector2Int(Screen.width, Screen.height);
                }
                var currentRatio = currentScreenSize.y * 1f / currentScreenSize.x;
                if (currentRatio < designRatio)
                {
                    var baseHeight = ConstSetting.DefaultUISize.y;
                    var rate = (float)baseHeight / currentScreenSize.y;
                    return new Vector2(currentScreenSize.x * rate, baseHeight);
                }
                else
                {
                    var baseWidth = ConstSetting.DefaultUISize.x;
                    var rate = (float)baseWidth / currentScreenSize.x;
                    return new Vector2(baseWidth, currentScreenSize.y * rate);
                }
            }
        }

        /// <summary>
        /// 获取UI系统显示的缩放值。
        /// Get the scaling value for display in the UI system.
        /// </summary>
        public static float PixelScale
        {
            get
            {
                if (!instance) return 1f;
                if (instance.canvasRenderMode == RenderMode.ScreenSpaceOverlay)
                    return 1f;
                if (UICamera.instance)
                {
                    return ScreenSize.x / UICamera.instance.cameraCom.pixelWidth;
                }
                return ScreenSize.x / Screen.currentResolution.width;
            }
        }

        public static Vector2 GetUIPosition(RectTransform transform)
        {
            var screenPos = GetScreenPosition(transform);
            return ScreenPosToUIPos(screenPos);
        }

        public static bool IsRectOutOfScreen(RectTransform transform)
        {
            var corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                var uiWorldPos = corners[i];
                if (!IsUIPosOutOfScreen(uiWorldPos)) return false;
            }
            return true;
        }
        
        public static bool IsUIPosOutOfScreen(Vector3 uiWorldPos)
        {
            switch (instance.canvasRenderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    var screenPos = ScreenSize;
                    return uiWorldPos.x < 0f || uiWorldPos.x > screenPos.x || uiWorldPos.y < 0f || uiWorldPos.y > screenPos.y;
                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    var viewPos = UICamera.instance.cameraCom.WorldToViewportPoint(uiWorldPos);
                    return viewPos.x < 0f || viewPos.x > 1f || viewPos.y < 0f || viewPos.y > 1f;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool IsPointOutOfScreen(RectTransform transform, Vector2 localPoint)
        {
            switch (instance.canvasRenderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    var screenPos = ScreenSize;
                    var worldPos = (Vector2)transform.position + localPoint;
                    return worldPos.x < 0f || worldPos.x > screenPos.x || worldPos.y < 0f || worldPos.y > screenPos.y;
                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    var uiPosition = transform.TransformPoint(localPoint);
                    var viewPos = UICamera.instance.cameraCom.WorldToViewportPoint(uiPosition);
                    return viewPos.x < 0f || viewPos.x > 1f || viewPos.y < 0f || viewPos.y > 1f;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 将屏幕坐标转换为UI坐标。
        /// Convert screen position to UI position.
        /// </summary>
        /// <param name="screenPos">屏幕坐标 / Screen position</param>
        /// <returns>UI坐标 / UI position</returns>
        public static Vector3 ScreenPosToUIPos(Vector2 screenPos)
        {
            switch (instance.canvasRenderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    return new Vector3(screenPos.x, screenPos.y, 0);
                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    var worldPos = UICamera.instance.cameraCom.ScreenToWorldPoint(screenPos);
                    worldPos.z = 0;
                    return worldPos;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 获取UI元素在屏幕上的位置。
        /// Get the screen position of a UI element.
        /// </summary>
        /// <param name="uiElement">UI元素的RectTransform / RectTransform of the UI element</param>
        /// <returns>UI元素在屏幕上的位置 / Screen position of the UI element</returns>
        public static Vector2 GetScreenPosition(RectTransform uiElement)
        {
            if (!uiElement) return Vector2.zero;
            return GetScreenPosition(uiElement.position);
        }

        /// <summary>
        /// 获取UI位置在屏幕上的位置。
        /// Get screen position from a UI position.
        /// </summary>
        /// <param name="uiPosition">UI位置的Vector3 / Vector3 of the UI position</param>
        /// <returns>UI位置在屏幕上的位置 / Screen position of the UI</returns>
        public static Vector2 GetScreenPosition(Vector3 uiPosition)
        {
            switch (instance.canvasRenderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    return new Vector2(uiPosition.x / ScreenSize.x, uiPosition.y / ScreenSize.y);
                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    return RectTransformUtility.WorldToScreenPoint(UICamera.instance.cameraCom, uiPosition);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 获取UI元素在主摄像机中的位置。
        /// Get the position of a UI element in the main camera.
        /// </summary>
        /// <param name="uiElement">UI元素的RectTransform / RectTransform of the UI element</param>
        /// <returns>UI元素在主摄像机中的位置 / Position of the UI element in the main camera</returns>
        public static Vector3 GetUIToMainCameraPosition(RectTransform uiElement)
        {
            if (!uiElement) return Vector2.zero;
            return GetUIToMainCameraPosition(uiElement.position);
        }

        /// <summary>
        /// 获取UI位置在主摄像机中的位置。
        /// Get the position from a UI to the main camera.
        /// </summary>
        /// <param name="uiPosition">UI位置的Vector3 / Vector3 of the UI position</param>
        /// <returns>UI位置在主摄像机中的位置 / Position of the UI in the main camera</returns>
        public static Vector3 GetUIToMainCameraPosition(Vector3 uiPosition)
        {
            Vector2 viewportPoint;
            switch (instance.canvasRenderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    viewportPoint = new Vector2(uiPosition.x / ScreenSize.x, uiPosition.y / ScreenSize.y);
                    break;
                case RenderMode.ScreenSpaceCamera:
                    viewportPoint = (Vector2)UICamera.instance.cameraCom.WorldToViewportPoint(uiPosition);
                    break;
                case RenderMode.WorldSpace:
                    return uiPosition;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return MainCamera.instance.CameraCom.ViewportToWorldPoint(viewportPoint);
        }

        /// <summary>
        /// 将主摄像机位置转换为UI位置。
        /// Convert main camera position to UI position.
        /// </summary>
        /// <param name="pos">主摄像机位置的Vector3 / Vector3 of the main camera position</param>
        /// <returns>UI位置的Vector2 / UI position as a Vector2</returns>
        public static Vector2 MainCamaraPosToUIPos(Vector3 pos)
        {
            var viewportPoint = MainCamera.instance.CameraCom.WorldToViewportPoint(pos);
            return UICamera.instance.cameraCom.ViewportToWorldPoint(viewportPoint);
        }

        /// <summary>
        /// 开启一个 Mask 窗口，阻止 UI 输入。
        /// Open a Mask window to block UI input.
        /// </summary>
        /// <param name="enable">是否启用 / Whether to enable</param>
        public static void ShowMaskWindow(bool enable)
        {
            if (enable) instance.CloseWindow<MaskWindow>();
            else instance.OpenWindow<MaskWindow>();
        }
    }
}