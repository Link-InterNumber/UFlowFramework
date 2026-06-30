using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerCellStudio
{
    public class WriteConfEnumKeysHandler
    {
        private static void Write(in CsWriter csWriter, in ConfigTypeInfo keyInfo, in string confName, in IConfigReader excelReader)
        {
            var list = excelReader.GetEnumList(keyInfo.columns[0]);
            if(list.Count == 0) return;
            csWriter.WriteLine("public enum " + confName + "Key");
            csWriter.StartWriteBody();
            foreach (var enumValue in list)
            {
                csWriter.WriteLine(enumValue + ",");
            }
            csWriter.EndWriteBody();
            csWriter.Space();
            
            csWriter.WriteLine("public class " + confName + "KeyMap");
            csWriter.StartWriteBody();
            csWriter.WriteLine("public static Dictionary<string, " + confName + "Key> map = new Dictionary<string, " + confName + "Key>");
            csWriter.StartWriteBody();
            foreach (var enumValue in list)
            {
                csWriter.WriteLine($"{{\"{enumValue}\", {confName}Key.{enumValue}}},");
            }
            csWriter.EndWriteBody();
            csWriter.WriteLine(";\n");
            csWriter.EndWriteBody();
            csWriter.Space();
        }
    }
}