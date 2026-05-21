using System;
using System.IO;

namespace PowerCellStudio
{
    internal static class PersistenceEnvelopeUtility
    {
        private const string StringEnvelopeMagic = "UFlowPersistenceEnvelope";
        private const string BinaryEnvelopeMagic = "UFlowPersistenceBinaryEnvelope";
        private const int BinaryEnvelopeFormatVersion = 1;

        [Serializable]
        private class StringEnvelope
        {
            public string magic;
            public int version;
            public string payload;
        }

        public static string PackString(int version, string payload)
        {
            var envelope = new StringEnvelope
            {
                magic = StringEnvelopeMagic,
                version = Math.Max(1, version),
                payload = payload ?? "{}"
            };
            return SerializeUtils.SerializeToJson(envelope);
        }

        public static bool TryUnpackString(string content, out int version, out string payload)
        {
            version = 0;
            payload = content;
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            try
            {
                var envelope = SerializeUtils.DeserializeFromJson<StringEnvelope>(content);
                if (envelope == null || envelope.magic != StringEnvelopeMagic)
                {
                    return false;
                }

                version = Math.Max(0, envelope.version);
                payload = envelope.payload ?? string.Empty;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static byte[] PackBinary(int version, byte[] payload)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(BinaryEnvelopeMagic);
            writer.Write(BinaryEnvelopeFormatVersion);
            writer.Write(Math.Max(1, version));
            writer.Write(payload?.Length ?? 0);
            if (payload != null && payload.Length > 0)
            {
                writer.Write(payload);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static bool TryUnpackBinary(byte[] content, out int version, out byte[] payload)
        {
            version = 0;
            payload = content;
            if (content == null || content.Length == 0)
            {
                return false;
            }

            try
            {
                using var stream = new MemoryStream(content, false);
                using var reader = new BinaryReader(stream);
                if (reader.ReadString() != BinaryEnvelopeMagic)
                {
                    return false;
                }

                if (reader.ReadInt32() != BinaryEnvelopeFormatVersion)
                {
                    return false;
                }

                version = Math.Max(0, reader.ReadInt32());
                var length = reader.ReadInt32();
                payload = reader.ReadBytes(length);
                return payload.Length == length;
            }
            catch
            {
                version = 0;
                payload = content;
                return false;
            }
        }
    }
}