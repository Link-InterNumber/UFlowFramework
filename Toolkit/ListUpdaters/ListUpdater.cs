using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public void UpdateList(IList data, bool destroyUnused = false);

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

    /// <summary>
    /// Concrete implementation of IListUpdater for updating lists.
    /// IListUpdater接口的具体实现，用以更新列表。
    /// </summary>
    public class ListUpdater : MonoBehaviour, IListUpdater
    {
        // Reference to the prefab used for list items.
        private GameObject _prefab;
        
        private IAssetLoader _assetLoader;

        public IAssetLoader AssetLoader => _assetLoader;
        
        public event OnItemInteraction onItemInteraction;

        public void ItemInteraction(IListItem item, object passData)
        {
            if (item == null) return;
            onItemInteraction?.Invoke(item, item.itemIndex, passData);
        }

        private void Awake()
        {
            _assetLoader = AssetUtils.SpawnLoader();
        }

        private void OnDestroy()
        {
            AssetUtils.DeSpawnLoader(_assetLoader);
        }

        /// <summary>
        /// Gets the count of active items.
        /// 获取活动子节点的数量。
        /// </summary>
        public int count
        {
            get
            {
                var c = 0;
                foreach (Transform o in transform)
                {
                    if (o.gameObject.activeSelf) c++;
                }
                return c;
            }
        }
        
        private GameObject GetPrefab()
        {
            if (!_prefab && transform.childCount > 0)
            {
                _prefab = transform.GetChild(0).gameObject;
            }
            return _prefab;
        }

        /// <summary>
        /// Updates the list with the specified data at intervals, optionally destroying unused items.
        /// 间隔更新列表，可以选择删除多余节点。
        /// </summary>
        /// <param name="data">The data to update the list with.
        /// 刷新列表的数据。</param>
        /// <param name="interval">Time interval between updates.
        /// 更新之间的时间间隔。</param>
        /// <param name="destroyUnused">If true, destroy unused items, otherwise hide them.
        /// 若为真则删除多余节点。</param>
        public void UpdateListWithInterval(IList data, float interval, bool destroyUnused = false)
        {
            if (interval <= 0)
            {
                UpdateList(data, destroyUnused);
                return;
            }
            if (data == null || !GetPrefab()) return;
            if (_updateCoroutine != null) ApplicationManager.instance.StopCoroutine(_updateCoroutine);
            if (destroyUnused)
            {
                var toDestroy = new List<GameObject>();
                for (var i = data.Count; i < transform.childCount; i++)
                {
                    toDestroy.Add(transform.GetChild(i).gameObject);
                }
                foreach (var go in toDestroy)
                {
                    GameObject.Destroy(go);
                }
            }
            else
            {
                for (var i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }
            _updateCoroutine = ApplicationManager.instance.StartCoroutine(UpdateListWithIntervalHandler(data, interval));
        }

        private Coroutine _updateCoroutine;

        private IEnumerator UpdateListWithIntervalHandler(IList data, float interval)
        {
            for (var i = 0; i < data.Count; i++)
            {
                if (transform.childCount <= i)
                {
                    var go = Instantiate(_prefab, transform);
                    go.SetActive(true);
                }
                var item = transform.GetChild(i).GetComponent<IListItem>();
                if (item == null) continue;
                var o = data[i];
                item.UpdateContent(i, o, this);
                yield return new WaitForSeconds(interval);
            }
            _updateCoroutine = null;
        }

        public void UpdateList(IList data, bool destroyUnused = false)
        {
            if (data == null || !GetPrefab()) return;
            for (var i = 0; i < data.Count; i++)
            {
                GameObject go;
                if (transform.childCount <= i)
                {
                    go = Instantiate(_prefab, transform);
                }
                else
                {
                    go = transform.GetChild(i).gameObject;
                }
                go.SetActive(true);
                var item = go.GetComponent<IListItem>();
                if (item == null) continue;
                var o = data[i];
                item.UpdateContent(i, o, this);
            }

            if (destroyUnused)
            {
                var toDestroy = new List<GameObject>();
                for (var i = data.Count; i < transform.childCount; i++)
                {
                    toDestroy.Add(transform.GetChild(i).gameObject);
                }
                foreach (var go in toDestroy)
                {
                    GameObject.Destroy(go);
                }
            }
            else
            {
                for (var i = data.Count; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }
        }

        public void Clear()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Gets an item by index.
        /// 通过索引获取子节点。
        /// </summary>
        /// <param name="index">The index of the item.
        /// 子节点的索引。</param>
        /// <typeparam name="T">Type of the item, must inherit from MonoBehaviour and IListItem.
        /// 项的类型，必须继承自 MonoBehaviour 和 IListItem。</typeparam>
        /// <returns>The item at the specified index.
        /// 指定索引处的子节点。</returns>
        public T GetItem<T>(int index) 
            where T :MonoBehaviour
        {
            if (index < 0 || index >= transform.childCount) return null;
            return transform.GetChild(index).GetComponent<T>();
        }

        public void UpdateItem(int index, object data)
        {
            if (index < 0 || index >= transform.childCount) return;
            var item = transform.GetChild(index).GetComponent<IListItem>();
            if (item == null) return;
            item.UpdateContent(index, data, this);
        }

        public void AddItem(int index, object data)
        {
            if (index < 0) return;

            // Find the first unused node
            var usedIndex = -1;
            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf) continue;
                usedIndex = i;
                break;
            }

            // If no unused node is found, create a new node
            if (usedIndex == -1)
            {
                var go = Instantiate(_prefab, transform);
                var realIndex = Mathf.Min(index, transform.childCount - 1);
                go.transform.SetSiblingIndex(realIndex);
                go.SetActive(true);
                var item = go.GetComponent<IListItem>();
                if (item == null) return;
                item.UpdateContent(realIndex, data, this);

                for (int i = index; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if(!child.gameObject.activeSelf) break;
                    var item2 = child.GetComponent<IListItem>();
                    if (item2 == null) continue;
                    item2.SetIndex(i);
                }
                return;
            }

            // If an unused node is found, set it active
            var itemGo = transform.GetChild(usedIndex);
            itemGo.gameObject.SetActive(true);
            itemGo.SetSiblingIndex(Mathf.Min(index, usedIndex));
            var item1 = itemGo.GetComponent<IListItem>();
            if (item1 == null) return;
            item1.UpdateContent(Mathf.Min(index, usedIndex), data, this);

            for (int i = index; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if(!child.gameObject.activeSelf) break;
                var item2 = child.GetComponent<IListItem>();
                if (item2 == null) continue;
                item2.SetIndex(i);
            }
        }

        public void RemoveItem(int index)
        {
            if (index < 0 ) return;
            if (index >= transform.childCount) return;
            var go = transform.GetChild(index);
            go.gameObject.SetActive(false);
            go.SetAsLastSibling();
            for (int i = index; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child.gameObject.activeSelf) break;
                var item2 = child.GetComponent<IListItem>();
                if (item2 == null) continue;
                item2.SetIndex(i);
            }
        }

        /// <summary>
        /// Finds a child item that matches the specified criteria.
        /// 寻找符合条件的子节点。
        /// </summary>
        /// <param name="match">The criteria to match.
        /// 匹配条件。</param>
        /// <typeparam name="T">Type of the item, must inherit from MonoBehaviour and IListItem.
        /// 项的类型，必须继承自 MonoBehaviour 和 IListItem。</typeparam>
        /// <returns>The first matching item, or null if none found.
        /// 第一个匹配的子节点，若无则为 null。</returns>
        public T FindItem<T>(Func<T, bool> match) 
            where T : MonoBehaviour, IListItem
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var item = transform.GetChild(i).GetComponent<T>();
                if (!item || !match.Invoke(item)) continue;
                return item;
            }
            return null;
        }
        
        /// <summary>
        /// Performs an action on each child item.
        /// 对每个子节点进行指定动作。
        /// </summary>
        /// <param name="action">The action to perform on each item.
        /// 用于处理每个子节点的动作。</param>
        /// <typeparam name="T">Type of the item, must inherit from MonoBehaviour and IListItem.
        /// 项的类型，必须继承自 MonoBehaviour 和 IListItem。</typeparam>
        public void ForEachItem<T>(Action<T> action) 
            where T : MonoBehaviour, IListItem
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var item = transform.GetChild(i).GetComponent<T>();
                if (!item) continue;
                action.Invoke(item);
            }
        }
        
        /// <summary>
        /// Removes unused items from the list of child items.
        /// 删除无用节点。
        /// </summary>
        public void RemoveUnusedItems()
        {
            for (var i = transform.childCount - 1; i > 0; i--)
            {
                if (!transform.GetChild(i).gameObject.activeSelf)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
            }
        }
    }
}