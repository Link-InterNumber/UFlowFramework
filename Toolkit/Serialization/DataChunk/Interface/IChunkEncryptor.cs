using System;

namespace PowerCellStudio
{
    public interface IChunkEncryptor
    {
        byte[] Encrypt(byte[] data);

        byte[] Decrypt(byte[] data, int offset, int count);
    }
}