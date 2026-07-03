using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace PowerCellStudio
{
    public class PlayerPrefsDataTranslator : DataTranslatorBase
    {
        protected override string saveKey => GetType().Name;

        public override int version => 0;

        private PlayerPrefsProcessor _binaryDataProcessor = new PlayerPrefsProcessor();
        protected override PersistenceDataProcessor targetProcessor => _binaryDataProcessor;

        protected override object ReadOldData(string filePath, bool decrypt)
        {
            try
            {
                var key = Path.GetFileNameWithoutExtension(filePath);
                string json = PlayerPrefs.GetString(key, "{}");
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to read binary data: {e.Message}");
                return null;
            }
        }
    }
}