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
    public class LocalizationAssetRef : LocalizationRef<string>
    {
        public override string Get()
        {
            if (LocalizationManager.instance.TryGetAssetGuid(localizationKey, out string guid))
            {
                return guid;
            }
#if UNITY_EDITOR
            return localizationKey;
#endif
            return rawString;
        }

        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("locasset") || lowerRawType.Equals("LocAsset");
        }

        public override string TypeName()
        {
            return "LocalizationAssetRef";
        }

#if UNITY_EDITOR
        private static AssetTable assetTable;
        public static void ClearCache()
        {
            assetTable = null;
        }
#endif
        
        public static LocalizationAssetRef Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            var result = new LocalizationAssetRef()
            {
                rawString = stringValue,
                localizationKey = $"{confName}_{rowIndex}_{colIndex}"
            };
#if UNITY_EDITOR
            if (assetTable == null)
            {
                assetTable = LocalizationSettings.AssetDatabase.GetTable(ConstSetting.LocalizationAssetTable);
            }
            var entry = assetTable?.AddEntry(result.localizationKey, UnityEditor.AssetDatabase.AssetPathToGUID(result.rawString));
            assetTable?.SharedData.AddKey(entry.Key, entry.KeyId);
            UnityEditor.EditorUtility.SetDirty(assetTable);
#endif
            return result;
        }

        public static void WriteItemData(LocalizationAssetRef item, BinaryWriter writer, Encoding encoding)
        {
            item.rawString.WriteString(writer, encoding);
            item.localizationKey.WriteString(writer, encoding);
        }

        public static LocalizationAssetRef ReadItemData(BinaryReader reader, Encoding encoding)
        {
            var rawString = StringExtension.ReadString(reader, encoding);
            var localizationKey = StringExtension.ReadString(reader, encoding);
            return new LocalizationAssetRef() { rawString = rawString, localizationKey = localizationKey };
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