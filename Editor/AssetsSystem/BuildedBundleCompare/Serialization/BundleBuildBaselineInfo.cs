using System;
using System.IO;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// 已构建 Bundle 的基准记录，用于跨次构建比较。
    /// Baseline data for a built Bundle, used for comparison across builds.
    /// </summary>
    [Serializable]
    internal struct BundleBuildBaselineInfo : IDisposable, IBundleReferenceBinary
    {
        public string bundleName;
        public long size;
        public string[] assetNames;
        public string[] dependentBundles;

        public void WriteBytes(BinaryWriter writer)
        {
            writer.Write(bundleName ?? string.Empty);
            writer.Write(size);
            WriteStrings(writer, assetNames);
            WriteStrings(writer, dependentBundles);
        }

        public void ReadBytes(BinaryReader reader)
        {
            bundleName = reader.ReadString();
            size = reader.ReadInt64();
            assetNames = ReadStrings(reader);
            dependentBundles = ReadStrings(reader);
        }

        public void Dispose()
        {
            bundleName = null;
            size = 0;
            Clear(assetNames);
            Clear(dependentBundles);
            assetNames = null;
            dependentBundles = null;
        }

        private static void WriteStrings(BinaryWriter writer, string[] values)
        {
            var count = values?.Length ?? 0;
            writer.Write(count);
            for (var i = 0; i < count; i++)
                writer.Write(values[i] ?? string.Empty);
        }

        private static string[] ReadStrings(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Bundle 基准文件包含无效的字符串数量。");

            var values = new string[count];
            for (var i = 0; i < count; i++)
                values[i] = reader.ReadString();
            return values;
        }

        private static void Clear(string[] values)
        {
            if (values != null)
                Array.Clear(values, 0, values.Length);
        }
    }

    [Serializable]
    internal struct BundleBuildBaselineFile : IBundleReferenceBinary
    {
        public int version;
        public BundleBuildBaselineInfo[] bundles;

        public void WriteBytes(BinaryWriter writer)
        {
            writer.Write(version);
            var count = bundles?.Length ?? 0;
            writer.Write(count);
            for (var i = 0; i < count; i++)
                bundles[i].WriteBytes(writer);
        }

        public void ReadBytes(BinaryReader reader)
        {
            version = reader.ReadInt32();
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Bundle 基准文件包含无效的 Bundle 数量。");

            bundles = new BundleBuildBaselineInfo[count];
            for (var i = 0; i < count; i++)
                bundles[i].ReadBytes(reader);
        }
    }
}
