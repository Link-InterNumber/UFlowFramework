using System;

namespace PowerCellStudio
{
    public abstract class PersistenceDataProcessor : BasePlayerDataProcessor, IPlayerDataSaver, IPlayerDataReader
    {
        public abstract bool Save<T>(string saveKey, T data, bool encrypt);

        public abstract void SaveAsync<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt);

        public abstract T Read<T>(string saveKey, bool decrypt);

        public abstract void ReadAsync<T>(string saveKey, Action<T> onComplete, bool decrypt);
    }
}