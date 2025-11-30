namespace PowerCellStudio
{
    public delegate void BaseLinkAction();

    public delegate void BaseLinkAction<T>(T data);

    public delegate void BaseLinkAction<T, TK>(T data, TK data2);

    public delegate void BaseLinkAction<T, TK, TL>(T data, TK data2, TL data3);

    public interface IInvolke
    {
        public void Invoke();

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<T>
    {
        public void Invoke(T data);

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<T, TK>
    {
        public void Invoke(T data, TK data2);

        public int GetEventListenerCount();
    }
    
    public interface IInvolke<T, TK, TL>
    {
        public void Invoke(T data, TK data2, TL data3);

        public int GetEventListenerCount();
    }
}