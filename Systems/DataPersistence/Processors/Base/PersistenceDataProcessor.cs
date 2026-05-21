using System;

namespace PowerCellStudio
{
    public abstract class PersistenceDataProcessor : BasePlayerDataProcessor, IPlayerDataSaver, IPlayerDataReader
    {
        protected static int GetCurrentVersion<T>()
        {
            return PersistenceVersionRouter.GetCurrentVersion(typeof(T));
        }

        protected static string SerializeStringPayload<T>(PlayerDataType dataType, T data)
        {
            return PersistenceVersionRouter.SerializeString(dataType, data);
        }

        protected static byte[] SerializeBinaryPayload<T>(PlayerDataType dataType, T data)
        {
            return PersistenceVersionRouter.SerializeBinary(dataType, data);
        }

        protected static T DeserializeStringPayload<T>(PlayerDataType dataType, int version, string payload)
        {
            return PersistenceVersionRouter.DeserializeString<T>(dataType, version, payload);
        }

        protected static T DeserializeBinaryPayload<T>(PlayerDataType dataType, int version, byte[] payload)
        {
            return PersistenceVersionRouter.DeserializeBinary<T>(dataType, version, payload);
        }

        protected static bool TryUpgradeData<T>(int sourceVersion, T sourceData, out T result, out bool upgraded)
        {
            return PersistenceVersionRouter.TryUpgrade(sourceVersion, sourceData, out result, out upgraded);
        }

        public abstract bool Save<T>(string saveKey, T data, bool encrypt);

        public abstract void SaveAsync<T>(string saveKey, T data, Action<bool> onComplete, bool encrypt);

        public abstract T Read<T>(string saveKey, bool decrypt);

        public abstract void ReadAsync<T>(string saveKey, Action<T> onComplete, bool decrypt);
    }
}