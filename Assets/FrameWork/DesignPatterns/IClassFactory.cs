namespace LinkFrameWork.DesignPatterns
{
    public interface IClassFactory<in T> where T : class, new()
    {
        public TK Creat<TK>() where TK : T;
    }
}