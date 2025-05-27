using System;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class UIManager
    {
        /// <summary>
        /// 获取UI系统下的屏幕尺寸。
        /// </summary>
        public static Vector2 ScreenSize
        {
            get
            {
                var screenHeight = ConstSetting.DefaultUISize.y;
                var screenWidth = ConstSetting.DefaultUISize.x;
                // var newRes = Vector2Int.zero;
                if (screenHeight < screenWidth)
                {
                    var baseHeight = ConstSetting.DefaultUISize.y;
                    var rate = (float)baseHeight / Screen.height;
                    return new Vector2(Screen.width * rate, baseHeight);
                }
                else
                {
                    var baseWidth = ConstSetting.DefaultUISize.x;
                    var rate = (float)baseWidth / Screen.width;
                    return new Vector2(baseWidth, Screen.height * rate);
                }
            }
        }

        /// <summary>
        /// 获取UI系统显示的缩放值
        /// </summary>
        public static float PixelScale
        {
            get
            {
                if (instance.canvasRenderMode == RenderMode.ScreenSpaceOverlay)
                    return 1f;
                if (UICamera.instance)
                {
                    return ScreenSize.x / UICamera.instance.cameraCom.pixelWidth;
                }
                return 1f;
            }
        }

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
        /// </summary>
        /// <param name="uiElement">UI元素的RectTransform。</param>
        /// <returns>UI元素在屏幕上的位置。</returns>
        public static Vector2 GetScreenPosition(RectTransform uiElement)
        {
            if(!uiElement) return Vector2.zero;
            return GetScreenPosition(uiElement.position);
        }
        
        /// <summary>
        /// 获取UI位置在屏幕上的位置。
        /// </summary>
        /// <param name="uiPosition">UI位置的Vector3。</param>
        /// <returns>UI位置在屏幕上的位置。</returns>
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
        /// </summary>
        /// <param name="uiElement">UI元素的RectTransform。</param>
        /// <returns>UI元素在主摄像机中的位置。</returns>
        public static Vector3 GetUIToMainCameraPosition(RectTransform uiElement)
        {
            if(!uiElement) return Vector2.zero;
            return GetUIToMainCameraPosition(uiElement.position);
        }
        
        /// <summary>
        /// 获取UI位置在主摄像机中的位置。
        /// </summary>
        /// <param name="uiPosition">UI位置的Vector3。</param>
        /// <returns>UI位置在主摄像机中的位置。</returns>
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
        /// </summary>
        /// <param name="pos">主摄像机位置的Vector3。</param>
        /// <returns>UI位置的Vector2。</returns>
        public static Vector2 MainCamaraPosToUIPos(Vector3 pos)
        {
            var viewportPoint = MainCamera.instance.CameraCom.WorldToViewportPoint(pos);
            return UICamera.instance.cameraCom.ViewportToWorldPoint(viewportPoint);
        }
        
        public static void EnableUIInput(bool enable)
        {
            EventManager.instance.onUIInputEnable?.Invoke(enable);
            if(enable) instance.CloseWindow<MaskWindow>();
            else instance.OpenWindow<MaskWindow>();
        }
    }
}