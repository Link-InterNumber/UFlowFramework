using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace PowerCellStudio
{
    /// <summary>
    /// 玩家数据类型枚举。
    /// Enumeration for player data types.
    /// </summary>
    public enum PlayerDataType
    {
        Json = 0,
        Binary,
        PlayerPrefs,
    }

    /// <summary>
    /// 玩家数据工具类，提供数据保存、读取、清除等功能。
    /// Utility class for player data, providing save, read, and clear functionalities.
    /// </summary>
    public static partial class PlayerDataUtils
    {
        private static PersistenceDataProcessor[] _persistenceDataProcessors;
        private static readonly CaptureDataProcessor _captureProcessor = new CaptureDataProcessor();

        private static void Init()
        {
            // 旧数据转换器列表初始化
            var dataTranslators = new List<DataTranslatorBase>();
            var dataTranslatorType = typeof(DataTranslatorBase);
            // 存储处理器数组初始化
            var enumValues = Enum.GetValues(typeof(PlayerDataType));
            _persistenceDataProcessors = new PersistenceDataProcessor[enumValues.Length];
            var persistenceDataProcessorType = typeof(PersistenceDataProcessor);

            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;

                if (dataTranslatorType.IsAssignableFrom(type))
                {
                    var instance = (DataTranslatorBase)Activator.CreateInstance(type);
                    dataTranslators.Add(instance);
                    continue;
                }

                if (!persistenceDataProcessorType.IsAssignableFrom(type))
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

            // 执行数据转换
            if (dataTranslators.Count == 0) return;
            dataTranslators.Sort((a, b) => a.version.CompareTo(b.version));
            foreach (var translator in dataTranslators)
            {
                try
                {
                    translator.TryTranslator();
                }
                catch (Exception e)
                {
                    LinkLog.LogError($"[PlayerDataUtils] Data translation failed for {translator.GetType().Name}:\n {e.Message}");
                }
            }
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

        /// <summary>
        /// 检查是否存在指定键的数据。
        /// Checks if data exists for the specified key.
        /// </summary>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <returns>如果存在数据，则返回 true；否则返回 false。Returns true if the data exists; otherwise, false.</returns>
        public static bool HasSave(string saveKey, PlayerDataType dataType)
        {
            var processor = GetProcessor(PlayerDataType.PlayerPrefs);
            if (processor == null) return false;
            return processor.HasSave(saveKey);
        }

        /// <summary>
        /// 检查是否存在指定类型的数据。
        /// Checks if data exists for the specified type.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <returns>如果存在数据，则返回 true；否则返回 false。Returns true if the data exists; otherwise, false.</returns>
        public static bool HasSave<T>(PlayerDataType dataType)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return HasSave(key, dataType);
        }

        #region Save
        /// <summary>
        /// 保存数据到指定键。
        /// Saves data to the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
        /// <returns>如果保存成功，则返回 true；否则返回 false。Returns true if the data is saved successfully; otherwise, false.</returns>
        public static bool Save<T>(string saveKey, T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return false;
            return processor.Save<T>(saveKey, data, encrypt);
        }

        /// <summary>
        /// 保存数据到默认键。
        /// Saves data to the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
        /// <returns>如果保存成功，则返回 true；否则返回 false。Returns true if the data is saved successfully; otherwise, false.</returns>
        public static bool Save<T>(T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return Save<T>(key, data, dataType, encrypt);
        }

        /// <summary>
        /// 异步保存数据到指定键。
        /// Asynchronously saves data to the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="onComplete">保存完成时的回调。The callback to invoke when the save operation is complete.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
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

        /// <summary>
        /// 异步保存数据到默认键。
        /// Asynchronously saves data to the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="onComplete">保存完成时的回调。The callback to invoke when the save operation is complete.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
        public static void SaveAsync<T>(T data, PlayerDataType dataType, Action<bool> onComplete, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            SaveAsync<T>(key, data, dataType, onComplete, encrypt);
        }

        /// <summary>
        /// 作为协程保存数据到指定键。
        /// Saves data as a coroutine to the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
        /// <returns>
        /// 返回一个协程等待对象，用于在协程中等待保存完成。
        /// Returns a coroutine wait object to wait for the save operation to complete in a coroutine.
        /// </returns> 
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

        /// <summary>
        /// 作为协程保存数据到默认键。
        /// Saves data as a coroutine to the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
        /// <returns>
        /// 返回一个协程等待对象，用于在协程中等待保存完成。
        /// Returns a coroutine wait object to wait for the save operation to complete in a coroutine.
        /// </returns>
        public static YieldInstructionCompletionSource<bool> SaveAsYieldInstruction<T>(T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return SaveAsYieldInstruction<T>(key, data, dataType, encrypt);
        }

#if !UNITY_WEBGL
        /// <summary>
        /// 以任务形式保存数据到指定键。
        /// Saves data as a task to the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
        /// <returns>
        /// 返回一个任务对象，表示保存操作的异步结果。
        /// Returns a task object representing the asynchronous result of the save operation.
        /// </returns>
        public static Task<bool> SaveAsTask<T>(string saveKey, T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return Task.FromResult(false);
            var tcs = new TaskCompletionSource<bool>();
            processor.SaveAsync<T>(saveKey, data, tcs.SetResult, encrypt);
            return tcs.Task;
        }

        /// <summary>
        /// 以任务形式保存数据到默认键。
        /// Saves data as a task to the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="data">要保存的数据。The data to save.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="encrypt">是否加密数据。Whether to encrypt the data.</param>
        /// <returns>
        /// 返回一个任务对象，表示保存操作的异步结果。
        /// Returns a task object representing the asynchronous result of the save operation.
        /// </returns>
        public static Task<bool> SaveAsTask<T>(T data, PlayerDataType dataType, bool encrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return SaveAsTask<T>(key, data, dataType, encrypt);
        }
#endif

        #endregion

        #region Read
        /// <summary>
        /// 从指定键读取数据。
        /// Reads data from the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>读取的数据。The data that was read.</returns>
        public static T Read<T>(string saveKey, PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return default;
            return processor.Read<T>(saveKey, decrypt);
        }

        /// <summary>
        /// 从默认键读取数据。
        /// Reads data from the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>读取的数据。The data that was read.</returns>
        public static T Read<T>(PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return Read<T>(key, dataType, decrypt);
        }

        /// <summary>
        /// 异步从指定键读取数据。
        /// Asynchronously reads data from the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="onComplete">读取完成时的回调。The callback to invoke when the read operation is complete.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>读取的数据。The data that was read.</returns>
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

        /// <summary>
        /// 异步从默认键读取数据。
        /// Asynchronously reads data from the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="onComplete">读取完成时的回调。The callback to invoke when the read operation is complete.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>读取的数据。The data that was read.</returns>
        public static void ReadAsync<T>(PlayerDataType dataType, Action<T> onComplete, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            ReadAsync<T>(key, dataType, onComplete, decrypt);
        }

        /// <summary>
        /// 作为协程从指定键读取数据。
        /// Reads data as a coroutine from the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>
        /// 返回一个协程等待对象，用于在协程中等待读取完成。
        /// Returns a coroutine wait object to wait for the read operation to complete in a coroutine.
        /// </returns>
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

        /// <summary>
        /// 作为协程从默认键读取数据。
        /// Reads data as a coroutine from the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>
        /// 返回一个协程等待对象，用于在协程中等待读取完成。
        /// Returns a coroutine wait object to wait for the read operation to complete in a coroutine.
        /// </returns>
        public static YieldInstructionCompletionSource<T> ReadAsYieldInstruction<T>(PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadAsYieldInstruction<T>(key, dataType, decrypt);
        }

#if !UNITY_WEBGL
        /// <summary>
        /// 以任务形式从指定键读取数据。
        /// Reads data as a task from the specified key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>
        /// 返回一个任务对象，表示读取操作的异步结果。
        /// Returns a task object representing the asynchronous result of the read operation.
        /// </returns>
        public static Task<T> ReadAsTask<T>(string saveKey, PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return default;
            var tcs = new TaskCompletionSource<T>();
            processor.ReadAsync<T>(saveKey, tcs.SetResult, decrypt);
            return tcs.Task;
        }

        /// <summary>
        /// 以任务形式从默认键读取数据。
        /// Reads data as a task from the default key.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="dataType">数据类型。The data type.</param>
        /// <param name="decrypt">是否解密数据。Whether to decrypt the data.</param>
        /// <returns>
        /// 返回一个任务对象，表示读取操作的异步结果。
        /// Returns a task object representing the asynchronous result of the read operation.
        /// </returns>
        public static Task<T> ReadAsTask<T>(PlayerDataType dataType, bool decrypt = true)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadAsTask<T>(key, dataType, decrypt);
        }
#endif

        #endregion

        #region Clear
        /// <summary>
        /// 清除指定键的数据。
        /// Clears the data for the specified key.
        /// </summary>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="dataType">数据类型。The data type.</param>
        public static void Clear(string saveKey, PlayerDataType dataType)
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return;
            processor.Clear(saveKey);
        }

        /// <summary>
        /// 清除指定类型的数据。
        /// Clears the data for the specified type.
        /// </summary>
        /// <typeparam name="T">数据的类型。The type of the data.</typeparam>
        /// <param name="dataType">数据类型。The data type.</param>
        public static void Clear<T>(PlayerDataType dataType)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            Clear(key, dataType);
        }

        /// <summary>
        /// 清除所有指定数据类型的数据。
        /// Clears all data of the specified data type.
        /// </summary>
        /// <param name="dataType">数据类型。The data type.</param>
        public static void ClearAll(PlayerDataType dataType)
        {
            var processor = GetProcessor(dataType);
            if (processor == null) return;
            processor.ClearAll();
        }

        /// <summary>
        /// 清除所有数据。
        /// Clears all data.
        /// </summary>
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

        /// <summary>
        /// 是否存在指定名称的截图。
        /// Checks if a capture with the specified name exists.
        /// </summary>
        /// <param name="fileName">截图文件名。The name of the capture file
        /// </param>
        /// <returns>如果存在截图，则返回 true；否则返回 false。Returns true if the capture exists; otherwise, false.</returns>
        public static bool HasCapture(string fileName)
        {
            return _captureProcessor.HasSave(fileName);
        }

        #region PlayerPrefsSave
        /// <summary>
        /// 保存整数到 PlayerPrefs。
        /// Saves an integer to PlayerPrefs.
        /// </summary>
        public static void SavePlayerPrefs(string key, int data)
        {
            PlayerPrefs.SetInt(key, data);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 保存字符串到 PlayerPrefs。
        /// Saves a string to PlayerPrefs.
        /// </summary>
        public static void SavePlayerPrefs(string key, string data)
        {
            PlayerPrefs.SetString(key, data);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 保存浮点数到 PlayerPrefs。
        /// Saves a float to PlayerPrefs.
        /// </summary>
        public static void SavePlayerPrefs(string key, float data)
        {
            PlayerPrefs.SetFloat(key, data);
            PlayerPrefs.Save();
        }

        #endregion

        #region PlayerPrefsRead

        /// <summary>
        /// 从 PlayerPrefs 读取整数。
        /// Reads an integer from PlayerPrefs.
        /// </summary>
        public static int ReadPlayerInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        /// <summary>
        /// 从 PlayerPrefs 读取字符串。
        /// Reads a string from PlayerPrefs.
        /// </summary>
        public static string ReadPlayerString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        /// <summary>
        /// 从 PlayerPrefs 读取浮点数。
        /// Reads a float from PlayerPrefs.
        /// </summary>
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

        /// <summary>
        /// 截取屏幕或摄像机画面并保存为图片。
        /// Captures the screen or camera view and saves it as an image.
        /// </summary>
        /// <param name="fileName">保存的文件名（不含扩展名）。The name of the file to save (without extension).</param>
        /// <param name="rect">截取区域。The area to capture.</param>
        /// <param name="camera">可选的摄像机对象。如果为 null，则截取屏幕。Optional camera object. If null, captures the screen.</param>
        /// <param name="encrypt">是否加密保存的图片。Whether to encrypt the saved image.</param>
        /// <returns>返回一个协程对象，用于在协程中等待截取完成。</returns>
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

        /// <summary>
        /// 加载已保存的截图。
        /// Loads a saved capture.
        /// </summary>
        /// <param name="fileName">截图文件名。The name of the capture file.</param>
        /// <param name="decrypt">是否解密图片。Whether to decrypt the image.</
        /// param>
        /// <returns>返回加载的截图精灵对象。</returns>
        public static Sprite LoadCapture(string fileName, bool decrypt = false)
        {
            return _captureProcessor.Read(fileName, decrypt);
        }

        /// <summary>
        /// 异步加载已保存的截图。
        /// Asynchronously loads a saved capture.
        /// </summary>
        /// <param name="fileName">截图文件名。The name of the capture file
        /// </param>
        /// <param name="action">加载完成时的回调。The callback to invoke
        /// </param>
        /// <param name="decrypt">是否解密图片。Whether to decrypt the image.</param>
        public static void LoadCaptureAsync(string fileName, Action<Sprite> action, bool decrypt = false)
        {
            _captureProcessor.ReadAsync(fileName, action, decrypt);
        }

        /// <summary>
        /// 作为协程加载已保存的截图。
        /// Loads a saved capture as a coroutine.
        /// </summary>
        /// <param name="fileName">截图文件名。The name of the capture file.</param>
        /// <param name="decrypt">是否解密图片。Whether to decrypt the image.</param>
        /// <returns>
        /// 返回一个协程等待对象，用于在协程中等待加载完成。
        /// Returns a coroutine wait object to wait for the load operation to complete in a coroutine.
        /// </returns>
        public static YieldInstructionCompletionSource<Sprite> LoadCaptureAsYieldInstruction(string fileName, bool decrypt = false)
        {
            var token = new YieldInstructionCompletionSource<Sprite>();
            _captureProcessor.ReadAsync(fileName, token.SetResult, decrypt);
            return token;
        }

#if !UNITY_WEBGL
        /// <summary>
        /// 以任务形式加载已保存的截图。
        /// Loads a saved capture as a task.
        /// </summary>
        /// <param name="fileName">截图文件名。The name of the capture file.</param>
        /// <param name="decrypt">是否解密图片。Whether to decrypt the image.</param>
        /// <returns>
        /// 返回一个任务对象，表示加载操作的异步结果。
        /// Returns a task object representing the asynchronous result of the load operation.
        /// </returns>
        public static Task<Sprite> LoadCaptureAsTask(string fileName, bool decrypt = false)
        {
            var tcs = new TaskCompletionSource<Sprite>();
            _captureProcessor.ReadAsync(fileName, tcs.SetResult, decrypt);
            return tcs.Task;
        }
#endif

        /// <summary>
        /// 删除已保存的截图。
        /// Deletes a saved capture.
        /// </summary>
        /// <param name="fileName">截图文件名。The name of the capture file.</param>
        public static void DeleteCapture(string fileName)
        {
            _captureProcessor.Clear(fileName);
        }

        /// <summary>
        /// 清除所有截图。
        /// Clears all captures.
        /// </summary>
        public static void ClearCapture()
        {
            _captureProcessor.ClearAll();
        }

        #endregion
    }
}