using System;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;

namespace PowerCellStudio
{
    public class DataPersistenceSettingItem : IEditorSettingWindowItem
    {
        private string _searchKey;

        private Dictionary<string, string> dataDictionary;
        private string selectedDataKey;
        private string readedDataString;

        public string itemName => "Persistence Data Inspector";

        public void InitSave()
        {
            dataDictionary = new Dictionary<string, string>();

            var typeStrJson = "Json";
            string folderPath = Path.Combine(PlayerDataUtils.SavePath, typeStrJson);
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    dataDictionary.Add(fileNameWithoutExtension, typeStrJson);
                }
            }
            
            var typeStrBinary = "Binary";
            folderPath = Path.Combine(PlayerDataUtils.SavePath, typeStrBinary);
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    dataDictionary.Add(fileNameWithoutExtension, typeStrBinary);
                }
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
            if (GUILayout.Button("Read") && dataDictionary.TryGetValue(_searchKey, out var typeName))
            {
                selectedDataKey = _searchKey;
                if (typeName == "Json")
                {
                    readedDataString = ReadJson(_searchKey);
                }
                else if (typeName == "Binary")
                {
                    readedDataString = ReadBinary(_searchKey);
                }
            }

            if (dataDictionary.Count > 0)
            {
                // Start of scroll view
                // scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
                foreach (var entry in dataDictionary)
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(entry.Key, GUILayout.Width(200));
                    GUILayout.Label(entry.Value, GUILayout.Width(50));

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
                        GUILayout.EndHorizontal();
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
            
                // End of scroll view
                // GUILayout.EndScrollView();
            }
            GUILayout.Space(10);

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
                var json = EncryptUtils.Base64Decrypt(jsonEn);
                dynamic parsedJson = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            else
            {
                dynamic parsedJson = JsonConvert.DeserializeObject(jsonEn);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
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
            
            StringBuilder result = new StringBuilder();
            result.Append("{\n");

            var fields = data.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                // Optionally check for NonSerialized attribute
                if (Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                    continue;

                var fieldValue = field.GetValue(data);
                result.Append($"\t{field.Name} = ");

                if (fieldValue is IList list)
                {
                    result.Append("[");
                    for (int i = 0; i < list.Count; i++)
                    {
                        result.Append(list[i]);
                        if (i < list.Count - 1)
                        {
                            result.Append(", \n");
                        }
                    }
                    result.Append("], \n");
                }
                else if (fieldValue is IDictionary dictionary)
                {
                    result.Append("{");
                    bool first = true;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        if (!first)
                        {
                            result.Append(", ");
                        }
                        result.Append($"[{entry.Key}: {entry.Value}]\n");
                        first = false;
                    }
                    result.Append("}, \n");
                }
                else
                {
                    // Handle other field types here
                    result.Append($"{fieldValue}, \n");
                }
            }
            result.Append("}");
            
            return result.ToString();
        }

        public void SaveData(){}
    }
}