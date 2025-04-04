#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;

namespace PowerCellStudio
{
    public class ExcelReader: IDisposable
    {
        private ExcelWorkbook _workbook;
        public ExcelWorkbook workbook => _workbook;
        private ExcelPackage _ep;
        private Dictionary<string, ConfigTypeInfo> _fieldMap;
        public Dictionary<string, ConfigTypeInfo> fieldMap => _fieldMap;
        // private ConfigTypeInfo[] _configTypeInfoList;
        private readonly string _fileName;
        public string fileName => _fileName;
        // private StringBuilder _csFile;
        
        private List<TypeRef> _typeResolvers = new List<TypeRef>();
        private readonly string _path;
        public string path => _path;

        public ExcelReader(string path)
        {
            _fileName = Path.GetFileNameWithoutExtension(path).Split('_')[0].Trim() + "Conf";
            _path = path;
            InitTypeResolvers();
            LoadExcel(path);
            GetFieldMap();
        }

        private void InitTypeResolvers()
        {
            if (_typeResolvers.Any()) return;
        
            var types = Assembly.GetAssembly(typeof(TypeRef)).GetTypes().Where(t => 
                !t.IsAbstract &&
                t.IsClass &&
                t.IsSubclassOf(typeof(TypeRef)));

            foreach (var type in types)
            {
                var resolver = (TypeRef)Activator.CreateInstance(type);
                _typeResolvers.Add(resolver);
            }
        }
        
        private void LoadExcel(string path)
        {
            if (!File.Exists(path))
            {
                ConfigLog.LogError("Cannot find file " + path);
                return;
            }

            var file = new FileInfo(path);
            _ep = new ExcelPackage(file);
            _workbook = _ep.Workbook;
        }

        private void GetFieldMap()
        {
            var sheet = _workbook.Worksheets[1];
            var columnCount = sheet.Dimension.Columns;

            _fieldMap = new Dictionary<string, ConfigTypeInfo>();
            for (var column = 1; column <= columnCount; column++)
            {
                if (sheet.Cells[2, column].Value == null) continue;
                var fieldComment = sheet.Cells[1, column].Value?.ToString().Split("\n")[0]??"";
                var fieldNameTemp = sheet.Cells[2, column].Value?.ToString();
                var fieldTypeTemp = sheet.Cells[3, column].Value?.ToString();
                if(string.IsNullOrEmpty(fieldTypeTemp) || string.IsNullOrEmpty(fieldNameTemp) ||
                   fieldNameTemp.StartsWith("#") || fieldTypeTemp.StartsWith("##"))
                {
                    continue;
                }

                var refTypeName = "StringRef";
                var fieldType = "string";
                foreach (var typeResolver in _typeResolvers)
                {
                    if (typeResolver.isMatch(fieldTypeTemp.ToLower()))
                    {
                        fieldType = typeResolver.TypeName();
                        refTypeName = typeResolver.GetType().Name;
                        break;
                    }
                }

                var split = fieldNameTemp.Split(':');
                var fieldName = split[0];
                var isKey = split.Length > 1 && split[1].ToLower() == "key";
                if (_fieldMap.TryGetValue(fieldName, out var typeInfo))
                {
                    if(typeInfo.isKey)
                    {
                        ConfigLog.LogError($"[{_workbook.Names}]配置的Key {fieldComment} 字段不能重复");
                        continue;
                    };
                    typeInfo.columns.Add(column);
                }
                else
                {
                    typeInfo = new ConfigTypeInfo()
                    {
                        columns = new List<int>(){column},
                        fieldName = fieldName,
                        comment = fieldComment,
                        typeName = fieldType,
                        refTypeName = refTypeName,
                        isKey = isKey
                    };
                    _fieldMap.Add(fieldName, typeInfo);
                }
            }
        }

        public void Dispose()
        {
            _workbook.Dispose();
            _ep.Dispose();
            _fieldMap.Clear();
            _fieldMap = null;
            _typeResolvers.Clear();
            _typeResolvers = null;
        }
    }
}

#endif