namespace PowerCellStudio
{
    /// <summary>
    /// Interface defining methods and properties for a list item.
    /// 定义列表项的方法和属性的接口。
    /// </summary>
    public interface IListItem
    {
        /// <summary>
        /// Gets the index of the item in the list.
        /// 获取列表项的索引。
        /// </summary>
        public int itemIndex { get; }
        
        /// <summary>
        /// Sets the index of the item in the list.
        /// 设置列表项的索引。
        /// </summary>
        /// <param name="index">The new index to set.
        /// 设置的新索引。</param>
        public void SetIndex(int index);

        /// <summary>
        /// Gets the list updater that holds this item.
        /// 获取持有此项的列表更新器。
        /// </summary>
        public IListUpdater itemHolder { get; }

        /// <summary>
        /// Updates the content of the list item.
        /// 更新列表项的内容。
        /// </summary>
        /// <param name="index">The index of the item.
        /// 列表项的索引。</param>
        /// <param name="data">The data to update the item with.
        /// 用来更新列表项的数据。</param>
        /// <param name="holder">The list updater holding the item.
        /// 持有该项的列表更新器。</param>
        public void UpdateContent(int index, object data, IListUpdater holder);
    }
}