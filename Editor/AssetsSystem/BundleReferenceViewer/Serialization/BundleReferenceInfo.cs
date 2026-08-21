using System;
using System.Collections.Generic;
using System.IO;

namespace PowerCellStudio.Editor
{
    [Serializable]
    public struct BundleReferenceInfo : IDisposable, IBundleReferenceBinary
    {
        public string bundleName;
        public string[] bundleDependent;
        public string[] defects;

        public void WriteBytes(BinaryWriter writer)
        {   
            writer.Write(bundleName ?? string.Empty);
            var length = bundleDependent?.Length ?? 0;
            writer.Write(length);
            for (var i = 0; i < length; i++)
            {
                var referenceBundle = bundleDependent[i];
                writer.Write(referenceBundle ?? string.Empty);
            }
            var defectsLength = defects?.Length ?? 0;
            writer.Write(defectsLength);
            for (var i = 0; i < defectsLength; i++)
            {
                var defect = defects[i];
                writer.Write(defect ?? string.Empty);
            }
        }
        
        public void ReadBytes(BinaryReader reader)
        {
            bundleName = reader.ReadString();
            int count = reader.ReadInt32();
            bundleDependent = new string[count];
            for (int i = 0; i < count; i++)
            {
                bundleDependent[i] = reader.ReadString();
            }
            int defectsCount = reader.ReadInt32();
            defects = new string[defectsCount];
            for (int i = 0; i < defectsCount; i++)
            {
                defects[i] = reader.ReadString();
            }
        }

        public void Dispose()
        {
            if (bundleDependent != null)
            {
                Array.Clear(bundleDependent, 0, bundleDependent.Length);
                bundleDependent = null;
            }
            if (defects != null)
            {
                Array.Clear(defects, 0, defects.Length);
                defects = null;
            }
            bundleName = null;
        }
    }
}