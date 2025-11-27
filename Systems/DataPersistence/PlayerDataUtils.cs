using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace PowerCellStudio
{
    public enum PlayerDataType
    {
        Json = 0,
        Binary,
        PlayerPrefs,
    }

    public static partial class PlayerDataUtils
    {
        private static PersistenceDataProcessor[] _persistenceDataProcessors;
        private static readonly CaptureDataProcessor _captureProcessor = new CaptureDataProcessor();

        private static void Init()
        {
            var enumValues = Enum.GetValues(typeof(PlayerDataType));
            _persistenceDataProcessors = new PersistenceDataProcessor[enumValues.Length];

            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(PersistenceDataProcessor)))
                    continue;

                // 获取自定义特性
                var attribute = type.GetCustomAttribute<DataProcessorAttribute>();
                if (attribute != null)
                {
                    try
                    {
                        // 4. 创建实例
                        var instance = (PersistenceDataProcessor)Activator.CreateInstance(type);

                        // 5. 根据枚举值放入数组对应的索引位置
                        int index = (int)attribute.DataType;
                        if (index < _persistenceDataProcessors.Length)
                        {
                            _persistenceDataProcessors[index] = instance;
#if UNITY_EDITOR
                            LinkLog.Log($"[PlayerDataUtils] Registered {type.Name} for {attribute.DataType}");
#endif
                        }
                    }
                    catch (Exception e)
                    {
                        LinkLog.LogError($"[PlayerDataUtils] Failed to instantiate processor for {attribute.DataType}: {e.Message}");
                    }
                }
            }
#if UNITY_EDITOR
            for (int i = 0; i < _persistenceDataProcessors.Length; i++)
            {
                if (_persistenceDataProcessors[i] == null && Enum.IsDefined(typeof(PlayerDataType), i))
                {
                    LinkLog.LogWarning($"[PlayerDataUtils] No processor registered for PlayerDataType: {(PlayerDataType)i}");
                }
            }
#endif
        }

        private static PersistenceDataProcessor GetProcessor(PlayerDataType dataType)
        {
            if (_persistenceDataProcessors == null)
            {
                Init();
            }
            int index = (int)dataType;
            if (index >= 0 && index < _persistenceDataProcessors.Length)
            {
                return _persistenceDataProcessors[index];
            }
            return null;
        }

        public static bool HasSave(string saveKey, Type dataType)
        {
            var processor = GetProcessor(PlayerDataType.PlayerPrefs);
            if (processor == null) return false;
            return processor.HasSave(saveKey);
        }

        public static bool HasSave<T>(Type dataType)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return HasSave(key, dataType);
        }

        #region Save
        public static bool Save<T>(string saveKey, T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return false;
            return processor.Save<T>(saveKey, data, encrypt);
        }

        public static bool Save<T>(T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return Save<T>(key, data, dataType, encrypt);
        }

        public static void SaveAsync<T>(string saveKey, T data, PlayerDataType dataType, Action<bool> onComplete, bool encrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null)
            {
                onComplete?.Invoke(false);
                return;
            }
            processor.SaveAsync<T>(saveKey, data, onComplete, encrypt);
        }

        public static void SaveAsync<T>(T data, PlayerDataType dataType, Action<bool> onComplete, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            SaveAsync<T>(key, data, dataType, onComplete, encrypt);
        }

        public static YieldInstructionCompletionSource<bool> SaveAsYieldInstruction<T>(string saveKey, T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var token = new YieldInstructionCompletionSource<bool>();
            var processor = GetProcessor(dataType);
            if (processor == null)
            {
                token.SetResult(false);
                return token;
            }
            processor.SaveAsync<T>(saveKey, data, token.SetResult, encrypt);
            return token;
        }

        public static YieldInstructionCompletionSource<bool> SaveAsYieldInstruction<T>(T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return SaveAsYieldInstruction<T>(key, data, dataType, encrypt);
        }

#if !UNITY_WEBGL
        public static Task<bool> SaveAsTask<T>(string saveKey, T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return Task.FromResult(false);
            var tcs = new TaskCompletionSource<bool>();
            processor.SaveAsync<T>(saveKey, data, tcs.SetResult, encrypt);
            return tcs.Task;
        }

        public static Task<bool> SaveAsTask<T>(T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return SaveAsTask<T>(key, data, dataType, encrypt);
        }
#endif

        #endregion

        #region Read
        public static T Read<T>(string saveKey, PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return default;
            return processor.Read<T>(saveKey, decrypt);
        }

        public static T Read<T>(PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return Read<T>(key, dataType, decrypt);
        }

        public static void ReadAsync<T>(string saveKey, PlayerDataType dataType, Action<T> onComplete, bool decrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null)
            {
                onComplete?.Invoke(default);
                return;
            }
            processor.ReadAsync<T>(saveKey, onComplete, decrypt);
        }

        public static void ReadAsync<T>(PlayerDataType dataType, Action<T> onComplete, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            ReadAsync<T>(key, dataType, onComplete, decrypt);
        }

        public static YieldInstructionCompletionSource<T> ReadAsYieldInstruction<T>(string saveKey, PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var token = new YieldInstructionCompletionSource<T>();
            var processor = GetProcessor(dataType);
            if (processor == null)
            {
                token.SetResult(default);
                return token;
            }
            processor.ReadAsync<T>(saveKey, token.SetResult, decrypt);
            return token;
        }

        public static YieldInstructionCompletionSource<T> ReadAsYieldInstruction<T>(PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadAsYieldInstruction<T>(key, dataType, decrypt);
        }

#if !UNITY_WEBGL
        public static Task<T> ReadAsTask<T>(string saveKey, PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return default;
            var tcs = new TaskCompletionSource<T>();
            processor.ReadAsync<T>(saveKey, tcs.SetResult, decrypt);
            return tcs.Task;
        }

        public static Task<T> ReadAsTask<T>(PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadAsTask<T>(key, dataType, decrypt);
        }
#endif

        #endregion

        #region Clear
        public static void Clear(string saveKey, PlayerDataType dataType)
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return;
            processor.Clear(saveKey);
        }

        public static void Clear<T>(PlayerDataType dataType)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            Clear(key, dataType);
        }

        public static void ClearAll(PlayerDataType dataType)
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return;
            processor.ClearAll();
        }

        public static void ClearAllData()
        {
            ClearCapture();
            if (_persistenceDataProcessors == null) return;
            foreach (var processor in _persistenceDataProcessors)
            {
                processor?.ClearAll();
            }
        }
        #endregion

        public static bool HasCapture(string fileName)
        {
            return _captureProcessor.HasSave(fileName);
        }

        #region PlayerPrefsSave

        public static void SavePlayerPrefs(string key, int data)
        {
            PlayerPrefs.SetInt(key, data);
            PlayerPrefs.Save();
        }

        public static void SavePlayerPrefs(string key, string data)
        {
            PlayerPrefs.SetString(key, data);
            PlayerPrefs.Save();
        }

        public static void SavePlayerPrefs(string key, float data)
        {
            PlayerPrefs.SetFloat(key, data);
            PlayerPrefs.Save();
        }

        #endregion

        #region PlayerPrefsRead

        public static int ReadPlayerInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public static string ReadPlayerString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public static float ReadPlayerFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        #endregion

        #region DebugLog

        public static void SaveDebugLog(DebugLogSaver coroutineRunner, string fileName, string data)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var directory = Path.Combine($"{Application.persistentDataPath}", "Debug");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            // string json = JsonConvert.SerializeObject(data);
            var path = Path.Combine($"{Application.persistentDataPath}", "Debug", $"{fileName}.txt");

            // 检查文件是否存在
            if (!File.Exists(path))
            {
                // 如果文件不存在，创建文件
                File.WriteAllTextAsync(path, data);
#if UNITY_EDITOR
                Debug.LogWarning("Save Debug txt At: " + path);
#endif
                return;
            }

            // 异步添加文本到文件末尾
            File.AppendAllTextAsync(path, data);
#if UNITY_EDITOR
            Debug.LogWarning("Save Debug txt At: " + path);
#endif
        }

        #endregion

        #region Capture

        private static bool captureTakeing = false;

        public static Coroutine TakeCapture(string fileName, Rect rect, Camera camera = null, bool encrypt = false)
        {
            if (captureTakeing) return null;
            captureTakeing = true;
            if (camera == null)
                return ApplicationManager.RunCoroutine(ScreenCapture(fileName, rect, encrypt));
            return ApplicationManager.RunCoroutine(CameraCapture(camera, fileName, rect, encrypt));
        }

        private static IEnumerator CameraCapture(Camera camera, string fileName, Rect rect, bool encrypt)
        {
            yield return new WaitForEndOfFrame();
            RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, 24);
            rt.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat;
            rt.format = RenderTextureFormat.ARGB32;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            Texture2D texture2D = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            texture2D.ReadPixels(rect, 0, 0);
            texture2D.Apply();
            var bytes = texture2D.EncodeToPNG();
            var token = new YieldInstructionCompletionSource<bool>();
            _captureProcessor.SaveAsync(fileName, texture2D, token.SetResult, encrypt);
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(texture2D);
            yield return token;
            captureTakeing = false;
#if UNITY_EDITOR
            if (token.Result)
            {
                _captureProcessor.TryGetSaveFilePath(fileName, out var path);
                LinkLog.Log($"Save a camera capture at {path}");
            }
#endif
        }

        private static IEnumerator ScreenCapture(string fileName, Rect rect, bool encrypt)
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture2D = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            texture2D.ReadPixels(rect, 0, 0);
            texture2D.Apply();
            var token = new YieldInstructionCompletionSource<bool>();
            _captureProcessor.SaveAsync(fileName, texture2D, token.SetResult, encrypt);
            Object.Destroy(texture2D);
            yield return token;
            captureTakeing = false;
#if UNITY_EDITOR
            if (token.Result)
            {
                _captureProcessor.TryGetSaveFilePath(fileName, out var path);
                LinkLog.Log($"Save a Capture at {path}");
            }
#endif
        }

        public static Sprite LoadCapture(string fileName, bool decrypt = false)
        {
            return _captureProcessor.Read(fileName, decrypt);
        }

        public static void LoadCaptureAsync(string fileName, Action<Sprite> action, bool decrypt = false)
        {
            _captureProcessor.ReadAsync(fileName, action, decrypt);
        }

        public static YieldInstructionCompletionSource<Sprite> LoadCaptureAsYieldInstruction(string fileName, bool decrypt = false)
        {
            var token = new YieldInstructionCompletionSource<Sprite>();
            _captureProcessor.ReadAsync(fileName, token.SetResult, decrypt);
            return token;
        }

#if !UNITY_WEBGL
        public static Task<Sprite> LoadCaptureAsTask(string fileName, bool decrypt = false)
        {
            var tcs = new TaskCompletionSource<Sprite>();
            _captureProcessor.ReadAsync(fileName, tcs.SetResult, decrypt);
            return tcs.Task;
        }
#endif

        public static void DeleteCapture(string fileName)
        {
            _captureProcessor.Clear(fileName);
        }

        public static void ClearCapture()
        {
            _captureProcessor.ClearAll();
        }

        #endregion
    }
}