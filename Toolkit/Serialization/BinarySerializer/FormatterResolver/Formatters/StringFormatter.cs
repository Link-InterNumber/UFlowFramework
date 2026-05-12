using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal sealed class StringFormatter : BinaryFormatterBase<string>
    {
        private const int StackBufferSize = 256;

        public override void Write(BinaryWriter writer, string value, Encoding encoding)
        {
            var bytes = encoding.GetBytes(value ?? string.Empty); // 预计算字节数，避免重复计算
            writer.Write(value == null ? -1 : bytes.Length); // 先写入长度，-1表示null
            if (bytes.Length > 0)
            {
                writer.Write(bytes); // 直接写入字节数据
            }
            // WriteString(writer, value, encoding);
        }

        public override string Read(BinaryReader reader, Encoding encoding)
        {
            var length = reader.ReadInt32();
            if (length < 0) return null;
            if (length == 0) return string.Empty;
            Span<byte> buffer = stackalloc byte[length];
            reader.Read(buffer);
            return encoding.GetString(buffer);
            // return ReadString(reader, encoding);
        }

        private static void WriteString(BinaryWriter writer, string value, Encoding encoding)
        {
            if (value == null)
            {
                writer.Write(-1);
                return;
            }

            int byteCount = encoding.GetByteCount(value);
            writer.Write(byteCount);

            if (byteCount == 0)
                return;

            if (byteCount <= StackBufferSize)
            {
                Span<byte> buffer = stackalloc byte[StackBufferSize];
                int written = encoding.GetBytes(value.AsSpan(), buffer);
                writer.Write(buffer.Slice(0, written));
            }
            else
            {
                byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
                Span<byte> buffer = rentedBuffer.AsSpan(0, byteCount);
                int written = encoding.GetBytes(value.AsSpan(), buffer);
                writer.Write(buffer.Slice(0, written));
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        private static string ReadString(BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                return null;

            if (length == 0)
                return string.Empty;

            if (length <= StackBufferSize)
            {
                Span<byte> buffer = stackalloc byte[StackBufferSize];
                ReadExactly(reader, buffer.Slice(0, length));
                return encoding.GetString(buffer.Slice(0, length));
            }

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(length);
            Span<byte> pooledSpan = rentedBuffer.AsSpan(0, length);
            ReadExactly(reader, pooledSpan);
            var result = encoding.GetString(pooledSpan);
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            return result;
        }

        private static void ReadExactly(BinaryReader reader, Span<byte> buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = reader.Read(buffer.Slice(offset));
                if (read <= 0)
                {
                    LinkLog.LogError("Unexpected end of stream while reading string data.");
                    return;
                }

                offset += read;
            }
        }
    }
}