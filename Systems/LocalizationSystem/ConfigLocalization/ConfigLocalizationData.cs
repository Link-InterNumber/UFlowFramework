using System;

namespace PowerCellStudio
{
    [Serializable]
    public struct ConfigLocalizationData
    {
        public string key;
        public string value;

        public static string GetKey(ConfigLocalizationData arg)
        {
            return arg.key;
        }
    }
}