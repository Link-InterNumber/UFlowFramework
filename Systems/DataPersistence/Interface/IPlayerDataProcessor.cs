using System;

namespace PowerCellStudio
{
    public interface IPlayerDataProcessor
    {
        public string directoryName { get; }
        public string extension { get; }
        public bool HasSave(string saveKey);
        public void Clear(string saveKey);
        public void ClearAll();
    }

    public interface IPlayerDataSaver
    {
        public bool Save<T>(string saveKey, T data, bool encrypt);
        public void SaveAsync<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt);
    }

    public interface IPlayerDataSaver<T>
    {
        public bool Save(string saveKey, T data, bool encrypt);
        public void SaveAsync(string saveKey, T data, Action<bool> onComplete, bool encrypt);
    }

    public interface IPlayerDataReader
    {
        public T Read<T>(string saveKey, bool decrypt);
        public void ReadAsync<T>(string saveKey, Action<T> onComplete, bool decrypt);
    }

    public interface IPlayerDataReader<T>
    {
        public T Read(string saveKey, bool decrypt);
        public void ReadAsync(string saveKey, Action<T> onComplete, bool decrypt);
    }
}