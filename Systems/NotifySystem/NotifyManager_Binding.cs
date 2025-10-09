using System;

namespace PowerCellStudio
{
    public sealed partial class NotifyManager
    {
        private partial void BindNodes()
        {
            SetNodeParent(NotifyType.A, NotifyType.Root);
            SetNodeParent(NotifyType.B, NotifyType.Root);
            SetNodeParent(NotifyType.C, NotifyType.A);
            SetNodeParent(NotifyType.D, NotifyType.B);
            SetNodeParent(NotifyType.E, NotifyType.C);
            SetNodeParent(NotifyType.F, NotifyType.C);
        }
    }
}
