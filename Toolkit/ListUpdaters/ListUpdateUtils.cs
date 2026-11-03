using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public static class ListUpdateUtils
    {
        /// <summary>
        /// 从指定索引开始更新，自动创建缺少的项；负索引按零处理，超出末尾时不操作。
        /// Updates from the given index and creates missing items; negative indices become zero, indices beyond the end are ignored.
        /// </summary>
        /// <remarks>
        /// destroyUnused 仅移除尾部集合引用，否则保留原内容；资源由调用方释放。
        /// destroyUnused only removes trailing collection entries; otherwise their content is retained. The caller owns resource cleanup.
        /// 数据源和回调不得在遍历期间改变列表结构。工厂必须返回独立的非空项。
        /// The data source and callbacks must not change the list structure during iteration. The factory must return distinct, non-null items.
        /// </remarks>
        public static void UpdateList<TRoot, TItem>(TRoot root, IList<TItem> list, IEnumerable data,
            Func<TItem> createItem, int startIndex = 0, bool destroyUnused = false, Action<TItem> onDestroy = null)
            where TRoot : IListUpdater
            where TItem : IListItem
        {
            if (IsNull(root, nameof(root)) || IsNull(list, nameof(list)) ||
                IsNull(data, nameof(data)) || IsNull(createItem, nameof(createItem))) return;

            var index = Math.Max(0, startIndex);
            if (index >= list.Count)
            {
                for (var i = list.Count - 1; i < index; i++)
                {
                    var item = CreateItem(createItem);
                    if (ReferenceEquals(item, null))
                    {
                        LinkLogger.LogError($"ListUpdateUtils: Failed to create item at index {index}.");
                        return;
                    }
                    item.SetIndex(index);
                    list.Add(item);
                }
            }

            foreach (var tData in data)
            {
                TItem item;
                if (index >= list.Count)
                {
                    item = CreateItem(createItem);
                    if (ReferenceEquals(item, null)) 
                    {
                        LinkLogger.LogError($"ListUpdateUtils: Failed to create item at index {index}.");
                        return;
                    }
                    list.Add(item);
                }
                else
                {
                    item = list[index];
                }
                if (ReferenceEquals(item, null))
                {
                    LinkLogger.LogError($"ListUpdateUtils: The item at index {index} is null.");
                    index++;
                    continue;
                }
                item.SetIndex(index);
                item.UpdateContent(index, tData, root);
                list[index] = item;
                index++;
            }

            if (destroyUnused) RemoveUnusedItems(list, index, onDestroy);
        }

        /// <summary>
        /// 获取指定索引的项；越界返回默认值。
        /// Gets the item at an index, or the default value when out of range.
        /// </summary>
        public static TItem GetItem<TItem>(IList<TItem> list, int index)
            where TItem : IListItem
        {
            if (IsNull(list, nameof(list))) return default;
            return index >= 0 && index < list.Count ? list[index] : default;
        }

        /// <summary>
        /// 更新现有项的内容与索引；忽略越界索引和空项。
        /// Updates an existing item's content and index; ignores invalid indices and null entries.
        /// </summary>
        public static void UpdateItem<TRoot, TItem>(TRoot root, IList<TItem> list, int index, object data)
            where TRoot : IListUpdater
            where TItem : IListItem
        {
            if (IsNull(root, nameof(root)) || IsNull(list, nameof(list))) return;
            if (index < 0 || index >= list.Count) return;

            var item = list[index];
            if (ReferenceEquals(item, null)) return;
            item.SetIndex(index);
            item.UpdateContent(index, data, root);
            list[index] = item;
        }

        /// <summary>
        /// 创建并插入一项，更新后续项索引；插入位置限制在零到列表长度之间。
        /// Creates and inserts an item, reindexing following items; clamps the insertion index to the list bounds.
        /// </summary>
        /// <remarks>
        /// 工厂必须返回独立的非空项，回调不得改变列表结构。
        /// The factory must return a distinct, non-null item, and callbacks must not change the list structure.
        /// </remarks>
        public static void AddItem<TRoot, TItem>(TRoot root, IList<TItem> list, int index, object data, Func<TItem> createItem)
            where TRoot : IListUpdater
            where TItem : IListItem
        {
            if (IsNull(root, nameof(root)) || IsNull(list, nameof(list)) || IsNull(createItem, nameof(createItem)))
                return;

            index = Math.Min(Math.Max(0, index), list.Count);
            var item = CreateItem(createItem);
            if (ReferenceEquals(item, null)) return;
            list.Insert(index, item);
            Reindex(list, index);
            item = list[index];
            item.UpdateContent(index, data, root);
            list[index] = item;
        }

        /// <summary>
        /// 移除指定项并更新后续索引；忽略越界索引，资源由调用方释放。
        /// Removes an item and reindexes following items; ignores invalid indices. The caller owns resource cleanup.
        /// </summary>
        public static void RemoveItem<TItem>(IList<TItem> list, int index)
            where TItem : IListItem
        {
            if (IsNull(list, nameof(list))) return;
            if (index < 0 || index >= list.Count) return;
            list.RemoveAt(index);
            Reindex(list, index);
        }

        /// <summary>
        /// 清空集合引用；不销毁或释放项持有的资源。
        /// Clears collection entries without destroying or disposing item resources.
        /// </summary>
        public static void Clear<TItem>(IList<TItem> list)
            where TItem : IListItem
        {
            if (IsNull(list, nameof(list))) return;
            list.Clear();
        }

        /// <summary>
        /// 查找首个匹配的非空项；未找到时返回默认值。谓词不得改变列表结构。
        /// Finds the first matching non-null item, or returns the default value. The predicate must not change the list structure.
        /// </summary>
        public static TItem FindItem<TItem>(IList<TItem> list, Func<TItem, bool> match)
            where TItem : IListItem
        {
            if (IsNull(list, nameof(list)) || IsNull(match, nameof(match))) return default;

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (!ReferenceEquals(item, null) && match(item)) return item;
            }

            return default;
        }

        /// <summary>
        /// 对每个非空项执行操作；回调不得改变列表结构，值类型项按值传递。
        /// Invokes an action for each non-null item; callbacks must not change the list structure and value-type items are passed by value.
        /// </summary>
        public static void ForEachItem<TItem>(IList<TItem> list, Action<TItem> action)
            where TItem : IListItem
        {
            if (IsNull(list, nameof(list)) || IsNull(action, nameof(action))) return;

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (!ReferenceEquals(item, null)) action(item);
            }
        }

        /// <summary>
        /// 移除从 startIndex 开始的全部尾部项；忽略越界索引，资源由调用方释放。
        /// Removes all trailing entries starting at startIndex; ignores invalid indices. The caller owns resource cleanup.
        /// </summary>
        public static void RemoveUnusedItems<TItem>(IList<TItem> list, int startIndex = 0, Action<TItem> onDestroy = null)
            where TItem : IListItem
        {
            if (IsNull(list, nameof(list))) return;
            if (startIndex < 0 || startIndex >= list.Count) return;

            for (var i = list.Count - 1; i >= startIndex; i--)
            {
                var item = list[i];
                if (!ReferenceEquals(item, null))
                    onDestroy?.Invoke(list[i]);
                list.RemoveAt(i);
            }
        }

        private static TItem CreateItem<TItem>(Func<TItem> createItem)
            where TItem : IListItem
        {
            var item = createItem();
            if (ReferenceEquals(item, null))
                LinkLogger.LogError("ListUpdateUtils: The item factory returned a null item.");
            return item;
        }

        private static bool IsNull(object value, string parameterName)
        {
            if (!ReferenceEquals(value, null)) return false;
            LinkLogger.LogError($"ListUpdateUtils: Parameter '{parameterName}' cannot be null.");
            return true;
        }

        private static void Reindex<TItem>(IList<TItem> list, int startIndex)
            where TItem : IListItem
        {
            for (var i = startIndex; i < list.Count; i++)
            {
                var item = list[i];
                if (ReferenceEquals(item, null)) continue;
                item.SetIndex(i);
                list[i] = item;
            }
        }
    }
}