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
                var screenHeight = ConstSetting.DefaultResolution.y;
                var screenWidth = ConstSetting.DefaultResolution.x;
                // var newRes = Vector2Int.zero;
                if (screenHeight < screenWidth)
                {
                    var baseHeight = ConstSetting.Resolution[2];
                    var rate = (float)baseHeight / Screen.height;
                    return new Vector2(Screen.width * rate, baseHeight);
                }
                else
                {
                    var baseWidth = ConstSetting.Resolution[2];
                    var rate = (float)baseWidth / Screen.width;
                    return new Vector2(baseWidth, Screen.height * rate);
                }
            }
        }

        public static float PixelScale
        {
            get
            {
                if (UICamera.instance)
                {
                    return ScreenSize.x / UICamera.instance.cameraCom.pixelWidth;
                }
                return 1f;
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
            var screenPos = instance.canvasRenderMode switch
            {
                RenderMode.ScreenSpaceOverlay => new Vector2(uiPosition.x / ScreenSize.x, uiPosition.y / ScreenSize.y),
                RenderMode.ScreenSpaceCamera => RectTransformUtility.WorldToScreenPoint(UICamera.instance.cameraCom, uiPosition),
                RenderMode.WorldSpace => RectTransformUtility.WorldToScreenPoint(UICamera.instance.cameraCom, uiPosition),
                _ => throw new ArgumentOutOfRangeException(),
            };
            return MainCamera.instance.CameraCom.ScreenToWorldPoint(screenPos);
        }

        /// <summary>
        /// 将主摄像机位置转换为UI位置。
        /// </summary>
        /// <param name="pos">主摄像机位置的Vector3。</param>
        /// <returns>UI位置的Vector2。</returns>
        public static Vector2 MainCamaraPosToUIPos(Vector3 pos)
        {
            var screenPos = MainCamera.instance.CameraCom.WorldToScreenPoint(pos);
            return UICamera.instance.cameraCom.ScreenToWorldPoint(screenPos);
        }

        public static void OpenMaskWindow(Func<bool> canClose, bool showWaiting = true)
        {
            var maskWindowData = new MaskWindow.MaskWindowData(showWaiting, canClose, null);
            instance.OpenWindow<MaskWindow>(maskWindowData);
        }
        
        public static void OpenMaskWindow(YieldInstruction yieldInstruction, bool showWaiting = true)
        {
            var maskWindowData = new MaskWindow.MaskWindowData(showWaiting, null, yieldInstruction);
            instance.OpenWindow<MaskWindow>(maskWindowData);
        }
        
        public static void OpenMaskWindow(float realTime, bool showWaiting = true)
        {
            var newWaitForSeconds = new WaitForSeconds(realTime);
            var maskWindowData = new MaskWindow.MaskWindowData(showWaiting, null, newWaitForSeconds);
            instance.OpenWindow<MaskWindow>(maskWindowData);
        }
        
        public static void EnableUIInput(bool enable)
        {
            EventManager.instance.onUIInputEnable?.Invoke(enable);
            if(enable) instance.CloseWindow<MaskWindow>();
            else instance.OpenWindow<MaskWindow>();
        }
    }
}