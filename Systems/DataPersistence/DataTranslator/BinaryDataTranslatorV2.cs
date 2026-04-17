using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;

namespace PowerCellStudio
{
    public class BinaryDataTranslatorV2 : DataTranslatorBase
    {
        protected override string saveKey => GetType().Name;

        public override int version => 1;

        private BinaryDataProcessor _binaryDataProcessor = new BinaryDataProcessor();
        protected override PersistenceDataProcessor targetProcessor => _binaryDataProcessor;

        protected override object ReadOldData(string filePath, bool decrypt)
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                var bytes = decrypt ? EncryptUtils.AESDecrypt(encryptedData, ConstSetting.FileEncryptionKey) : encryptedData;
                byte[] result = null;
                using (var compressedStream = new MemoryStream(bytes))
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    gzipStream.CopyTo(resultStream);
                    result = resultStream.ToArray();
                }

                var json = Encoding.UTF8.GetString(result);
                var data = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    // TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
                return data;
            }
            catch (Exception e)
            {
                LinkLog.LogError($"Failed to read binary data: {e.Message}");
                return null;
            }
        }
    }
}