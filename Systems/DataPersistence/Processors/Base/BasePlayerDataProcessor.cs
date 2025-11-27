using System.IO;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class BasePlayerDataProcessor : IPlayerDataProcessor
    {
        public static readonly string SavePathRoot = $"{Application.persistentDataPath}";

        public abstract string directoryName { get; }

        public abstract string extension { get; }

        public bool TryGetSaveFilePath(string saveKey, out string savePath)
        {
            if (string.IsNullOrEmpty(saveKey))
            {
                savePath = string.Empty;
                return false;
            }
            savePath = Path.Combine(SavePathRoot, directoryName, $"{saveKey}.{extension}");
            return true;
        }

        protected void CheckDirectory()
        {
            var directory = Path.Combine(SavePathRoot, directoryName);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }

        public virtual bool HasSave(string saveKey)
        {
            var directory = Path.Combine(SavePathRoot, directoryName);
            if (!Directory.Exists(directory)) return false;
            if (!TryGetSaveFilePath(saveKey, out var path)) return false;
            return File.Exists(path);
        }

        public virtual void Clear(string saveKey)
        {
            if (!TryGetSaveFilePath(saveKey, out var path)) return;
            if (!File.Exists(path)) return;
            File.Delete(path);
        }

        public virtual void ClearAll()
        {
            var path = Path.Combine(SavePathRoot, directoryName);
            if (!Directory.Exists(path)) return;
            DirectoryInfo di = new DirectoryInfo(path);
            di.Delete(true);
        }
    }
}