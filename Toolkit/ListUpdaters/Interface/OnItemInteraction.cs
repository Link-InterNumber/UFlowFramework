namespace PowerCellStudio
{
    /// <summary>
    /// Delegate for handling item interaction events.
    /// 处理子节点交互事件的委托。
    /// </summary>
    /// <param name="item">The item interacted with.
    /// 交互的子节点。</param>
    /// <param name="index">Index of the item.
    /// 子节点的索引。</param>
    /// <param name="passData">Data passed during interaction.
    /// 交互传递的数据。</param>
    public delegate void OnItemInteraction(IListItem item, int index, object passData);
}