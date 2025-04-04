using System;

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

        public static LocalizationStringRef Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            return new LocalizationStringRef()
            {
                rawString = stringValue,
                localizationKey = $"{confName}_{rowIndex}_{colIndex}"
            };
        }
    }
}