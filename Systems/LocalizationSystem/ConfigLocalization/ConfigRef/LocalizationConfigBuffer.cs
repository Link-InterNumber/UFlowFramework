using System.Collections.Generic;

namespace PowerCellStudio
{
    public class LocalizationConfigBuffer
    {
        private static List<LocalizationStringRef> _localizationStringRefs;

        private static List<LocalizationAssetRef> _localizationAssetRefs;

        public static void PrepareBuffer()
        {
            _localizationStringRefs = new List<LocalizationStringRef>();
            _localizationAssetRefs = new List<LocalizationAssetRef>();
        }
        
        public static void DisposeBuffer()
        {
            _localizationStringRefs.Clear();
            _localizationAssetRefs.Clear();
            _localizationStringRefs = null;
            _localizationAssetRefs = null;
        }

        public static void ClearBuffer()
        {
            _localizationStringRefs.Clear();
            _localizationAssetRefs.Clear();
        }

        public static void AddStringRef(LocalizationStringRef stringRef)
        {
            _localizationStringRefs.Add(stringRef);
        }
        
        public static void AddAssetRef(LocalizationAssetRef assetRef)
        {
            _localizationAssetRefs.Add(assetRef);
        }
        
        public static bool hasBuffer => (_localizationStringRefs != null && _localizationStringRefs.Count > 0) 
                                        || (_localizationAssetRefs != null && _localizationAssetRefs.Count > 0);
        
        public static List<LocalizationStringRef> GetStringRefs()
        {
            return _localizationStringRefs;
        }
        
        public static List<LocalizationAssetRef> GetAssetRefs()
        {
            return _localizationAssetRefs;
        }
    }
}