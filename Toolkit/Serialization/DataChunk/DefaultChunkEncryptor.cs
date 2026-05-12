using System;

namespace PowerCellStudio
{
    public sealed class DefaultChunkEncryptor : IChunkEncryptor
    {
        public static DefaultChunkEncryptor Instance { get; } = new DefaultChunkEncryptor();

        private DefaultChunkEncryptor()
        {
        }

        public byte[] Encrypt(byte[] data)
        {
            return EncryptUtils.AESEncrypt(data, ConstSetting.FileEncryptionKey);
        }

        public byte[] Decrypt(byte[] data, int offset, int count)
        {
            return EncryptUtils.AESDecrypt(data, ConstSetting.FileEncryptionKey, offset, count);
        }
    }
}