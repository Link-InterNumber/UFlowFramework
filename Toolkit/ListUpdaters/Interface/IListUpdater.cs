using System.Collections;

namespace PowerCellStudio
{
    /// <summary>
    /// Interface for list updater behavior.
    /// 列表更新器行为的接口。
    /// </summary>
    public interface IListUpdater
    {
        /// <summary>
        /// Gets the asset loader.
        /// 获取资产加载器。
        /// </summary>
        public IAssetLoader AssetLoader { get; }

        /// <summary>
        /// Adds a listener for item interaction events.
        /// 为子节点交互事件添加监听器。
        /// </summary>
        public void AddInteractionListener(OnItemInteraction listener);

        /// <summary>
        /// Removes a listener for item interaction events.
        /// 为子节点交互事件移除监听器。
        /// </summary>
        public void RemoveInteractionListener(OnItemInteraction listener);
        
        /// <summary>
        /// Triggers the registered logic for a child item.
        /// 触发注册的子节点逻辑。
        /// </summary>
        /// <param name="item">The child item to interact with.
        /// 交互的子节点。</param>
        /// <param name="passData">Data passed for interaction.
        /// 交互传递的数据。</param>
        public void ItemInteraction(IListItem item, object passData);

        /// <summary>
        /// Updates the list with given data and optionally destroys unused items.
        /// 将数据列表传入并刷新列表，可以选择删除多余节点。
        /// </summary>
        /// <param name="data">The list data to update.
        /// 刷新的列表数据。</param>
        /// <param name="destroyUnused">If true, destroy unused items, otherwise hide them.
        /// 是否删除多余节点。</param>
        public void UpdateList(IEnumerable data, int startIndex = 0, bool destroyUnused = false);

        /// <summary>
        /// Updates a specific item at a given index.
        /// 更新索引位上子节点。
        /// </summary>
        /// <param name="index">The index of the item.
        /// 子节点索引。</param>
        /// <param name="data">The data to update the item with.
        /// 更新子节点的数据。</param>
        public void UpdateItem(int index, object data);

        /// <summary>
        /// Adds a new item at the specified index.
        /// 在指定索引处添加子节点。
        /// </summary>
        /// <param name="index">The index to add the item at.
        /// 添加子节点的索引。</param>
        /// <param name="data">The data for the new item.
        /// 新子节点的数据。</param>
        public void AddItem(int index, object data);

        /// <summary>
        /// Removes the item at the specified index.
        /// 移除索引位的子节点。
        /// </summary>
        /// <param name="index">The index of the item to remove.
        /// 被移除子节点的索引。</param>
        public void RemoveItem(int index);

        /// <summary>
        /// Hides all child items.
        /// 隐藏所有子节点。
        /// </summary>
        public void Clear();
    }
}