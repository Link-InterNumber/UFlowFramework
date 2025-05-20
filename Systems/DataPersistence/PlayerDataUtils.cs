using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if !UNITY_WEBGL
using System.Threading.Tasks;
using Microsoft.Xbox.Services.Client;
#endif
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public static partial class PlayerDataUtils
    {
        public static readonly string SavePath = $"{Application.persistentDataPath}";

        private static readonly string JsonDirectory = "Json";
        private static readonly string BinaryDirectory = "Binary";
        private static readonly string CaptureDirectory = "Capture";

        private static void CheckDirectory(string name)
        {
            var directory = Path.Combine(SavePath, name);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }

        public static bool HasPlayerPrefsSave(string saveKey)
        {
            return PlayerPrefs.HasKey(saveKey);
        }
        
        public static bool HasPlayerPrefsSave<T>()
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return PlayerPrefs.HasKey(key);
        }
        
        public static bool HasJsonSave(string fileName)
        {
            var directory = Path.Combine(SavePath, JsonDirectory);
            if (!Directory.Exists(directory)) return false;
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            return File.Exists(path);
        }
        
        public static bool HasJsonSave<T>()
            where T : IPersistenceData
        {
            var directory = Path.Combine(SavePath, JsonDirectory);
            if (!Directory.Exists(directory)) return false;
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            return File.Exists(path);
        }
        
        public static bool HasBinarySave(string fileName)
        {
            var directory = Path.Combine(SavePath, BinaryDirectory);
            if (!Directory.Exists(directory)) return false;
            var path = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            return File.Exists(path);
        }
        
        public static bool HasBinarySave<T>()
            where T : IPersistenceData
        {
            var directory = Path.Combine(SavePath, BinaryDirectory);
            if (!Directory.Exists(directory)) return false;
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            var path = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            return File.Exists(path);
        }

        public static bool HasCapture(string fileName)
        {
            var directory = Path.Combine(SavePath, CaptureDirectory);
            if (!Directory.Exists(directory)) return false;
            var path = Path.Combine(SavePath, CaptureDirectory, $"{fileName}.png");
            return File.Exists(path);
        }
        
        #region PlayerPrefsSave

        public static void SavePlayerPrefs<T>(string key, T data)
            where T : IPersistenceData
        {
            string json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public static void SavePlayerPrefs<T>(T data)
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            SavePlayerPrefs(key, data);
        }

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

        public static T ReadPlayerPrefs<T>(string key)
            where T : IPersistenceData
        {
            var json = PlayerPrefs.GetString(key, "{}");
            return JsonConvert.DeserializeObject<T>(json);
        }
        
        public static T ReadPlayerPrefs<T>()
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadPlayerPrefs<T>(key);
        }

        public static void ClearPlayerPrefs(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
        
        public static void ClearPlayerPrefs<T>()
            where T : IPersistenceData
        {
            var key = $"{typeof(T).Namespace}_{typeof(T).Name}";
            PlayerPrefs.DeleteKey(key);
        }
        
        public static void ClearAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }

        #endregion

        #region JsonSave

        private static string Encrypt(string data)
        {
            return EncryptUtils.Base64Encrypt(data);
        }

        private static string Decrypt(string data)
        {
            return EncryptUtils.Base64Decrypt(data);
        }
        
        public static Coroutine SaveDebugLog(string fileName, DebugSave data, Action action = null)
        {
            if(string.IsNullOrEmpty(fileName)) return null;
            return ApplicationManager.instance.StartCoroutine(SaveJsonHandle($"{fileName}_DebugLog", data, action, false));
        }

#if !UNITY_WEBGL
        public static async Task SaveJson<T>(string fileName, T data, bool encrypt = true)
            where T : IPersistenceData
        {
            if (string.IsNullOrEmpty(fileName)) return;
            CheckDirectory(JsonDirectory);
            string json = JsonConvert.SerializeObject(data);
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            if (encrypt)
            {
                var jsonEn = Encrypt(json);
                await File.WriteAllTextAsync(path, jsonEn);
            }
            else
            {
                await File.WriteAllTextAsync(path, json);
            }
#if UNITY_EDITOR
            LinkLog.Log($"Save a Json at {path}");
#endif
        }

        public static async Task SaveJson<T>(T data, bool encrypt = true)
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            await SaveJson(fileName, data, encrypt);
        }
#endif

        public static Coroutine SaveJsonAsync<T>(string fileName, T data, Action action = null, bool encrypt = true)
            where T : IPersistenceData
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            return ApplicationManager.instance.StartCoroutine(SaveJsonHandle(fileName, data, action, encrypt));
        }
        
        public static Coroutine SaveJsonAsync<T>(T data, Action action = null, bool encrypt = true)
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            if (string.IsNullOrEmpty(fileName)) return null;
            return ApplicationManager.instance.StartCoroutine(SaveJsonHandle(fileName, data, action, encrypt));
        }
        
        private static IEnumerator SaveJsonHandle<T>(string fileName, T data, Action action, bool encrypt = true)
            where T : IPersistenceData
        {
            CheckDirectory(JsonDirectory);
            yield return null;
            string json = JsonConvert.SerializeObject(data);
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            if (encrypt)
            {
                var jsonEn = Encrypt(json);
                yield return File.WriteAllTextAsync(path, jsonEn).AsCoroutine();
            }
            else
            {
                yield return File.WriteAllTextAsync(path, json).AsCoroutine();
            }
            action?.Invoke();
#if UNITY_EDITOR
            LinkLog.Log($"Save a Json at {path}");
#endif
        }

        #endregion

        #region JsonRead
        
        public static T ReadJson<T>(string fileName, bool decrypt = true)
            where T : IPersistenceData
        {
            if(string.IsNullOrEmpty(fileName)) return default;
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            if (!File.Exists(path)) return default;
            var jsonEn = File.ReadAllText(path);
            if (decrypt)
            {
                var json = Decrypt(jsonEn);
                return JsonConvert.DeserializeObject<T>(json);
            }
            return JsonConvert.DeserializeObject<T>(jsonEn);
        }

        public static T ReadJson<T>(bool decrypt = true)
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadJson<T>(fileName, decrypt);
        }
        
        public static LoaderYieldInstruction<T> ReadJsonAsync<T>(string fileName, UnityAction<T> action, bool decrypt = true)
            where T : class, IPersistenceData
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            var loadHandler = new LoaderYieldInstruction<T>(path);
            if (action != null) loadHandler.onLoadSuccess += (savedData, path) => {action.Invoke(savedData);};
            ApplicationManager.instance.StartCoroutine(ReadJsonHandle(path, loadHandler, decrypt));
            return loadHandler;
        }
        
        public static LoaderYieldInstruction<T> ReadJsonAsync<T>(UnityAction<T> action, bool decrypt = true)
            where T : class, IPersistenceData
        {
            var typ = typeof(T);
            var fileName = $"{typ.Namespace}_{typ.Name}";
            // if (string.IsNullOrEmpty(fileName)) return null;
            // var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            // var loaderYieldInstruction = new LoaderYieldInstruction<T>(path)
            // if (action != null) loaderYieldInstruction.onLoadSuccess += (savedData, path) => {action.Invoke(savedData)};
            // ApplicationManager.instance.StartCoroutine(ReadJsonHandle(fileName, actiloaderYieldInstructionon, decrypt));
            return ReadJsonAsync<T>(fileName, action, decrypt);
        }
        
        private static IEnumerator ReadJsonHandle<T>(string path, LoaderYieldInstruction<T> loadHandler, bool decrypt)
            where T : class, IPersistenceData
        {
            if (!File.Exists(path)) 
            {
                loadHandler.SetAsset(null);
                yield break;
            }
            string uri = "file://" + path;
            T data = null;
            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonEn = request.downloadHandler.text;
                    var json = decrypt ? Decrypt(jsonEn) : jsonEn;
                    data = JsonConvert.DeserializeObject<T>(json);
                }
                else
                {
                    Debug.LogError("Failed to read file: " + request.error);
                }
            }
            loadHandler.SetAsset(data);
            // var task = File.ReadAllTextAsync(path);
            // yield return task.AsCoroutine();
            // var jsonEn = task.Result;
            // if (decrypt)
            // {
            //     var json = Decrypt(jsonEn);
            //     var dataDe = JsonConvert.DeserializeObject<T>(json);
            //     loadHandler.SetAsset(dataDe);
            //     yield break;
            // }
            // var data = JsonConvert.DeserializeObject<T>(jsonEn);
            // loadHandler.SetAsset(data);
        }
        
        public static void ClearJson(string fileName)
        {
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }
        
        public static void ClearJson<T>()
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            var path = Path.Combine(SavePath, JsonDirectory, $"{fileName}.json");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }
        
        public static void ClearAllJson()
        {
            var path = Path.Combine(SavePath, JsonDirectory);
            if (!Directory.Exists(path)) return;
            DirectoryInfo di = new DirectoryInfo(path);
            di.Delete(true);
        }

        #endregion

        #region BinarySave
        
        public static void SaveDataBinary<T>(string fileName, T data, bool encrypt = true)
            where T : IPersistenceData
        {
            if (string.IsNullOrEmpty(fileName)) return;
            CheckDirectory(BinaryDirectory);
            var filePath = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, data);
                var bytes = memoryStream.ToArray();
                if(encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                File.WriteAllBytes(filePath, bytes);
                memoryStream.Close();
            }
            LinkLog.Log($"Save a Binary at {filePath}");
        }

        public static void SaveDataBinary<T>(T data, bool encrypt = true)
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            SaveDataBinary(fileName, data, encrypt);
        }

#if !UNITY_WEBGL
        public static async Task SaveDataBinaryTask<T>(string fileName, T data, bool encrypt = true)
            where T : IPersistenceData
        {
            if(string.IsNullOrEmpty(fileName)) return;
            CheckDirectory(BinaryDirectory);
            var filePath = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, data);
                var bytes = memoryStream.ToArray();
                if(encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                await File.WriteAllBytesAsync(filePath, bytes);
                memoryStream.Close();
            }
            LinkLog.Log($"Save a Binary at {filePath}");
        }

        public static async Task SaveDataBinaryTask<T>(T data, bool encrypt = true)
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            await SaveDataBinaryTask(fileName, data, encrypt);
        }
#endif

        public static Coroutine SaveDataBinaryAsync<T>(string fileName, T data, Action action, bool encrypt = true)
            where T : IPersistenceData
        {
            if(string.IsNullOrEmpty(fileName)) return null;
            return ApplicationManager.instance.StartCoroutine(SaveDataBinaryHandler(fileName, data, action, encrypt));
        }

        public static Coroutine SaveDataBinaryAsync<T>(T data, Action action, bool encrypt = true)
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return SaveDataBinaryAsync(fileName, data, action, encrypt);
        }
        
        private static IEnumerator SaveDataBinaryHandler<T>(string fileName, T data, Action action, bool encrypt = true)
            where T : IPersistenceData
        {
            CheckDirectory(BinaryDirectory);
            var filePath = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            // 创建一个文件流用于保存数据
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, data);
                var bytes = memoryStream.ToArray();
                if(encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
                yield return File.WriteAllBytesAsync(filePath, bytes).AsCoroutine();
                memoryStream.Close();
            }
            action?.Invoke();
            LinkLog.Log($"Save a Binary at {filePath}");
        }

        #endregion

        #region BinaryRead

        private static IEnumerator ReadBinaryCoroutineHandle<T>(string filePath, LoaderYieldInstruction<T> loadHandler, bool decrypt = true)
            where T : class, IPersistenceData
        {
            // if (string.IsNullOrEmpty(fileName)) yield break;
            // var filePath = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            if (!File.Exists(filePath)) 
            {
                loadHandler.SetAsset(null);
                yield break;
            }

            string uri = "file://" + filePath;
            byte[] decryptedData = null;
            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var encryptedData = request.downloadHandler.data;
                    decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
                }
                else
                {
                    Debug.LogError("Failed to read file: " + request.error);
                }
            }
            if (decryptedData == null)
            {
                loadHandler.SetAsset(null);
                yield break;
            }

            T data = null;
            using (MemoryStream memoryStream = new MemoryStream(decryptedData))
            {
                // 使用BinaryFormatter进行反序列化
                BinaryFormatter formatter = new BinaryFormatter();
                data = (T)formatter.Deserialize(memoryStream);
                // 关闭文件流
                memoryStream.Close();
            }
            loadHandler.SetAsset(data);
        }
        
        public static LoaderYieldInstruction<T> ReadBinaryAsync<T>(string fileName, Action<T> callback, bool decrypt = true)
            where T : class, IPersistenceData
        {
            if(string.IsNullOrEmpty(fileName)) return null;
            var filePath = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            var loadHandler = new LoaderYieldInstruction<T>(filePath);
            if (callback != null) loadHandler.onLoadSuccess += (savedData, path) => {callback.Invoke(savedData);};
            ApplicationManager.instance.StartCoroutine(ReadBinaryCoroutineHandle(filePath, loadHandler, decrypt));
            return loadHandler;
        }
        
        public static LoaderYieldInstruction<T> ReadBinaryAsync<T>(Action<T> callback, bool decrypt = true)
            where T : class, IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadBinaryAsync(fileName, callback, decrypt);
        }
        
        // public static async Task<T> ReadBinaryTask<T>(string fileName, bool decrypt = true)
        //     where T : IPersistenceData
        // {
        //     if(string.IsNullOrEmpty(fileName)) return default;
        //     var filePath = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
        //     if (!File.Exists(filePath)) return default;
        //     byte[] encryptedData = await File.ReadAllBytesAsync(filePath);
        //     var decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
        //     using MemoryStream memoryStream = new MemoryStream(decryptedData);
        //     // 使用BinaryFormatter进行反序列化
        //     BinaryFormatter formatter = new BinaryFormatter();
        //     T data = (T)formatter.Deserialize(memoryStream);
        //     // 关闭文件流
        //     memoryStream.Close();
        //     return data;
        // }
        
        // public static async Task<T> ReadBinaryTask<T>(bool decrypt = true)
        //     where T : IPersistenceData
        // {
        //     var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
        //     return await ReadBinaryTask<T>(fileName, decrypt);
        // }

        public static T ReadBinary<T>(string fileName, bool decrypt = true)
            where T : IPersistenceData
        {
            if(string.IsNullOrEmpty(fileName)) return default;
            var filePath = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            if (!File.Exists(filePath)) return default;
            byte[] encryptedData = File.ReadAllBytes(filePath);
            var decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
            using MemoryStream memoryStream = new MemoryStream(decryptedData);
            // 使用BinaryFormatter进行反序列化
            BinaryFormatter formatter = new BinaryFormatter();
            T data = (T)formatter.Deserialize(memoryStream);
            // 关闭文件流
            memoryStream.Close();
            return data;
        }

        public static T ReadBinary<T>(bool decrypt = true)
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            return ReadBinary<T>(fileName, decrypt);
        }

        public static void ClearBinary(string fileName)
        {
            var path = Path.Combine(SavePath, BinaryDirectory, $"{fileName}.bytes");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }
        
        public static void ClearBinary<T>()
            where T : IPersistenceData
        {
            var fileName = $"{typeof(T).Namespace}_{typeof(T).Name}";
            ClearBinary(fileName);
        }
        
        public static void ClearAllBinary()
        {
            var path = Path.Combine(SavePath, BinaryDirectory);
            if (!Directory.Exists(path)) return;
            DirectoryInfo di = new DirectoryInfo(path);
            di.Delete(true);
        }

        #endregion

        #region Capture

        private static bool captureTakeing = false;

        public static Coroutine TakeCapture(string fileName, Rect rect, Camera camera = null)
        {
            if (captureTakeing) return null;
            captureTakeing = true;
            if (camera == null)
                return ApplicationManager.instance.StartCoroutine(ScreenCapture(fileName, rect));
            return ApplicationManager.instance.StartCoroutine(CameraCapture(camera, fileName, rect));
        }

        private static IEnumerator CameraCapture(Camera camera, string fileName, Rect rect)
        {
            CheckDirectory(CaptureDirectory);
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(SavePath, CaptureDirectory, $"{fileName}.png");
            RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, 0);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            Texture2D texture2D = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            texture2D.ReadPixels(rect, 0,0);
            texture2D.Apply();
            var bytes = texture2D.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(texture2D);
            captureTakeing = false;
#if UNITY_EDITOR
            LinkLog.Log($"Save a camera capture at {path}");
#endif
        }
        
        private static IEnumerator ScreenCapture(string fileName, Rect rect)
        {
            CheckDirectory(CaptureDirectory);
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(SavePath, CaptureDirectory, $"{fileName}.png");
            Texture2D texture2D = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
            texture2D.ReadPixels(rect, 0,0);
            texture2D.Apply();
            var pngbytes = texture2D.EncodeToPNG();
            File.WriteAllBytes(path, pngbytes);
            Object.Destroy(texture2D);
            captureTakeing = false;
#if UNITY_EDITOR
            LinkLog.Log($"Save a Capture at {path}");
#endif
        }

        public static Sprite LoadCapture(string fileName)
        {
            var path = Path.Combine(SavePath, CaptureDirectory, $"{fileName}.png");
            if (!File.Exists(path)) return null;
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] imgByte = new byte[stream.Length];
            var read = stream.Read(imgByte, 0, imgByte.Length);
            stream.Close();
            stream.Dispose();
            Texture2D texture2D = new Texture2D(640, 360);
            texture2D.LoadImage(imgByte);
            return Sprite.Create(texture2D, new Rect(0f,0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
        }
        
        public static LoaderYieldInstruction<Sprite> LoadCaptureAsync(string fileName, Action<Sprite> action)
        {
            var path = Path.Combine(SavePath, CaptureDirectory, $"{fileName}.png");
            if (!File.Exists(path)) return null;
            var loadHandler = new LoaderYieldInstruction<Sprite>(path);
            if (action != null) loadHandler.onLoadSuccess += (asset, path) => {action.Invoke(asset);};
            ApplicationManager.instance.StartCoroutine(LoadCaptureHandle(path, loadHandler));
            return loadHandler;
        }
        
        public static IEnumerator LoadCaptureHandle(string path, LoaderYieldInstruction<Sprite> loadHandler)
        {
            if (!File.Exists(path)) 
            {
                loadHandler.SetAsset(null);
                yield break;
            }
            // 使用 file:// 协议加载本地文件
            string url = "file://" + path;
            Sprite sprite = null;
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                // 发送请求并等待完成
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // 获取 Texture2D
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);

                    // 创建 Sprite
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f) // 设置 Sprite 的锚点为中心
                    );
                }
                else
                {
                    Debug.LogError("加载图片失败: " + request.error);
                }
            }
            loadHandler.SetAsset(sprite);
            // FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            // byte[] imgByte = new byte[stream.Length];
            // yield return stream.ReadAsync(imgByte, 0, imgByte.Length).AsCoroutine();
            // stream.Close();
            // stream.Dispose();
            // Texture2D texture2D = new Texture2D(640, 360);
            // texture2D.LoadImage(imgByte);
            // var sprite =  Sprite.Create(texture2D, new Rect(0f,0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
            // action?.Invoke(sprite);
        }
        
        public static void DeleteCapture(string fileName)
        {
            var path = Path.Combine(SavePath, CaptureDirectory, $"{fileName}.png");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }
        
        public static void ClearCapture()
        {
            var path = Path.Combine(SavePath, CaptureDirectory);
            if (!Directory.Exists(path)) return;
            DirectoryInfo di = new DirectoryInfo(path);
            di.Delete(true);
        }

        #endregion

        public static void ClearAll()
        {
            ClearAllPlayerPrefs();
            ClearAllJson();
            ClearAllBinary();
            ClearCapture();
        }
    }
}