#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class EditorConfigGroup: IDisposable
    {
        private Dictionary<Type, IConfBaseCollections> _configs;

        public void Append<T>() where T: class, IConfBaseCollections, new()
        {
            _configs.Add(typeof(T), new T());
        }

        public void LoadConfig()
        {
            foreach (var keyValue in _configs)
            {
                keyValue.Value.Prepare();
            }
        }

        public T GetConfig<T>() where T : class, IConfBaseCollections, new()
        {
            if (_configs.TryGetValue(typeof(T), out var config))
            {
                return config as T;
            }
            return null;
        }

        public void Dispose()
        {
            foreach(var keyValue in _configs)
            {
                keyValue.Value.Release(true);
            }
            _configs.Clear();
        }
    }

//     public class EditorConfigLoadHandle : ConfAsyncLoadHandle
//     {
//         public override void LoadScriptableObject(string path)
//         {
// #if SCRIPTABLE_OBJECT_CONFIG
//             var asset = AssetDatabase.LoadAssetAtPath<ConfBaseData>(path);
//             Completed?.Invoke(asset);
// #endif
//         }
//
//         public override void LoadJson<T>(string path)
//         {
//             var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
//             var jsonString = EncryptUtils.AESDecrypt(asset.text, ConstSetting.FileEncryptionKey); // 解密配置文件
//             var data = JsonConvert.DeserializeObject<T>(jsonString);
//             Completed?.Invoke(data);
//         }
//
//         public override void LoadBinary<T>(string path)
//         {
//             var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
//             var bytes = EncryptUtils.AESDecrypt(asset.bytes, ConstSetting.FileEncryptionKey); // 解密配置文件
//             using MemoryStream stream = new MemoryStream(bytes);
//             BinaryFormatter formatter = new BinaryFormatter();
//             T data = (T) formatter.Deserialize(stream);
//             stream.Close();
//             Completed?.Invoke(data);
//         }
//
//         public override void Release()
//         {
//
//         }
//     }
}

#endif