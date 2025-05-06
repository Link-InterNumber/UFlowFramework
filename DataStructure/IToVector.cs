namespace PowerCellStudio
{
    public interface IToVector2
    {
        public Vector2 ToVector();
    }

    publuc interface IToVector3: IToVector2
    {
        public Vector3 ToVector();
    }
}