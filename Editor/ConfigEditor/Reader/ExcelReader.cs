#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class ExcelReader: IConfigReader
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
        
        private readonly string _path;
        public string path => _path;
        
        public ExcelReader(string path)
        {
            _fileName = Path.GetFileNameWithoutExtension(path).Split('_')[0].Trim() + "Conf";
            _path = path;
            LoadExcel(path);
            GetFieldMap();
        }
        
        private void LoadExcel(string path)
        {
            if (!File.Exists(path))
            {
                ConfigLogger.LogError("Cannot find file " + path);
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
            var typeResolvers = ResolversTypeBuffer.buffer;
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
                foreach (var typeResolver in typeResolvers)
                {
                    if (typeResolver.isMatch(fieldTypeTemp.ToLower()))
                    {
                        fieldType = typeResolver.TypeName();
                        refTypeName = typeResolver.GetType().Name;
                        break;
                    }
                }

                var split = fieldNameTemp.Split(':');
                var fieldName = char.ToLower(split[0][0]) + split[0].Substring(1);
                var isKey = split.Length > 1 && split[1].ToLower() == "key";
                if (_fieldMap.TryGetValue(fieldName, out var typeInfo))
                {
                    if(typeInfo.isKey)
                    {
                        ConfigLogger.LogError($"[{_workbook.Names}]配置的Key {fieldComment} 字段不能重复");
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

        public List<string> GetEnumList(int keyColumn)
        {
            var enumValues = HashSetPool<string>.Get();
            var list = new List<string>();
            
            var ws = workbook.Worksheets[1];
            var rowCount = ws.Dimension.Rows;
            for (int raw = 4; raw <= rowCount; raw++)
            {
                var keyCell = ws.Cells[raw, keyColumn].Value;
                if (keyCell == null || string.IsNullOrEmpty(keyCell.ToString())) continue;
                var valueString  = keyCell.ToString();
                if (enumValues.Contains(valueString))
                {
                    ConfigLogger.LogError($"配置的Key {valueString} 字段不能重复");
                    continue;
                }
                enumValues.Add(valueString);
                list.Add(valueString);
            }
            HashSetPool<string>.Release(enumValues);
            return list;
        }

        private int _currentRow;
        private List<string> _buffer;
        public void StartReadLine(int startLine)
        {
            startLine = Math.Max(startLine, 0);
            startLine++;
            _currentRow = startLine;
            _buffer = new List<string>();
        }

        public List<string> GetNextLine()
        {
            var ws = workbook.Worksheets[1];
            var rowCount = ws.Dimension.Rows;
            var columnCount = ws.Dimension.Columns;
            if (_currentRow < 1 || _currentRow > rowCount)
                return null;
            _buffer.Clear();
            for (int column = 1; column <= columnCount; column++)
            {
                var cell = ws.Cells[_currentRow, column].Value;
                var stringValue = cell?.ToString() ?? string.Empty;
                _buffer.Add(stringValue);
            }
            _currentRow++;
            return _buffer;
        }

        public void Dispose()
        {
            _buffer?.Clear();
            _buffer = null;
            _workbook.Dispose();
            _ep.Dispose();
            _fieldMap.Clear();
            _fieldMap = null;
        }
    }
}

#endif