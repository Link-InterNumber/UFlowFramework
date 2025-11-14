namespace PowerCellStudio
{
    public interface ICacheablePage
    {
        // Page暂存的时间（秒）
        // Page cache duration (seconds)
        public float retainTime { get; }
    }
}