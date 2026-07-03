using System;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class DataPersistenceSettingItem : IEditorSettingWindowItem
    {
        private string _searchKey;
        private PersistenceDataProcessor[] _persistenceDataProcessors;
        private CaptureDataProcessor _captureProcessor;
        private Dictionary<string, string> _dataDictionary;
        private string selectedDataKey;
        private string readedDataString;
        public string itemName => "Persistence Data";
        private bool _isEncrypt = true;
        private List<string> _configurationWarnings;

        private Sprite _loadedSprite;
        public void InitSave()
        {
            _dataDictionary = new Dictionary<string, string>();
            _captureProcessor = new CaptureDataProcessor();
            _configurationWarnings = PersistenceVersionRouter.GetConfigurationWarnings();
            var enumValues = Enum.GetValues(typeof(PlayerDataType));
            _persistenceDataProcessors = new PersistenceDataProcessor[enumValues.Length];

            var assembly = typeof(PersistenceDataProcessor).Assembly;
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
                            LinkLogger.Log($"[PlayerDataUtils] Registered {type.Name} for {attribute.DataType}");
#endif
                        }
                    }
                    catch (Exception e)
                    {
                        LinkLogger.LogError($"[PlayerDataUtils] Failed to instantiate processor for {attribute.DataType}: {e.Message}");
                    }
                }
            }
#if UNITY_EDITOR
            for (int i = 0; i < _persistenceDataProcessors.Length; i++)
            {
                if (_persistenceDataProcessors[i] == null && Enum.IsDefined(typeof(PlayerDataType), i))
                {
                    LinkLogger.LogWarning($"[PlayerDataUtils] No processor registered for PlayerDataType: {(PlayerDataType)i}");
                }
            }
#endif
        }

        public void OnDestroy()
        {
            _searchKey = null;
            _dataDictionary = null;
            _persistenceDataProcessors = null;
            _captureProcessor = null;
            _configurationWarnings = null;
            selectedDataKey = null;
            readedDataString = null;
            _loadedSprite = null;
        }

        public void OnGUI(EditorWindow window)
        {
            if (_configurationWarnings != null && _configurationWarnings.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", _configurationWarnings), MessageType.Warning);
                GUILayout.Space(8);
            }

            if (GUILayout.Button("Clear Player Prefs"))
            {
                _persistenceDataProcessors[(int)PlayerDataType.PlayerPrefs].ClearAll();
            }
            if (GUILayout.Button("Clear Json"))
            {
                _persistenceDataProcessors[(int)PlayerDataType.Json].ClearAll();
            }
            if (GUILayout.Button("Clear Binary"))
            {
                _persistenceDataProcessors[(int)PlayerDataType.Binary].ClearAll();
            }
            if (GUILayout.Button("Delete All Capture"))
            {
                _captureProcessor.ClearAll();
            }
            if (GUILayout.Button("Delete All"))
            {
                _persistenceDataProcessors[(int)PlayerDataType.PlayerPrefs].ClearAll();
                _persistenceDataProcessors[(int)PlayerDataType.Json].ClearAll();
                _persistenceDataProcessors[(int)PlayerDataType.Binary].ClearAll();
                _captureProcessor.ClearAll();
            }
            // 读取
            _searchKey = EditorGUILayout.TextField("Data Save Key:", _searchKey);
            _isEncrypt = EditorGUILayout.Toggle("Decrypt Data", _isEncrypt);
            EditorGUILayout.LabelField("If no saved key is specified, the key is the name of type of the data class");
            if (!string.IsNullOrEmpty(_searchKey) && GUILayout.Button("Read"))
            {
                selectedDataKey = _searchKey;
                ReadData();
                _loadedSprite = null;
            }

            if (!string.IsNullOrEmpty(_searchKey) && GUILayout.Button("Load Capture"))
            {
                _loadedSprite = _captureProcessor.Read(_searchKey, _isEncrypt);
                _dataDictionary.Clear();
                return;
            }

            if (_dataDictionary != null && _dataDictionary.Count > 0)
            {
                // Start of scroll view
                // scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
                GUILayout.Label("Saved Data:");
                foreach (var entry in _dataDictionary)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{entry.Key}: ", GUILayout.Width(80));
                    GUILayout.Label(entry.Value);

                    if (GUILayout.Button("Delete", GUILayout.Width(50)))
                    {
                        if (entry.Key == "Json")
                        {
                            PlayerDataUtils.Clear(entry.Key, PlayerDataType.Json);
                        }
                        else if (entry.Key == "Binary")
                        {
                            PlayerDataUtils.Clear(entry.Key, PlayerDataType.Binary);
                        }
                        else if (entry.Key == "PlayerPrefs")
                        {
                            PlayerDataUtils.Clear(entry.Key, PlayerDataType.PlayerPrefs);
                        }
                        _dataDictionary.Remove(entry.Key);
                        GUILayout.EndHorizontal();
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            
            if (_loadedSprite != null)
            {
                GUILayout.Label("Loaded Capture:");
                GUILayout.Label(_loadedSprite.texture, GUILayout.Width(400), GUILayout.Height(400 * (_loadedSprite.texture.height / (float)_loadedSprite.texture.width)));
                
            }
            GUILayout.Space(10);
        }
        
        private void ReadData()
        {
            _dataDictionary.Clear();
            // Read Json Data
            var jsonProcessor = _persistenceDataProcessors[(int)PlayerDataType.Json];
            if (jsonProcessor.TryGetSaveFilePath(selectedDataKey, out var path))
            {
                var json = ReadJson(path);
                if (!string.IsNullOrEmpty(json)) _dataDictionary.Add("Json", json);
            }
            // Read Binary Data
            var binaryProcessor = _persistenceDataProcessors[(int)PlayerDataType.Binary];
            if (binaryProcessor.TryGetSaveFilePath(selectedDataKey, out var binPath))
            {
                var binData = ReadBinary(binPath);
                if (!string.IsNullOrEmpty(binData)) _dataDictionary.Add("Binary", binData);
            }
            // Read Player Prefs Data
            var playerPrefsProcessor = _persistenceDataProcessors[(int)PlayerDataType.PlayerPrefs];
            if (playerPrefsProcessor.TryGetSaveFilePath(selectedDataKey, out var prefPath))
            {
                var prefData = ReadPlayerPrefs(selectedDataKey);
                if (!string.IsNullOrEmpty(prefData)) _dataDictionary.Add("PlayerPrefs", prefData);
            }
        }

        private string ReadJson(string path, bool decrypt = true)
        {
            if (!File.Exists(path)) return null;
            var jsonEn = File.ReadAllText(path);
            if (_isEncrypt)
            {
                var json = EncryptUtils.Base64Decrypt(jsonEn);
                var parsedJson = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            else
            {
                var parsedJson = JsonConvert.DeserializeObject(jsonEn);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
        }

        private string ReadBinary(string binPath, bool decrypt = true)
        {
            if (!File.Exists(binPath)) return null;
            byte[] encryptedData = File.ReadAllBytes(binPath);
            var decryptedData = _isEncrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
            using MemoryStream memoryStream = new MemoryStream(decryptedData);
            // 使用BinaryFormatter进行反序列化
            BinaryFormatter formatter = new BinaryFormatter();
            var data = formatter.Deserialize(memoryStream);
            // 关闭文件流
            memoryStream.Close();

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            return json;
        }

        private string ReadPlayerPrefs(string prefKey, bool decrypt = true)
        {
            if (!PlayerPrefs.HasKey(prefKey)) return null;
            var prefValue = PlayerPrefs.GetString(prefKey, null);
            if (!string.IsNullOrEmpty(prefValue))
            {
                if (_isEncrypt)
                {
                    var decryptedPref = EncryptUtils.Base64Decrypt(prefValue);
                    var parsedJson = JsonConvert.DeserializeObject(decryptedPref);
                    return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                }
                else
                {
                    var parsedJson = JsonConvert.DeserializeObject(prefValue);
                    return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                }
            }
            else if (PlayerPrefs.GetInt(prefKey, int.MinValue) != int.MinValue)
            {
                return PlayerPrefs.GetInt(prefKey).ToString();
            }
            else if (PlayerPrefs.GetFloat(prefKey, float.MinValue) != float.MinValue)
            {
                return PlayerPrefs.GetFloat(prefKey).ToString();
            }
            return null;
        }

        public void SaveData(){}
    }
}