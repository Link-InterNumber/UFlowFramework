namespace PowerCellStudio
{
    public class WriteConfBaseDataHandler
    {
        public static void Write(in CsWriter csWriter, in ExcelReader reader, in ConfigTypeInfo[] configTypeInfoList,
            in string confName)
        {
            csWriter.WriteLine("[Serializable]");
            csWriter.WriteLine("public class " + confName + "Data : ConfBaseData");
            csWriter.StartWriteBody();
            csWriter.WriteField(CsWriter.FieldSign.Public, $"List<{confName}>", "source", $"new List<{confName}>()");
            csWriter.EndWriteBody();
        }
    }
}