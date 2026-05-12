namespace PowerCellStudio
{
    public sealed class ChunkDataOptions
    {
        private static readonly ChunkDataOptions s_default = new ChunkDataOptions();
        private static readonly ChunkDataOptions s_withoutEncryption = new ChunkDataOptions { EnableEncryption = false };

        public bool EnableEncryption { get; set; } = true;

        public IChunkSerializer Serializer { get; set; }

        public IChunkEncryptor Encryptor { get; set; }

        public static ChunkDataOptions Default => s_default;

        public static ChunkDataOptions WithoutEncryption => s_withoutEncryption;

        internal IChunkSerializer ResolvedSerializer => Serializer ?? DefaultChunkSerializer.Instance;

        internal IChunkEncryptor ResolvedEncryptor => EnableEncryption ? Encryptor ?? DefaultChunkEncryptor.Instance : null;

        internal static ChunkDataOptions Resolve(ChunkDataOptions options)
        {
            return options ?? s_default;
        }
    }
}