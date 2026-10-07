namespace UFlowFramework
{
    public interface IInputAdapter<TKey>
    {
        InputStack<TKey> inputStack { get; }
    }
}