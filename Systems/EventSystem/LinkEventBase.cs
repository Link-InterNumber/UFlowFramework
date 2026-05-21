namespace PowerCellStudio
{
    public delegate void BaseLinkAction();

    public delegate void BaseLinkAction<in T>(T data);

    public delegate void BaseLinkAction<in T, in TK>(T data, TK data2);

    public delegate void BaseLinkAction<in T, in TK, in TL>(T data, TK data2, TL data3);

    public interface IInvolke
    {
        public void Invoke();

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<in T>
    {
        public void Invoke(T data);

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<in T, in TK>
    {
        public void Invoke(T data, TK data2);

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<in T, in TK, in TL>
    {
        public void Invoke(T data, TK data2, TL data3);

        public int GetEventListenerCount();
    }
}