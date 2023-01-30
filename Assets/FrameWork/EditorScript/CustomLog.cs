using System;
using Unity.Burst;
using UnityEngine;
using Wahoo.Modules.Base;

namespace Wahoo.Debugs.CustomLog
{
    public static class ConfLog
    {
        [BurstDiscard] public static void Log(string msg) { Debug.Log($"[Config Log] {msg}"); }
        [BurstDiscard] public static void LogWarning(string msg) { Debug.LogWarning($"[Config Warning] {msg}"); }
        [BurstDiscard] public static void LogError(string msg) { Debug.LogError($"[配置错误] {msg}"); }
        public static Exception Exception(string msg) { return new Exception($"[配置错误] {msg}"); }
    }

    public static class ServerLog
    {
        [BurstDiscard] public static void Log(string msg) { Debug.Log($"[Server Log] {msg}"); }
        [BurstDiscard] public static void LogWarning(string msg) { Debug.LogWarning($"[Server Warning] {msg}"); }
        [BurstDiscard] public static void LogError(string msg) { Debug.LogError($"[Server Error] {msg}"); }
        public static Exception Exception(string msg) { return new Exception($"[Server Error] {msg}"); }
    }
    
    public static class ModuleLog<T>
        where T : ModuleBase
    {
        [BurstDiscard] public static void Log(string msg) { Debug.Log($"[{typeof(T).Name} Log] {msg}"); }
        [BurstDiscard] public static void LogWarning(string msg) { Debug.LogWarning($"[{typeof(T).Name} Warning] {msg}"); }
        [BurstDiscard] public static void LogError(string msg) { Debug.LogError($"[{typeof(T).Name} Error] {msg}"); }
        public static Exception Exception(string msg) { return new Exception($"[{typeof(T).Name} Error] {msg}"); }
    }
    
    public static class LinkLog
    {
        [BurstDiscard] public static void Log(string msg) { Debug.Log($"[Link |•'-'•) ✧] {msg}"); }
        [BurstDiscard] public static void LogWarning(string msg) { Debug.LogWarning($"[Link (°⌓°)] {msg}"); }
        [BurstDiscard] public static void LogError(string msg) { Debug.LogError($"[Link (◓Д◒)✄╰⋃╯] {msg}"); }
        public static Exception Exception(string msg) { return new Exception($"[Link (✘Д✘๑ )] {msg}"); }
    }
}