using System;
using System.IO;

namespace PowerCellStudio.Editor
{
    [Serializable]
    public struct AssetReferenceInfo : IDisposable, IBundleReferenceBinary
    {
        public string assetPath;
        public string assetGuid;
        public string bundleName;
        public string[] assetsDependent;
        
        public void Dispose()
        {
            assetPath = null;
            assetGuid = null;
            bundleName = null;
            if (assetsDependent != null)
            {
                Array.Clear(assetsDependent, 0, assetsDependent.Length);
                assetsDependent = null;
            }
        }

        public void WriteBytes(BinaryWriter writer)
        {
            writer.Write(assetPath ?? string.Empty);
            writer.Write(assetGuid ?? string.Empty);
            writer.Write(bundleName ?? string.Empty);
            var length = assetsDependent?.Length ?? 0;
            writer.Write(length);
            for (var i = 0; i < length; i++)
            {
                var referenceAsset = assetsDependent[i];
                writer.Write(referenceAsset ?? string.Empty);
            }
        }

        public void ReadBytes(BinaryReader reader)
        {
            assetPath = reader.ReadString();
            assetGuid = reader.ReadString();
            bundleName = reader.ReadString();
            int count = reader.ReadInt32();
            assetsDependent = new string[count];
            for (int i = 0; i < count; i++)
            {
                assetsDependent[i] = reader.ReadString();
            }
        }
    }
}