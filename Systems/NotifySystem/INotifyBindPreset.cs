

namespace PowerCellStudio
{
    public interface INotifyBindPreset
    {
        public void BindNodes(NotifyManager manager);
        // {
        //     manager.SetNodeParent(NotifyType.A, NotifyType.Root);
        //     manager.SetNodeParent(NotifyType.B, NotifyType.Root);
        //     manager.SetNodeParent(NotifyType.C, NotifyType.A);
        //     manager.SetNodeParent(NotifyType.D, NotifyType.B);
        //     manager.SetNodeParent(NotifyType.G, NotifyType.B);
        //     manager.SetNodeParent(NotifyType.E, NotifyType.C);
        //     manager.SetNodeParent(NotifyType.F, NotifyType.C);
        // }
    }
}
