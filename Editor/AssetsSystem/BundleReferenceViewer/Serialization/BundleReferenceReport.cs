using System;
using System.IO;
using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    [Serializable]
    public class BundleReferenceReport : IBundleReferenceBinary
    {
        public string dateTime;
        public int bundleCount;
        public List<BundleDefectReport> bundleDefectReports;
        
        public BundleReferenceReport()
        {
            bundleDefectReports = new List<BundleDefectReport>();
        }
        
        public void WriteBytes(BinaryWriter writer)
        {
            writer.Write(dateTime ?? string.Empty);
            writer.Write(bundleCount);
            writer.Write(bundleDefectReports.Count);
            foreach (var report in bundleDefectReports)
            {
                report.WriteBytes(writer);
            }
        }

        public void ReadBytes(BinaryReader reader)
        {
            dateTime = reader.ReadString();
            bundleCount = reader.ReadInt32();
            int reportCount = reader.ReadInt32();
            bundleDefectReports.Clear();
            for (int i = 0; i < reportCount; i++)
            {
                var report = new BundleDefectReport();
                report.ReadBytes(reader);
                bundleDefectReports.Add(report);
            }
        }
    }

    [Serializable]
    public class BundleDefectReport : IBundleReferenceBinary
    {
        public string bundleName;
        public DefectLevel defectLevel;
        public string tag;
        public string defectDetail;
        
        public void WriteBytes(BinaryWriter writer)
        {
            writer.Write(bundleName ?? string.Empty);
            
            // defectLevel只保存最严重的缺陷等级
            if ((defectLevel & DefectLevel.High) != 0)
                writer.Write((int)DefectLevel.High);
            else if ((defectLevel & DefectLevel.Medium) != 0)
                writer.Write((int)DefectLevel.Medium);
            else if ((defectLevel & DefectLevel.Low) != 0)
                writer.Write((int)DefectLevel.Low);
            else
                writer.Write((int)DefectLevel.None);

            writer.Write(tag ?? string.Empty);
            writer.Write(defectDetail ?? string.Empty);
        }

        public void ReadBytes(BinaryReader reader)
        {
            bundleName = reader.ReadString();
            defectLevel = (DefectLevel)reader.ReadInt32();
            tag = reader.ReadString();
            defectDetail = reader.ReadString();
        }
    }
}