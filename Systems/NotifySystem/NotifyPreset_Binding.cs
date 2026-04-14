using System;


namespace PowerCellStudio
{
	public sealed class NotifyPreset_Binding : INotifyBindPreset
	{
		public void BindNodes(NotifyManager manager)
		{
			manager.SetNodeParent(NotifyType.Role, NotifyType.Root);
			manager.SetNodeParent(NotifyType.Bag, NotifyType.Root);
			manager.SetNodeParent(NotifyType.Bag_NewItem, NotifyType.Bag);
			manager.SetNodeParent(NotifyType.Bag_CanUse, NotifyType.Bag_NewItem);
			manager.SetNodeParent(NotifyType.Role_CanLvUp, NotifyType.Role);
			manager.SetNodeParent(NotifyType.Role_CanStarUp, NotifyType.Role);
		}
	}
}
