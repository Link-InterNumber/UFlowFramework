using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PowerCellStudio
{
    public static class ConfigLogger
    {
        public static bool enableLog = true;
        public static bool enableWarning = true;
        public static bool enableError = true;
        public static void Log(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#ECF304FF>Config Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#ECF304FF>Config Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#ECF304FF>Config Error</color>] {message}");
        }

        public static Exception Exception(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#ECF304FF>Config Exception</color>] {message}");
        }
    }

    public static class AppLogger
    {
        public static bool enableLog = true;
        public static bool enableWarning = true;
        public static bool enableError = true;
        public static void Log(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#005BFFFF>App Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#005BFFFF>App Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#005BFFFF>App Error</color>] {message}");
        }

        public static Exception Exception(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#005BFFFF>App Exception</color>] {message}");
        }
    }

    public static class AssetLogger
    {
        public static bool enableLog = true;
        public static bool enableWarning = true;
        public static bool enableError = true;
        public static void Log(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#FF8D15FF>Asset Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF8D15FF>Asset Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF8D15FF>Asset Error</color>] {message}");
        }

        public static Exception Exception(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF8D15FF>Asset Exception</color>] {message}");
        }
    }

    public static class UILogger
    {
        public static bool enableLog = true;
        public static bool enableWarning = true;
        public static bool enableError = true;
        public static void Log(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#FF6800FF>UI Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF6800FF>UI Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF6800FF>UI Error</color>] {message}");
        }

        public static Exception Exception(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF6800FF>UI Exception</color>] {message}");
        }
    }

    public static class NetWorkLogger
    {
        public static bool enableLog = true;
        public static bool enableWarning = true;
        public static bool enableError = true;
        public static void Log(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#009FFFFF>NetWork Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#009FFFFF>NetWork Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#009FFFFF>NetWork Error</color>] {message}");
        }

        public static Exception Exception(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#009FFFFF>NetWork Exception</color>] {message}");
        }
    }

    public static class ModuleLogger
    {
        public static bool enableLog = true;
        public static bool enableWarning = true;
        public static bool enableError = true;
        public static void Log(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#FF00DFFF>Module Log</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF00DFFF>Module Warning</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF00DFFF>Module Error</color>] {message}");
        }

        public static Exception Exception(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF00DFFF>Module Exception</color>] {message}");
        }

        public static void Log<T>(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#FF00DFFF>Module Log</color>:{typeof(T).Name}] {message}");
        }

        public static void LogWarning<T>(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#FF00DFFF>Module Warning</color>:{typeof(T).Name}] {message}");
        }

        public static void LogError<T>(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#FF00DFFF>Module Error</color>:{typeof(T).Name}] {message}");
        }

        public static Exception Exception<T>(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#FF00DFFF>Module Exception</color>:{typeof(T).Name}] {message}");
        }
    }

    public static class LinkLogger
    {
        public static bool enableLog = true;
        public static bool enableWarning = true;
        public static bool enableError = true;
        public static void Log(object message)
        {
            if (!enableLog) return;
            if(Application.isPlaying && !ApplicationManager.enableLog) return;
            Debug.Log($"[<color=#00D1FFFF>Link |•'-'•) ✧</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            if (!enableWarning) return;
            if(Application.isPlaying && !ApplicationManager.enableWarning) return;
            Debug.LogWarning($"[<color=#00D1FFFF>Link (°⌓°)</color>] {message}");
        }

        public static void LogError(object message)
        {
            if (!enableError) return;
            if(Application.isPlaying && !ApplicationManager.enableError) return;
            Debug.LogError($"[<color=#00D1FFFF>Link (◓Д◒)✄╰⋃╯</color>] {message}");
        }

        public static Exception Exception(object message)
        {
            if (!enableError) return null;
            if(Application.isPlaying && !ApplicationManager.enableError) return null;
            return new Exception($"[<color=#00D1FFFF>Link (✘Д✘๑ )</color>] {message}");
        }
    }

}
