/****************************************************
    public partial class ConfigManager
    {
#for {{ConfigCollections}}
        private readonly {{CollectionName}} _{{fieldName}} = new {{CollectionName}}();
        public {{CollectionName}} {{fieldName}} => _{{fieldName}};

#forend
        public ConfigGroup GetGroupOfAllConfig()
        {
            var configGroup = new ConfigGroup();
#for {{ConfigCollections}}
            configGroup.Append(_{{fieldName}});
#forend
            return configGroup;
        }
    }
****************************************************/

using System.Collections.Generic;

namespace PowerCellStudio
{
    public class WriteConfManagerHandler
    {
        public static void Write(in CsWriter csWriter, in List<string> confCollections)
        {
            csWriter.WriteLine("public partial class ConfigManager : SingletonBase<ConfigManager>");
            csWriter.StartWriteBody();

            WriteFields(csWriter, confCollections);
            WriteGetGroupMethod(csWriter, confCollections);

            csWriter.EndWriteBody();
        }

        private static void WriteFields(CsWriter csWriter, List<string> confCollections)
        {
            for (var i = 0; i < confCollections.Count; i++)
            {
                var fieldName = GetFieldName(confCollections[i]);
                csWriter.WriteLine($"private readonly {confCollections[i]} _{fieldName} = new {confCollections[i]}();")
                    .WriteLine($"public {confCollections[i]} {fieldName} => _{fieldName};")
                    .Space();
            }
        }

        private static void WriteGetGroupMethod(CsWriter csWriter, List<string> confCollections)
        {
            csWriter.StartWriteMethod(CsWriter.MethodSign.Public, CsWriter.MethodSign.None, "ConfigGroup", "GetGroupOfAllConfig");
            csWriter.WriteVar("configGroup", "new ConfigGroup()");

            for (var i = 0; i < confCollections.Count; i++)
            {
                var fieldName = GetFieldName(confCollections[i]);
                csWriter.WriteLine($"configGroup.Append(_{fieldName});");
            }

            csWriter.WriteLine("return configGroup;");
            csWriter.EndWriteMethod();
        }

        private static string GetFieldName(string collectionName)
        {
            var fieldName = collectionName.Replace("Collections", "");
            return fieldName[0].ToString().ToLower() + fieldName.Substring(1);
        }
    }
}