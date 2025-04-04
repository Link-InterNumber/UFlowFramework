using System;
using UnityEngine;

namespace PowerCellStudio
{
    public static class ConfigLog
    {
        public static void Log(object message)
        {
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#ECF304>Config Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#ECF304>Config Log</color> Warning] {message}");
        }

        public static void LogError(object message)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<col$or=#ffff00ff>Config Log</color> Error] {message}");
        }

        public static Exception Exception(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#ECF304>Config Log</color> Exception] {msg}");
        }
    }

    public static class AppLog
    {
        public static void Log(object message)
        {
            if (Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#005BFF>App Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#005BFF>App Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#005BFF>App Error</color>] {message}");
        }

        public static Exception Exception(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#005BFF>App Exception</color>] {msg}");
        }
    }

    public static class AssetLog
    {
        public static void Log(object message)
        {
            if (Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#FF8D15>Asset Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF8D15>Asset Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF8D15>Asset Error</color>] {message}");
        }

        public static Exception Exception(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF8D15>Asset Exception</color>] {msg}");
        }
    }

    public static class UILog
    {
        public static void Log(object message)
        {
            if (Application.isPlaying && !ApplicationManager.enableLog) return; 
            Debug.Log($"[<color=#FF6800>UI Manager</color> Log] {message}");
        }

        public static void LogWarning(object message)
        {
            if (Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF6800>UI Manager</color> Warning] {message}");
        }

        public static void LogError(object message)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF6800>UI Manager</color> Error] {message}");
        }

        public static Exception Exception(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF6800>UI Manager</color> Exception] {msg}");
        }
        
        public static void Log<T>(object message) where T : IUIComponent
        {
            if (Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#FF6800>{typeof(T).Name}</color> Log] {message}");
        }

        public static void LogWarning<T>(object message) where T: IUIComponent
        {
            if (Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF6800>{typeof(T).Name}</color> Warning] {message}");
        }

        public static void LogError<T>(object message) where T: IUIComponent
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF6800>{typeof(T).Name}</color> Error] {message}");
        }

        public static Exception Exception<T>(object msg) where T: IUIComponent
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF6800>{typeof(T).Name}</color> Exception] {msg}");
        }
    }

    public static class NetWorkLog
    {
        public static void Log(object msg)
        {
            if (Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"<color=#009FFF>[NetWork Log]</color> {msg}");
        }

        public static void LogWarning(object msg)
        {
            if (Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"<color=#009FFF>[NetWork Warning]</color> {msg}");
        }

        public static void LogError(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"<color=#009FFF>[NetWork Error]</color> {msg}");
        }

        public static Exception Exception(object msg)
        {
            return new Exception($"<color=#009FFF>[NetWork Exception]</color> {msg}");
        }
    }

    public static class ModuleLog<T>
    {
        public static void Log(object msg)
        {
            if (Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#FF00DF>{typeof(T).Name}</color> Log] {msg}");
        }

        public static void LogWarning(object msg)
        {
            if (Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF00DF>{typeof(T).Name}</color> Warning] {msg}");
        }

        public static void LogError(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF00DF>{typeof(T).Name}</color> Error] {msg}");
        }

        public static Exception Exception(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF00DF>{typeof(T).Name}</color> Exception] {msg}");
        }
    }

    public static class LinkLog
    {
        public static void Log(object msg)
        {
            if (Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#00D1FF>Link |•'-'•) ✧</color>] {msg}");
        }

        public static void LogWarning(object msg)
        {
            if (Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#00D1FF>Link (°⌓°)</color>] {msg}");
        }

        public static void LogError(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#00D1FF>Link (◓Д◒)✄╰⋃╯</color>] {msg}");
        }

        public static Exception Exception(object msg)
        {
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#00D1FF>Link (✘Д✘๑ )</color>] {msg}");
        }
    }
}