using System;
using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    public interface IConfigReader : IDisposable
    {
        public Dictionary<string, ConfigTypeInfo> fieldMap { get; }
        
        public string fileName { get; }
        
        public string path { get; }

        public List<string> GetEnumList(int keyColumn);
        
        public void StartReadLine(int startLine);
        
        public List<string> GetNextLine();
    }
}