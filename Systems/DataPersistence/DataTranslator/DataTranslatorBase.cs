using System.IO;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IDataTranslator
    {
        public int version { get; }
        public void TryTranslator();
    }

    public abstract class DataTranslatorBase : IDataTranslator
    {
        public DataTranslatorBase(){}

        protected abstract string saveKey { get; }

        public abstract int version {get;}

        protected abstract PersistenceDataProcessor targetProcessor {get;}

        protected abstract object ReadOldData(string filePath, bool decrypt);

        public virtual void TryTranslator()
        {
            if (PlayerPrefs.GetInt(saveKey, 0) != 0) return;
            var processor = targetProcessor;
            var saveRoot = Path.Combine(BasePlayerDataProcessor.SavePathRoot, processor.directoryName);
            if (!Directory.Exists(saveRoot))
            {
                PlayerPrefs.SetInt(saveKey, 1);
                return;
            }
            var byteFiles = Directory.GetFiles(saveRoot, $"*.{processor.extension}", SearchOption.TopDirectoryOnly);
            if (byteFiles.Length == 0)
            {
                PlayerPrefs.SetInt(saveKey, 1);
                return;
            }
            foreach (var filePath in byteFiles)
            {
                var data = ReadOldData(filePath, true);
                if (data != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    processor.Save(fileName, data, true);
                }
            }
            PlayerPrefs.SetInt(saveKey, 1);
        }
   }
}