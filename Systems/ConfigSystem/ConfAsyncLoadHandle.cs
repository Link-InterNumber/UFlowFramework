using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using UnityEngine;

namespace PowerCellStudio
{
    // 用于将加载函数抽离出ConfBaseCollections
    // Used to extract the loading function out of ConfBaseCollections
    public abstract class ConfAsyncLoadHandle
    {
        public delegate void OnCompleted(ConfBaseData confData);
        public OnCompleted Completed;

        public void LoadAsync<T>(string path)  where T : ConfBaseData
        {
            switch (ConstSetting.ConfigConfigSaveMode)
            {
                case ConstSetting.ConfigSaveMode.ScriptableObject:
                    LoadScriptableObject(path);
                    break;
                case ConstSetting.ConfigSaveMode.Json:
                    LoadJson<T>(path);
                    break;
                case ConstSetting.ConfigSaveMode.Binary:
                    LoadBinary<T>(path);
                    break;
                default:
                    ConfigLog.LogError("Unknown ConfigSaveMode");
                    break;
            }
        }

        public abstract void LoadScriptableObject(string path);
        public abstract void LoadJson<T>(string path) where T : ConfBaseData;
        public abstract void LoadBinary<T>(string path) where T : ConfBaseData;

        public abstract void Release();
    }

    public class CommonConfigLoader : ConfAsyncLoadHandle
    {
        private IAssetLoader _loader;

        public override void LoadScriptableObject(string path)
        {
            _loader = AssetUtils.SpawnLoader("config");
#if SCRIPTABLE_OBJECT_CONFIG
            _loader.LoadAsync<ConfBaseData>(path, OnLoadCompleted, () => Completed?.Invoke(null));
#endif
        }

        private void OnLoadCompleted(ConfBaseData obj)
        {
            Completed?.Invoke(obj);
        }

        public override void LoadJson<T>(string path)
        {
            _loader = AssetUtils.SpawnLoader("config");
            _loader.LoadAsync<TextAsset>(path, OnLoadJsonCompleted<T>, () => Completed?.Invoke(null));
        }

        private void OnLoadJsonCompleted<T>(TextAsset obj) where T :ConfBaseData
        {
            var jsonString = EncryptUtils.AESDecrypt(obj.text, ConstSetting.FileEncryptionKey); // 解密配置文件
            var data = JsonConvert.DeserializeObject<T>(jsonString);
            Completed?.Invoke(data);
        }

        public override void LoadBinary<T>(string path)
        {
            _loader = AssetUtils.SpawnLoader("config");
            _loader.LoadAsync<TextAsset>(path, OnLoadBinaryCompleted<T>, () => Completed?.Invoke(null));
        }

        private void OnLoadBinaryCompleted<T>(TextAsset obj) where T :ConfBaseData
        {
            var bytes = EncryptUtils.AESDecrypt(obj.bytes, ConstSetting.FileEncryptionKey); // 解密配置文件
            using MemoryStream stream = new MemoryStream(bytes);
            BinaryFormatter formatter = new BinaryFormatter();
            T data = (T) formatter.Deserialize(stream);
            stream.Close();
            Completed?.Invoke(data);
        }

        public override void Release()
        {
            AssetUtils.DeSpawnLoader(_loader);
            _loader = null;
        }
    }
    
    // // 我的项目里使用了Addressable来加载数据，因此创建了一个子类来处理这个用途。
    // // 如果你使用其他加载方式，可以替换成你自己的实现。
    // // In my case, I used Addressable to load the data, so I created a subclass of ConfAsyncLoadHandle for this purpose.
    // // You can replace this with your own implementation if needed.
    // public class ConfAsyncLoadHandleFromAddressable : ConfAsyncLoadHandle
    // {
    //     private AsyncOperationHandle<ConfBaseData> _handle;
    //     private AsyncOperationHandle<TextAsset> _handleTextAsset;
    //     
    //     
    //     public override void LoadScriptableObject(string path)
    //     {
    //         _handle = Addressables.LoadAssetAsync<ConfBaseData>(path);
    //         _handle.Completed += OnLoadCompleted;
    //     }
    //
    //     public override void LoadJson<T>(string path)
    //     {
    //         if(string.IsNullOrEmpty(path))
    //         {
    //             Completed?.Invoke(null);
    //             return;
    //         }
    //         _handleTextAsset = Addressables.LoadAssetAsync<TextAsset>(path);
    //         _handleTextAsset.Completed += OnLoadJsonCompleted<T>;
    //     }
    //
    //     private void OnLoadJsonCompleted<T>(AsyncOperationHandle<TextAsset> obj) where T :ConfBaseData
    //     {
    //         if (obj.Status != AsyncOperationStatus.Succeeded)
    //         {
    //             Completed?.Invoke(null);
    //             return;
    //         }
    //         var jsonString = EncryptUtils.AESDecrypt(obj.Result.text, ConstsSetting.BinaryEncryptionKey);
    //         var data = JsonConvert.DeserializeObject<T>(jsonString);
    //         Completed?.Invoke(data);
    //     }
    //
    //     public override void LoadBinary<T>(string path)
    //     {
    //         if(string.IsNullOrEmpty(path))
    //         {
    //             Completed?.Invoke(null);
    //             return;
    //         }
    //         _handleTextAsset = Addressables.LoadAssetAsync<TextAsset>(path);
    //         _handleTextAsset.Completed += OnLoadBinaryCompleted<T>;
    //     }
    //
    //     private void OnLoadBinaryCompleted<T>(AsyncOperationHandle<TextAsset> obj) where T : ConfBaseData
    //     {
    //         if (obj.Status != AsyncOperationStatus.Succeeded)
    //         {
    //             Completed?.Invoke(null);
    //             return;
    //         }
    //         var bytes = EncryptUtils.AESDecrypt(obj.Result.bytes, ConstsSetting.BinaryEncryptionKey);
    //         using MemoryStream stream = new MemoryStream(bytes);
    //         BinaryFormatter formatter = new BinaryFormatter();
    //         string json = (string) formatter.Deserialize(stream);
    //         var data = JsonConvert.DeserializeObject<T>(json);
    //         stream.Close();
    //         Completed?.Invoke(data);
    //     }
    //
    //     public override void Release()
    //     {
    //         if(_handleTextAsset.IsValid()) Addressables.Release(_handleTextAsset);
    //         if(!_handle.IsValid()) return;
    //         Addressables.Release(_handle);
    //     }
    //
    //     private void OnLoadCompleted(AsyncOperationHandle<ConfBaseData> obj)
    //     {
    //         Completed?.Invoke(obj.Result);
    //     }
    // }
    
    public class ConfAsyncLoadHandleFromResource : ConfAsyncLoadHandle
    {
        private ResourceRequest _handle;
        
        public override void LoadScriptableObject(string path)
        {
            var recoursePath = path.Split("/Resources/")[1];
            recoursePath = Path.GetFileNameWithoutExtension(recoursePath);
#if SCRIPTABLE_OBJECT_CONFIG
            _handle = Resources.LoadAsync<ConfBaseData>(recoursePath);
            ApplicationManager.instance.StartCoroutine(OnLoadCompleted(_handle));
#endif
        }
    
        public override void LoadJson<T>(string path)
        {
            var recoursePath = path.Split("/Resources/")[1];
            recoursePath = Path.GetFileNameWithoutExtension(recoursePath);
            _handle = Resources.LoadAsync<TextAsset>(recoursePath);
            ApplicationManager.instance.StartCoroutine(OnLoadJsonCompleted<T>(_handle));
        }
    
        private IEnumerator OnLoadJsonCompleted<T>(ResourceRequest handle) where T:ConfBaseData
        {
            yield return handle;
            if (handle.asset == null)
            {
                Completed?.Invoke(null);
                yield break;
            }
            var json = handle.asset as TextAsset;
            if (json == null)
            {
                Completed?.Invoke(null);
                yield break;
            }
            var jsonString = EncryptUtils.AESDecrypt(json.text, ConstSetting.FileEncryptionKey);
            var loaded = JsonConvert.DeserializeObject<T>(jsonString);
            Completed?.Invoke(loaded);
        }
    
        public override void LoadBinary<T>(string path)
        {
            var recoursePath = path.Split("/Resources/")[1];
            recoursePath = Path.GetFileNameWithoutExtension(recoursePath);
            _handle = Resources.LoadAsync<TextAsset>(recoursePath);
            ApplicationManager.instance.StartCoroutine(OnLoadBinaryCompleted<T>(_handle));
        }
    
        private IEnumerator OnLoadBinaryCompleted<T>(ResourceRequest handle) where T : ConfBaseData
        {
            yield return handle;
            if (handle.asset == null)
            {
                Completed?.Invoke(null);
                yield break;
            }
    
            var textAsset = handle.asset as TextAsset;
            if (textAsset == null)
            {
                Completed?.Invoke(null);
                yield break;
            }
            using MemoryStream stream = new MemoryStream(textAsset.bytes);
            BinaryFormatter formatter = new BinaryFormatter();
            string json = (string) formatter.Deserialize(stream);
            var data = JsonConvert.DeserializeObject<T>(json);
            stream.Close();
            Completed?.Invoke(data);
        }
    
        public override void Release()
        {
            Resources.UnloadAsset(_handle.asset);
        }
    
        private IEnumerator OnLoadCompleted(ResourceRequest obj)
        {
            yield return obj;
            if (obj.asset == null)
            {
                Completed.Invoke(null);
                yield break;
            }
#if SCRIPTABLE_OBJECT_CONFIG
            var loaded = obj.asset as ConfBaseData;
            Completed.Invoke(loaded);
#endif
        }
    }
}