using System;
using UnityEngine;

namespace PowerCellStudio
{
    public static class UIWindowUtils
    {
        public static void LoadWidget<T>(Transform parent, IAssetLoader loader, Action<T> widgetCallback)
        {
            var info = WindowInfoCache.GetInfo(typeof(T));
            if (info == null)
            {
                UILogger.LogError($"UIWidget {typeof(T).Name}没有挂载WindowInfo特性");
                return;
            }
            loader.LoadAsync<GameObject>(info.path, (obj) =>
            {
                if (obj == null)
                {
                    UILogger.LogError($"UIWidget {typeof(T).Name}加载失败");
                    return;
                }
                var instance = GameObject.Instantiate(obj, parent);
                instance.name = obj.name;
                var widget = instance.GetComponent<T>();
                if (widget == null)
                {
                    UILogger.LogError($"UIWidget {typeof(T).Name}没有挂载在预制体上");
                    return;
                }
                if (info.standaloneCanvas)
                {
                    var canvas = instance.GetComponent<Canvas>();
                    if (!canvas) instance.AddComponent<Canvas>();
                }
                if (info.ignoreRaycast) instance.TryAddComponent<CanvasGroup>().blocksRaycasts = false;
                widgetCallback?.Invoke(widget);
            });
        }
    }
}