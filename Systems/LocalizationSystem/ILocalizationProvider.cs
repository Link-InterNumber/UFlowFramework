using System;
using System.Collections;

namespace PowerCellStudio
{
    public interface ILocalizationProvider
    {
        void Init(Language language);
        bool TryGetString(string key, out string result, params object[] param);
        LoaderYieldInstruction<T> GetAssetAsync<T>(string key) where T : UnityEngine.Object;
        void ReleaseAsset(string key);
        string GetAssetPath(string key);
        string GetAssetGuid(string key);
        IEnumerator ChangeLanguage(Language language);
    }
}