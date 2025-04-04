using System;
using System.Text;

namespace PowerCellStudio
{
    public class CsWriter
    {
        public enum MethodSign
        {
            None,
            Public,
            Private,
            Protected,
            Static,
            Abstract,
            Sealed,
            Virtual,
            Partial,
            Override
        }
        
        public enum FieldSign
        {
            Public,
            Private,
            Protected,
        }
        
        public enum FieldSign2
        {
            Static,
            ReadOnly,
            Const,
            None
        }

        private StringBuilder _sb;

        private string _tab;

        private int _tabNumber;
        private int _tabCount
        {
            set
            {
                if (value < 1)
                {
                    _tab = "";
                    _tabNumber = 0;
                    return;
                }
                var sb = new StringBuilder();
                for (int i = 0; i < value; i++)
                {
                    sb.Append("\t");
                }
                _tab = sb.ToString();
                _tabNumber = value;
            }
            get => _tabNumber;
        }

        public CsWriter()
        {
            _sb = new StringBuilder();
            _tabCount = 0;
        }

        public CsWriter WriteLine(string line)
        {
            _sb.AppendLine($"{_tab}{line}");
            return this;
        }
        
        public CsWriter WriteWithoutLine(string line)
        {
            _sb.Append($"{_tab}{line}");
            return this;
        }
        
        public CsWriter WriteAppend(string line)
        {
            _sb.Append(line);
            return this;
        }
        
        public CsWriter WriteLineWithoutTab(string line)
        {
            _sb.AppendLine(line);
            return this;
        }
        
        public CsWriter Space(int count = 1)
        {
            if (count < 1) return this;
            for (int i = 0; i < count; i++)
            {
                _sb.Append("\n");
            }
            return this;
        }
        
        public CsWriter WriteVar(string name, string val)
        {
            _sb.AppendLine($"{_tab}var {name} = {val};");
            return this;
        }
        
        public CsWriter WriteUsing(params string[] usings)
        {
            if (usings == null) return this;
            foreach (var sring in usings)
            {
                _sb.Insert(0, $"using {sring};\n");
            }
            return this;
        }

        public CsWriter StartWriteIf(string line)
        {
            _sb.AppendLine(_tab + $"if ({line})");
            StartWriteBody();
            return this;
        }
        
        public CsWriter EndWriteIf()
        {
            EndWriteBody();
            return this;
        }

        public CsWriter WriteField(FieldSign sign, string type, string name, string value = null, FieldSign2 sign2 = FieldSign2.None)
        {
            switch (sign)
            {
                case FieldSign.Public:
                    _sb.Append($"{_tab}public ");
                    break;
                case FieldSign.Private:
                    _sb.Append($"{_tab}private ");
                    break;
                case FieldSign.Protected:
                    _sb.Append($"{_tab}protected ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sign), sign, null);
            }
            switch (sign2)
            {
                case FieldSign2.ReadOnly:
                    _sb.Append($"readOnly ");
                    break;
                case FieldSign2.Const:
                    _sb.Append($"const ");
                    break;
                case FieldSign2.Static:
                    _sb.Append($"static ");
                    break;
                default:
                    break;
            }
            
            _sb.Append($"{type} {name}");

            if (value != null)
            {
                _sb.Append($" = {value}");
            }
            _sb.AppendLine(";");
            return this;
        }
        
        public CsWriter StartWriteMethod(MethodSign sign, MethodSign sign2, string returnType, string name, params string[] paras)
        {
            switch (sign)
            {
                case MethodSign.Private:
                    _sb.Append($"{_tab}private ");
                    break;
                case MethodSign.Protected:
                    _sb.Append($"{_tab}protected ");
                    break;
                case MethodSign.Partial:
                    _sb.Append($"{_tab}partial ");
                    break;
                default:
                    _sb.Append($"{_tab}public ");
                    break;
            }
            switch (sign2)
            {
                case MethodSign.Abstract:
                    _sb.Append("abstract ");
                    break;
                case MethodSign.Sealed:
                    _sb.Append("sealed ");
                    break;
                case MethodSign.Virtual:
                    _sb.Append("virtual ");
                    break;
                case MethodSign.Override:
                    _sb.Append("override ");
                    break;
                case MethodSign.Static:
                    _sb.Append("static ");
                    break;
                default:
                    break;
            }
            _sb.Append($"{returnType} {name}(");

            if (paras != null && paras.Length > 0)
            {
                for (int i = 0; i < paras.Length; i++)
                {
                    _sb.Append($"{paras[i]}");
                    if (i != paras.Length - 1)
                    {
                        _sb.Append(", ");
                        if (i > 0 && i % 3 == 0) _sb.Append($"\n{_tab}\t\t\t\t\t\t");
                    }
                }
            }
            if (sign2 == MethodSign.Abstract || sign == MethodSign.Partial)
            {
                _sb.AppendLine(");");
                Space();
                return this;
            }
            _sb.AppendLine(")");
            StartWriteBody();
            return this;
        }
        
        public CsWriter EndWriteMethod()
        {
            EndWriteBody();
            Space();
            return this;
        }

        public CsWriter StartWriteBody()
        {
            _sb.AppendLine(_tab + "{");
            _tabCount++;
            return this;
        }

        public CsWriter EndWriteBody()
        {
            _tabCount--;
            _sb.AppendLine(_tab + "}");
            return this;
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}