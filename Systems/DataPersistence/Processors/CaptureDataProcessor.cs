using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerCellStudio
{
    public class CaptureDataProcessor : BasePlayerDataProcessor, IPlayerDataSaver<Texture2D>, IPlayerDataReader<Sprite>
    {
        private static readonly string _directoryName = "Capture";

        public override string directoryName => _directoryName;

        private static readonly string _extension = "png";

        public override string extension => _extension;

        [CLSCompliant(false)]
        public Sprite Read(string saveKey, bool decrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var path)) return null;
            if (!File.Exists(path)) return null;
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] imgByte = new byte[stream.Length];
            var read = stream.Read(imgByte, 0, imgByte.Length);
            stream.Close();
            stream.Dispose();
            var decryptedData = decrypt ? EncryptUtils.AESDecrypt(imgByte, ConstSetting.FileEncryptionKey) : imgByte;
            Texture2D texture2D = new Texture2D(640, 360);
            texture2D.LoadImage(decryptedData);
            return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
        }

        [CLSCompliant(false)]
        public void ReadAsync(string saveKey, Action<Sprite> onComplete, bool decrypt)
        {
            if (!TryGetSaveFilePath(saveKey, out var path))
            {
                onComplete?.Invoke(null);
                return;
            }
            // 使用 file:// 协议加载本地文件
            string url = "file://" + path;
            ApplicationManager.RunCoroutine(LoadImageCoroutine(url, onComplete, decrypt));
        }

        private IEnumerator LoadImageCoroutine(string url, Action<Sprite> onComplete, bool decrypt)
        {
            if (decrypt)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        byte[] encryptedData = request.downloadHandler.data;
                        var decryptedData = EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey);
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(decryptedData);
                        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        onComplete?.Invoke(sprite);
                        yield break;
                    }
                    LinkLog.LogError("加载图片失败: " + request.error);
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(request);
                        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        onComplete?.Invoke(sprite);
                        yield break;
                    }
                    LinkLog.LogError("加载图片失败: " + request.error);
                    onComplete?.Invoke(null);
                }
            }
        }

        [CLSCompliant(false)]
        public bool Save(string saveKey, Texture2D data, bool encrypt)
        {
            if (data == null) return false;
            if (!TryGetSaveFilePath(saveKey, out var path)) return false;
            CheckDirectory();
            var bytes = data.EncodeToPNG();
            if (encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
            File.WriteAllBytes(path, bytes);
            return true;
        }

        [CLSCompliant(false)]
        public void SaveAsync(string saveKey, Texture2D data, Action<bool> onComplete, bool encrypt)
        {
#if UNITY_WEBGL
            var isSuccess = Save<T>(saveKey, data, encrypt);
            onComplete?.Invoke(isSuccess);
#else
            SaveDataCaptureHandler(saveKey, data, onComplete, encrypt);
#endif
        }

        private async void SaveDataCaptureHandler(string saveKey, Texture2D data, Action<bool> onComplete, bool encrypt)
        {
            if (data == null || !TryGetSaveFilePath(saveKey, out var path))
            {
                onComplete?.Invoke(false);
                return;
            }
            CheckDirectory();
            var bytes = data.EncodeToPNG();
            if (encrypt) bytes = EncryptUtils.AESEncrypt(bytes, ConstSetting.FileEncryptionKey);
            await File.WriteAllBytesAsync(path, bytes);
            onComplete?.Invoke(true);
        }
    }
}