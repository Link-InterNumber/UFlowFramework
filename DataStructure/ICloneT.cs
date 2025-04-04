namespace PowerCellStudio
{
    public interface ICloneT<out T>
    {
        T Clone();
    }
}