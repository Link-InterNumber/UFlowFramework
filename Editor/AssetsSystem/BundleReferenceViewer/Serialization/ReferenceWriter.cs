using System;
using System.Collections.Generic;
using System.IO;

namespace PowerCellStudio.Editor
{
    public class ReferenceWriter : IDisposable
    {
        private BinaryWriter _writer;
        private MemoryStream _memoryStream;
        
        public ReferenceWriter()
        {
            _memoryStream = new MemoryStream();
            _writer = new BinaryWriter(_memoryStream);
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _memoryStream?.Dispose();
        }
        
        public void Write<T>(IList<T> references) where T : IBundleReferenceBinary
        {
            _writer.Write(references.Count);
            foreach (var reference in references)
            {
                reference.WriteBytes(_writer);
            }
        }

        public void Flush(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            using var dataFile = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            dataFile.Write(_memoryStream.ToArray());
            dataFile.Flush();
        }
    }
}