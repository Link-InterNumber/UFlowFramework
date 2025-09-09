namespace PowerCellStudio
{
    public interface ILinkEntity : IIndex
    {
        public bool isDestroy { get; set; }
        
        public void Destroy();
    }
}