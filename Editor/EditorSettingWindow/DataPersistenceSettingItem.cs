using UnityEditor;
using System;
using System.IO;

namespace PowerCellStudio
{
    public class DataPersistenceSettingItem : IEditorSettingWindowItem
    {
        private string _searchKey;

        private Vector2 scrollPosition;
        private Dictionary<string, string> dataDictionary
        private string selectedDataKey;
        private string readedDataString;

        public string itemName => "开发中本地数据管理"

        public void InitSave()
        {
            scrollPosition = Vector2.Zero;
            dataDictionary = new Dictionary<string, string>();

            var typeStrJson = "Json";
            string folderPath = Path.Combine(PlayerDataUtils.SavePath, typeStrJson);
            string[] files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                dataDictionary.Add(fileNameWithoutExtension, typeStrJson);
            }

            var typeStrBinary = "Binary";
            folderPath = Path.Combine(PlayerDataUtils.SavePath, typeStrBinary);
            files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                dataDictionary.Add(fileNameWithoutExtension, typeStrBinary);
            }
        }

        public void OnDestroy()
        {
            _searchKey = null;
            dataDictionary.Clear();
            dataDictionary = null;
            selectedDataKey = null;
            readedDataString = null;
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Clear Player Prefs"))
            {
                PlayerDataUtils.ClearAllPlayerPrefs();
            }
            if (GUILayout.Button("Clear Json"))
            {
                PlayerDataUtils.ClearAllJson();
            }
            if (GUILayout.Button("Clear Binary"))
            {
                PlayerDataUtils.ClearAllBinary();
            }
            if (GUILayout.Button("Delete All Capture"))
            {
                PlayerDataUtils.ClearCapture();
            }
            if (GUILayout.Button("Delete All"))
            {
                PlayerDataUtils.ClearAll();
            }
            // 读取
            _searchKey = EditorGUILayout.TextField("Data Save Key:", _searchKey);
            EditorGUILayout.LabelField("If no saved key is specified, the key is the name of type of the data class");
            if (GUILayout.Button("Read") && dataDictionary.ContainsKey(_searchKey))
            {
                selectedDataKey = _searchKey;
                if (entry.Value == "Json")
                {
                    readedDataString = ReadJson(entry.Key);
                }
                else if (entry.Value == "Binary")
                {
                    readedDataString = ReadBinary(entry.Key);
                }
            }

            // Start of scroll view
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height - 100));
            
            foreach (var entry in dataDictionary)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(entry.Key, GUILayout.Width(150));
                GUILayout.Label(entry.Value.type, GUILayout.Width(50));

                if (GUILayout.Button("Show", GUILayout.Width(50)))
                {
                    selectedDataKey = entry.Key;
                    if (entry.Value == "Json")
                    {
                        readedDataString = ReadJson(entry.Key);
                    }
                    else if (entry.Value == "Binary")
                    {
                        readedDataString = ReadBinary(entry.Key);
                    }
                }

                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    if (entry.Value == "Json")
                    {
                        PlayerDataUtils.ClearJson(entry.Key);
                    }
                    else if (entry.Value == "Binary")
                    {
                        PlayerDataUtils.ClearBinary(entry.Key);
                    }
                    dataDictionary.Remove(entry.Key);
                    break;
                }
                GUILayout.EndHorizontal();
            }
            
            // End of scroll view
            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(selectedDataKey) && dataDictionary.ContainsKey(selectedDataKey))
            {
                GUILayout.Label($"Selected Key: {selectedDataKey}\nValue:\n{readedDataString}");
            }
        }

        private string ReadJson(string fileName, bool decrypt = true)
        {
            if(string.IsNullOrEmpty(fileName)) return default;
            var path = Path.Combine(PlayerDataUtils.SavePath, "Json", $"{fileName}.json");
            if (!File.Exists(path)) return default;
            var jsonEn = File.ReadAllText(path);
            if (decrypt)
            {
                var json = Decrypt(jsonEn);
                return json;
            }
            return jsonEn;
        }

        private string ReadBinary(string fileName, bool decrypt = true)
        {
            if(string.IsNullOrEmpty(fileName)) return default;
            var filePath = Path.Combine(PlayerDataUtils.SavePath, "Binary", $"{fileName}.bytes");
            if (!File.Exists(filePath)) return default;
            byte[] encryptedData = File.ReadAllBytes(filePath);
            var decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
            using MemoryStream memoryStream = new MemoryStream(decryptedData);
            // 使用BinaryFormatter进行反序列化
            BinaryFormatter formatter = new BinaryFormatter();
            var data = formatter.Deserialize(memoryStream);
            // 关闭文件流
            memoryStream.Close();
            return data.ToString();
        }

        public void SaveData(){}
    }
}