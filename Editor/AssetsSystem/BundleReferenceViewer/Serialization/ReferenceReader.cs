using System;
using System.Collections.Generic;
using System.IO;

namespace PowerCellStudio.Editor
{
    public class ReferenceReader : IDisposable
    {
        private BinaryReader _reader;
        private MemoryStream _memoryStream;
        
        public ReferenceReader()
        {
            _memoryStream = new MemoryStream();
            _reader = new BinaryReader(_memoryStream);
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _memoryStream?.Dispose();
        }

        public IEnumerable<T> Read<T>(string filePath) where T : IBundleReferenceBinary, new()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");
            var bytes = File.ReadAllBytes(filePath);
            _memoryStream.Write(bytes, 0, bytes.Length);
            _memoryStream.Position = 0;
            var count = _reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var reference = new T();
                reference.ReadBytes(_reader);
                yield return reference;
            }
        }
    }
}