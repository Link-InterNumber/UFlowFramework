#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio.Editor
{
    public class ConfigWriter : IDisposable
    {
        private CsWriter _csFile;
            
        public ConfigWriter()
        {
            _csFile = new CsWriter();
        }


        private static void WriteHeadLines(CsWriter csWriter)
        {
            csWriter.WriteLine("//------------------------------------------------------------------------------")
                .WriteLine("// <auto generated>")
                .WriteLine("//\tShould not be edited manually!")
                .WriteLine("// </auto generated>")
                .WriteLine("//------------------------------------------------------------------------------")
                .Space();
        }
        
        public void GenerateRuntimeCsString(IConfigReader reader)
        {
            var confName = reader.fileName;
            var configTypeInfoList = reader.fieldMap.Values.ToArray();
            var keys = new List<ConfigTypeInfo>();
            foreach (var info in configTypeInfoList)
            {
                if (info.isKey) keys.Add(info);
            }

            WriteHeadLines(_csFile);
            
            _csFile.WriteUsing(
                "UnityEngine",
                "System.Linq",
                "System.Collections.Generic",
                "System.IO",
                "System");

            _csFile.Space(1);
            _csFile.WriteLine("namespace PowerCellStudio");
            _csFile.StartWriteBody();
            // ConfBase 类
            WriteConfBaseHandler.Write(_csFile, reader, configTypeInfoList, confName);
            // ConfBaseCollections类
            WriteConfBaseCollectionsHandler.Write(_csFile, reader, configTypeInfoList, confName);
            // ConfBaseData类
            // WriteConfBaseDataHandler.Write(_csFile, reader, configTypeInfoList, confName);
            _csFile.EndWriteBody();
        }
        
        public void GenerateEditorCsString(IConfigReader reader)
        {
            var confName = reader.fileName;
            var configTypeInfoList = reader.fieldMap.Values.ToArray();
            
            WriteHeadLines(_csFile);
            _csFile.WriteUsing("System", "System.IO", "System.Collections.Generic", "UnityEngine");
            _csFile.Space(1);
            _csFile.WriteLine("namespace PowerCellStudio");
            _csFile.StartWriteBody();

            WriteConfCreatorHandler.Write(_csFile, reader, configTypeInfoList, confName);
            _csFile.EndWriteBody();
        }
        
        public string GetCSFileString()
        {
            return _csFile.ToString();
        }

        public static string GenerateManagerCSString(List<string> confCollections)
        {
            var csFile = new CsWriter();
            WriteHeadLines(csFile);
            csFile.WriteLine("namespace PowerCellStudio");
            csFile.StartWriteBody();
            WriteConfManagerHandler.Write(csFile, confCollections);
            csFile.EndWriteBody();
            return csFile.ToString();
        }

        public void Clear()
        {
            _csFile.Clear();
        }

        public void Dispose()
        {
            _csFile?.Dispose();
        }
    }
}

#endif