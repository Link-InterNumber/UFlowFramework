using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerCellStudio.Editor
{
    public class CsvReader : IConfigReader
    {
        private Dictionary<string, ConfigTypeInfo> _fieldMap;
        public Dictionary<string, ConfigTypeInfo> fieldMap => _fieldMap;
        
        private string _fileName;
        public string fileName => _fileName;
        
        private string _path;
        public string path => _path;
        
        public int rowCount { get; set; }
        
        public int columnCount { get; set; }
        
        public CsvReader(string path)
        {
            _fileName = Path.GetFileNameWithoutExtension(path).Split('_')[0].Trim() + "Conf";
            _path = path;
            GetFieldMap();
        }

        private void GetFieldMap()
        {
            _fieldMap = new Dictionary<string, ConfigTypeInfo>();
            var typeResolvers = ResolversTypeBuffer.buffer;
            using var reader = new StreamReader(path, Encoding.UTF8);
            var fieldComments = ReadCsvLine(reader);
            var fieldNames = ReadCsvLine(reader);
            var fieldTypes = ReadCsvLine(reader);
            if (fieldComments == null || fieldNames == null || fieldTypes == null)
            {
                ConfigLogger.LogError($"CSV config field header is incomplete: {path}");
                return;
            }

            columnCount = Math.Max(fieldComments.Count, Math.Max(fieldNames.Count, fieldTypes.Count));
            rowCount = 3;
            
            for (var column = 1; column <= columnCount; column++)
            {
                var fieldNameTemp = GetCell(fieldNames, column);
                if (string.IsNullOrEmpty(fieldNameTemp)) continue;

                var fieldComment = GetCell(fieldComments, column).Split('\n')[0];
                var fieldTypeTemp = GetCell(fieldTypes, column);
                if (string.IsNullOrEmpty(fieldTypeTemp) ||
                    fieldNameTemp.StartsWith("#") ||
                    fieldTypeTemp.StartsWith("##"))
                {
                    continue;
                }

                var refTypeName = "StringRef";
                var fieldType = "string";
                var lowerFieldType = fieldTypeTemp.ToLower();
                foreach (var typeResolver in typeResolvers)
                {
                    if (typeResolver.isMatch(lowerFieldType))
                    {
                        fieldType = typeResolver.TypeName();
                        refTypeName = typeResolver.GetType().Name;
                        break;
                    }
                }

                var split = fieldNameTemp.Split(':');
                if (string.IsNullOrEmpty(split[0])) continue;

                var fieldName = char.ToLower(split[0][0]) + split[0].Substring(1);
                var isKey = split.Length > 1 && split[1].ToLower() == "key";
                if (_fieldMap.TryGetValue(fieldName, out var typeInfo))
                {
                    if (typeInfo.isKey)
                    {
                        ConfigLogger.LogError($"[{fileName}]配置的Key {fieldComment} 字段不能重复");
                        continue;
                    }

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

        private static string GetCell(List<string> row, int column)
        {
            var index = column - 1;
            if (row == null || index < 0 || index >= row.Count) return string.Empty;
            return row[index]?.Trim() ?? string.Empty;
        }

        public string GetCellValue(int row, int column)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            for (int i = 0; i < row; i++)
            {
                reader.ReadLine();
            }
            var values = ReadCsvLine(reader);
            return GetCell(values, column);
        }

        private static List<string> ReadCsvLine(StreamReader reader)
        {
            var line = reader.ReadLine();
            if (line == null) return null;

            var result = new List<string>();
            var cellBuilder = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var current = line[i];
                if (current == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cellBuilder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (current == ',' && !inQuotes)
                {
                    result.Add(cellBuilder.ToString());
                    cellBuilder.Clear();
                }
                else
                {
                    cellBuilder.Append(current);
                }
            }

            result.Add(cellBuilder.ToString());
            return result;
            
        }
        
        public List<string> GetEnumList(int keyColumn)
        {
            var enumValues = new HashSet<string>();
            var list = new List<string>();
            using var reader = new StreamReader(path, Encoding.UTF8);
            ReadCsvLine(reader);
            ReadCsvLine(reader);
            ReadCsvLine(reader);

            while (!reader.EndOfStream)
            {
                var row = ReadCsvLine(reader);
                var valueString = GetCell(row, keyColumn);
                if (string.IsNullOrEmpty(valueString)) continue;

                if (enumValues.Contains(valueString))
                {
                    ConfigLogger.LogError($"配置的Key {valueString} 字段不能重复");
                    continue;
                }

                enumValues.Add(valueString);
                list.Add(valueString);
            }

            return list;
        }

        private StreamReader _reader;
        private List<string> _buffer;
        
        public void StartReadLine(int startLine)
        {
            _reader = new StreamReader(path, Encoding.UTF8);
            for (int i = 0; i < startLine; i++)
            {
                _reader.ReadLine();
            }

            _buffer = new List<string>();
        }

        public List<string> GetNextLine()
        {
            var line = _reader.ReadLine();
            if (line == null) return null;
            _buffer.Clear();
            var result = _buffer;
            var cellBuilder = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var current = line[i];
                if (current == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cellBuilder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (current == ',' && !inQuotes)
                {
                    result.Add(cellBuilder.ToString());
                    cellBuilder.Clear();
                }
                else
                {
                    cellBuilder.Append(current);
                }
            }

            result.Add(cellBuilder.ToString());
            return result;
        }
        
        public void Dispose()
        {
            _buffer?.Clear();
            _buffer = null;
            _reader?.Dispose();
            _reader = null;
            _fieldMap.Clear();
            _fieldMap = null;
            _fileName = null;
            _path = null;
        }
    }
}