/****************************************************
    [Serializable]
    public partial class {ConfName} : ConfBase
    {
        public  {ConfName}({InputParameter})
        {
#for {InitCode}
            this._{FieldName} = {FieldName};
#forend
        }

#for {FieldDefine}
        [{FieldAttribute}]
        private {FieldType} _{FieldName};
        public {FieldType} {FieldName} => _{FieldName};
#forend
    }
****************************************************/


using System.Linq;

namespace PowerCellStudio
{
    public class WriteConfBaseHandler
    {
        public static void Write(in CsWriter csWriter, in ExcelReader reader, in ConfigTypeInfo[] configTypeInfoList, in string confName)
        {
            csWriter.WriteLine("[Serializable]")
                .WriteLine("public partial class " + confName + " : ConfBase")
                .StartWriteBody();

            WriteConstructor(csWriter, configTypeInfoList, confName);

            GetFieldAttribute(csWriter, configTypeInfoList);

            WriteWriteDataMethod(csWriter, configTypeInfoList);
            WriteReadDataMethod(csWriter, configTypeInfoList);

            csWriter.EndWriteBody()
                .Space();
        }

        private static void WriteConstructor(CsWriter csWriter, ConfigTypeInfo[] configTypeInfoList, string confName)
        {
            var inputParameter = configTypeInfoList.Select(o =>
            {
                var type = o.typeName;
                if (o.IsList)
                {
                    type = $"List<{type}>";
                }
                return $"{type} {o.fieldName.ToLower()}";
            }).ToArray();
            csWriter.StartWriteMethod(CsWriter.MethodSign.Public,
                CsWriter.MethodSign.None,
                "",
                confName,
                inputParameter);
            foreach (var t in configTypeInfoList)
            {
                csWriter.WriteLine($"this._{t.fieldName} = {t.fieldName.ToLower()};");
            }
            csWriter.EndWriteMethod();
        }

        private static void GetFieldAttribute(CsWriter csWriter, ConfigTypeInfo[] configTypeInfoList)
        {
            foreach (var info in configTypeInfoList)
            {
                var typeName = info.IsList ? $"List<{info.typeName}>" : info.typeName;
                csWriter.WriteLine((info.IsList || info.typeName.Contains("[]")) ? "[SerializeField, SerializeReference]" : "[SerializeField]")
                    .WriteField(CsWriter.FieldSign.Private,
                        typeName,
                        $"_{info.fieldName}")
                    .WriteLine($"///{info.comment}")
                    .WriteLine($"public {typeName} {info.fieldName} => _{info.fieldName};")
                    .Space();
            }
        }

        private static void WriteWriteDataMethod(CsWriter csWriter, ConfigTypeInfo[] configTypeInfoList)
        {
            csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "WriteData",
                    "BinaryWriter writer", "Encoding encoding");
            foreach (var info in configTypeInfoList)
            {
                if (info.IsList)
                {
                    csWriter.WriteVar($"{info.fieldName}Count", $"{info.fieldName}?.Count ?? -1");
                    csWriter.WriteLine($"writer.Write({info.fieldName}Count);");
                    csWriter.StartWriteIf($"{info.fieldName} != null")
                        .WriteLine($"foreach (var item in {info.fieldName})")
                        .StartWriteBody()
                        .WriteLine($"{info.refTypeName}.WriteItemData(item, writer, encoding);")
                        .EndWriteBody()
                        .EndWriteIf();
                }
                else
                {
                    csWriter.WriteLine($"{info.refTypeName}.WriteItemData({info.fieldName}, writer, encoding);");
                }
            }
            csWriter.EndWriteMethod();
        }

        private static void WriteReadDataMethod(CsWriter csWriter, ConfigTypeInfo[] configTypeInfoList)
        {
            csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.Override, "void", "ReadData",
                    "BinaryReader reader", "Encoding encoding");
            foreach (var info in configTypeInfoList)
            {
                if (info.IsList)
                {
                    csWriter.WriteLine($"int {info.fieldName}Count = reader.ReadInt32();");
                    csWriter.StartWriteIf($"{info.fieldName}Count >= 0")
                        .WriteLine($"_{info.fieldName} = new List<{info.typeName}>();")
                        .WriteLine($"for (int i = 0; i < {info.fieldName}Count; i++)")
                        .StartWriteBody()
                        .WriteVar("item", $"{info.refTypeName}.ReadItemData(reader, encoding)")
                        .WriteLine($"_{info.fieldName}.Add(item);")
                        .EndWriteBody()
                        .EndWriteIf();
                }
                else
                {
                    csWriter.WriteVar($"_{info.fieldName}", $"{info.refTypeName}.ReadItemData(reader, encoding)");
                }
            }
            csWriter.EndWriteMethod();
        }
    }
}