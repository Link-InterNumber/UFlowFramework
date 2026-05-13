using System;
using System.IO;
using System.Text;


#if UNITY_EDITOR
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
#endif

namespace PowerCellStudio
{
    [Serializable]
    public class LocalizationStringRef : LocalizationRef<string>
    {
        public override string Get()
        {
            if (LocalizationManager.instance.TryGetString(localizationKey, out string localizationString))
            {
                return localizationString;
            }
#if UNITY_EDITOR
            // editor中可以测试出key没有正确匹配
            return localizationKey;
#endif
            //release环境中至少获得默认语言
            return rawString; 
        }

        public override string ToString()
        {
            return Get();
        }

        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("locstring") || lowerRawType.Equals("LocString");
        }

        public override string TypeName()
        {
            return "LocalizationStringRef";
        }
        
#if UNITY_EDITOR
        private static StringTable stringTable;
        public static void ClearCache()
        {
            stringTable = null;
        }
#endif
        public static LocalizationStringRef Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            var result = new LocalizationStringRef()
            {
                rawString = stringValue,
                localizationKey = $"{confName}_{rowIndex}_{colIndex}"
            };
#if UNITY_EDITOR
            if (stringTable == null)
            {
                stringTable = LocalizationSettings.StringDatabase.GetTable(ConstSetting.LocalizationStringTable);
            }
            var entry = stringTable?.AddEntry(result.localizationKey, result.rawString);
            stringTable?.SharedData.AddKey(entry.Key, entry.KeyId);
            UnityEditor.EditorUtility.SetDirty(stringTable);
#endif
            return result;
        }

        public static void WriteItemData(LocalizationStringRef item, BinaryWriter writer, Encoding encoding)
        {
            item.rawString.WriteString(writer, encoding);
            item.localizationKey.WriteString(writer, encoding);
        }

        public static LocalizationStringRef ReadItemData(BinaryReader reader, Encoding encoding)
        {
            var rawString = StringExtension.ReadString(reader, encoding);
            var localizationKey = StringExtension.ReadString(reader, encoding);
            return new LocalizationStringRef() { rawString = rawString, localizationKey = localizationKey };
        }

        public override void WriteData(BinaryWriter writer, Encoding encoding)
        {
            rawString.WriteString(writer, encoding);
            localizationKey.WriteString(writer, encoding);
        }

        public override void ReadData(BinaryReader reader, Encoding encoding)
        {
            rawString = StringExtension.ReadString(reader, encoding);
            localizationKey = StringExtension.ReadString(reader, encoding);
        }
    }
}