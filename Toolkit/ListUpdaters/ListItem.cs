using UnityEngine;

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

    /// <summary>
    /// Class implementing the IListItem interface for Unity MonoBehaviour.
    /// 在Unity中实现IListItem接口的类。
    /// </summary>
    public class ListItem : MonoBehaviour, IListItem
    {
        // Stores the index of this item in the list.
        // 保存此列表项在列表中的索引。
        private int _index;
        
        /// <summary>
        /// Gets the index of the item.
        /// 获取列表项的索引。
        /// </summary>
        public int itemIndex => _index;

        /// <summary>
        /// Gets or privately sets the list updater holding this item.
        /// 获取或私下设置持有该项的列表更新器。
        /// </summary>
        public IListUpdater itemHolder { private set; get; }
        
        /// <summary>
        /// Gets the asset loader from the item holder.
        /// 从项持有者获取资产加载器。
        /// </summary>
        protected IAssetLoader assetLoader => itemHolder.AssetLoader;
    
        /// <summary>
        /// Updates the item's content with new data and assigns its index and holder.
        /// 使用新数据更新项的内容，并分配其索引和持有者。
        /// </summary>
        /// <param name="index">The index of the item.
        /// 项的索引。</param>
        /// <param name="data">The data to update the item with.
        /// 用于更新项的数据。</param>
        /// <param name="holder">The list updater holding this item.
        /// 持有该项的列表更新器。</param>
        public virtual void UpdateContent(int index, object data, IListUpdater holder)
        {
            _index = index;
            itemHolder = holder;
        }

        /// <summary>
        /// Explicit implementation to set the item index.
        /// 显式实现以设置项索引。
        /// </summary>
        /// <param name="index">The new index to set.
        /// 设置的新索引。</param>
        void IListItem.SetIndex(int index)
        {
            _index = index;
        }
    }
}