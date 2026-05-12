using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    [Serializable]
    public struct ScriptableAssetBundleData: IBinaryData
    {
        private const int StackBufferSize = 256;

        // public override int GetHashCode()
        // {
        //     return hashCode;
        // }

        // public int hashCode;
        public string assetName;
        public string assetBundle;
 
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AssetBundleData : { ");

            // sb.AppendFormat("hashCode: {0}, ", hashCode);
            sb.AppendFormat("assetName: {0}, ", assetName);
            sb.AppendFormat("assetBundle: {0} ", assetBundle);
            sb.Append(" }");
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScriptableAssetBundleData == false) return false;

            var o = (ScriptableAssetBundleData)obj;

            // if (hashCode != o.hashCode) return false;
            if (assetName != o.assetName) return false;
            if (assetBundle != o.assetBundle) return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (assetName != null ? assetName.GetHashCode() : 0);
                hash = hash * 31 + (assetBundle != null ? assetBundle.GetHashCode() : 0);
                return hash;
            }
        }

        private void WriteString(BinaryWriter writer, string value, Encoding encoding)
        {
            var bytes = encoding.GetBytes(value ?? string.Empty); // 预计算字节数，避免重复计算
            writer.Write(value == null ? -1 : bytes.Length); // 先写入长度，-1表示null
            if (bytes.Length > 0)
            {
                writer.Write(bytes); // 直接写入字节数据
            }
        }

        private string ReadString(BinaryReader reader, Encoding encoding)
        {
            var length = reader.ReadInt32();
            if (length < 0) return null;
            if (length == 0) return string.Empty;
            Span<byte> buffer = stackalloc byte[length];
            reader.Read(buffer);
            return encoding.GetString(buffer);
        }

        public void WriteData(BinaryWriter writer, Encoding encoding)
        {
            WriteString(writer, assetName, encoding);
            WriteString(writer, assetBundle, encoding);
        }

        public void ReadData(BinaryReader reader, Encoding encoding)
        {
            assetName = ReadString(reader, encoding);
            assetBundle = ReadString(reader, encoding);
        }

        public static bool operator ==(ScriptableAssetBundleData lhs, ScriptableAssetBundleData rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ScriptableAssetBundleData lhs, ScriptableAssetBundleData rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}