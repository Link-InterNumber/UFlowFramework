using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    public class ConfigTypeInfo
    {
        public List<int> columns = new List<int>();
        public string fieldName;
        public string comment;
        public string typeName;
        public string refTypeName;
        public bool IsList => columns.Count > 1;
        public bool isKey;
    }
}