using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PowerCellStudio
{
    public class BinaryDataTranslator : DataTranslatorBase
    {
        protected override string saveKey => GetType().Name;

        public override int version => 0;

        private BinaryDataProcessor _binaryDataProcessor = new BinaryDataProcessor();
        protected override PersistenceDataProcessor targetProcessor => _binaryDataProcessor;

        protected override object ReadOldData(string filePath, bool decrypt)
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                var decryptedData = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
                using MemoryStream memoryStream = new MemoryStream(decryptedData);
                // 使用BinaryFormatter进行反序列化
                BinaryFormatter formatter = new BinaryFormatter();
                var data = formatter.Deserialize(memoryStream);
                return data;
            }
            catch (Exception e)
            {
                LinkLogger.LogError($"Failed to read binary data: {e.Message}");
                return null;
            }
        }
    }
}