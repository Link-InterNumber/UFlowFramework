namespace PowerCellStudio
{
    internal static class BinaryFormatterResolverCache<T>
    {
        internal static readonly IBinaryFormatter<T> Instance = (IBinaryFormatter<T>)BinaryFormatterResolver.GetFormatter(typeof(T));
    }
}