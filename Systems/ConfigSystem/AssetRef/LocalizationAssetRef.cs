using System;

namespace PowerCellStudio
{
    [Serializable]
    public class LocalizationAssetRef : LocalizationRef<string>
    {
        public override string Get()
        {
            if (LocalizationManager.instance.TryGetAssetGuid(localizationKey, out string path))
            {
                return path;
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

        public static LocalizationAssetRef Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return new LocalizationAssetRef()
            {
                rawString = stringValue,
                localizationKey = $"{confName}_{rowIndex}_{colIndex}"
            };
        }
    }
}