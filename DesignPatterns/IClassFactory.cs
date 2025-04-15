namespace PowerCellStudio
{
    /// <summary>
    /// 工厂模式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IClassFactory<in T> where T : class, new()
    {
        public TK Creat<TK>() where TK : T, new();
    }
}