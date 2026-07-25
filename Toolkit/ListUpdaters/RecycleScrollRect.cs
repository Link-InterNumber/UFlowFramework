using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public partial class RecycleScrollRect : MonoBehaviour, IEnumerable, IListUpdater
    {
        #region define

        public enum ListDirection
        {
            HORIZONTAL = 0,
            VERTICAL = 1
        }
        
        private struct RecycleItem
        {
            public int index;
            public RectTransform transform;
            public IListItem listItem;
        }        

        #endregion
        
        #region opened field
        
        public LayoutGroup layoutGroup;
        public ScrollRect scroll;
        public Mask maskObj;
        public RectTransform prefab;
        public ListDirection direction = ListDirection.HORIZONTAL;
        public bool optimize = true;
        
        #endregion
        
        private int _count = 1;
        public int count => _count;
        private RectTransform _container => scroll.content;
        private int _numBuffer = 1;
        public int numberBuffer
        {
            get => _numBuffer;
            set => _numBuffer = Math.Max(1, value);
        }
        // private float _containerHalfSize;

        private Dictionary<int, RecycleItem> _itemDict = new Dictionary<int, RecycleItem>();
        private List<object> _dataList;

        private IRecycleScrollRectLayout _layoutHandler;
        
        private IAssetLoader _assetLoader;
        public IAssetLoader AssetLoader  => _assetLoader;

        public event OnItemInteraction onItemInteraction;

        public void AddInteractionListener(OnItemInteraction listener)
        {
            onItemInteraction += listener;
        }

        public void RemoveInteractionListener(OnItemInteraction listener)
        {
            onItemInteraction -= listener;
        }

        public void ItemInteraction(IListItem item, object passData)
        {
            if(item == null) return;
            onItemInteraction?.Invoke(item, item.itemIndex, passData);
        }

        private void Awake()
        {
            _assetLoader = AssetUtils.SpawnLoader(gameObject.name);
            layoutGroup.enabled = false;
            if (!scroll) return;
            if (optimize) scroll.onValueChanged.AddListener(OnScrollValueChanged);
            _container.anchorMin = Vector2.up;
            _container.anchorMax = Vector2.up;
            _container.pivot = Vector2.up;
            var contentSizeFitter = _container.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter) contentSizeFitter.enabled = false;
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
            layoutGroup = null;
            scroll = null;
            maskObj = null;
            prefab = null;
            onItemInteraction = null;
            _layoutHandler = null;
            _itemDict?.Clear();
            _dataList?.Clear();
            _itemDict = null;
            _dataList = null;
            AssetUtils.DeSpawnLoader(_assetLoader);
            if (!scroll) return;
            if (optimize) scroll.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        /// <summary>
        /// 将数据列表传入并刷新列表
        /// Updates the list with the given data and refreshes the list.
        /// </summary>
        /// <param name="datas">数据列表 - The list of data.</param>
        /// <param name="destroyUnused">是否销毁未使用的对象 - Whether to destroy unused objects.</param>
        public void UpdateList(IEnumerable datas, int startIndex = 0, bool destroyUnused = false)
        {
            if (datas == null) return;
            
            _itemDict.Clear();
            
            if (_dataList == null) _dataList = new List<object>();
            else if (startIndex <= 0) _dataList.Clear();
            else if (startIndex < _dataList.Count) _dataList.RemoveRange(startIndex, _dataList.Count - startIndex);
            
            foreach (var data in datas)
            {
                _dataList.Add(data);
            }
            _count = _dataList.Count;
            if (_count < 1)
            {
                _container.gameObject.SetActive(false);
                return;
            }
            _container.gameObject.SetActive(true);
            // Init();
            if (!prefab && transform.childCount > 0)
            {
                prefab = transform.GetChild(0) as RectTransform;
            }
            AsyncManager.Run(DelayInit());
        }

        private IEnumerator DelayInit()
        {
            yield return new WaitForEndOfFrame();;
            Init();
        }

        // Use this for initialization
        private void Init()
        {
            Vector2 prefabRectSize = prefab.rect.size;
            
            if (_layoutHandler == null)
            {
                if (layoutGroup is GridLayoutGroup gridLayoutGroup)
                {
                    prefabRectSize = gridLayoutGroup.cellSize;
                }
                _layoutHandler = CreateLayoutHandler(prefabRectSize);
                if (_layoutHandler == null) return;
                _layoutHandler.InitScroll(scroll);
            }

            _container.sizeDelta = _layoutHandler.GetContainerSize(_count);
            _layoutHandler.CalVisibleNum(maskObj.GetComponent<RectTransform>().rect.size, _numBuffer, out var numItems);
            numItems = optimize ? Mathf.Min(_count, numItems) : _count;
            var anchorValue = Vector2.up;
            for (int i = 0; i < numItems; i++)
            {
                var obj = _container.transform.childCount > i
                    ? _container.transform.GetChild(i).gameObject
                    : Instantiate(prefab.gameObject, _container.transform);
                var t = obj.GetComponent<RectTransform>();
                t.anchorMax = anchorValue;
                t.anchorMin = anchorValue;
                t.pivot = anchorValue;
                t.sizeDelta = prefabRectSize;
                t.anchoredPosition = _layoutHandler.GetItemLocalPos(i);
                obj.SetActive(true);
                var li = obj.GetComponent<IListItem>();
                li?.UpdateContent(i, _dataList[i], this);
                _itemDict[i] = new RecycleItem { index = i, transform = t, listItem = li };
            }
            var removeNumber = _container.transform.childCount;
            if (numItems < removeNumber)
            {
                var toDestroy = ListPool<GameObject>.Get();
                for (int i = numItems; i < removeNumber; i++)
                {
                    toDestroy.Add(_container.transform.GetChild(i).gameObject);
                }
                foreach (var go in toDestroy)
                {
                    GameObject.Destroy(go);
                }
                ListPool<GameObject>.Release(toDestroy);
            }
            _previousIndex = -1;
            ForceRebuild();
        }

        private IRecycleScrollRectLayout CreateLayoutHandler(Vector2 prefabRectSize)
        {
            if (layoutGroup is HorizontalLayoutGroup horizontalLayoutGroup)
            {
                return new RSHorizontalLayout(prefabRectSize, layoutGroup.padding,
                    new Vector2(horizontalLayoutGroup.spacing, 0f));
            }
            if (layoutGroup is VerticalLayoutGroup verticalLayoutGroup)
            {
                return new RSVerticalLayout(prefabRectSize, layoutGroup.padding,
                    new Vector2(0, verticalLayoutGroup.spacing));
            }
            if (layoutGroup is GridLayoutGroup gridLayoutGroup)
            {
                return new RSGridLayout(gridLayoutGroup.startAxis, prefabRectSize, layoutGroup.padding,
                    gridLayoutGroup.spacing);
            }

            return null;
        }

        private IEnumerator DelayReorderItemsByPos()
        {
            yield return null;
            ForceRebuild();
        }

        private void OnScrollValueChanged(Vector2 normVector)
        {
            ForceRebuild();
        }
        
        private int _previousIndex = -1;

        /// <summary>
        /// 强制重建列表的可见部分
        /// Forces a rebuild of the visible portion of the list.
        /// </summary>
        public void ForceRebuild()
        {
            if(_dataList == null || _dataList.Count == 0 || _layoutHandler == null) return;
            
            var passLength = direction == ListDirection.HORIZONTAL
                ? -_container.localPosition.x
                : _container.localPosition.y;
            var firstIndex = 0;
            var maxVisibleIndex = _layoutHandler.visibleNum - 1 + _numBuffer;
            _layoutHandler.GetViewIndexRange(passLength, _numBuffer, _count, ref  firstIndex, ref maxVisibleIndex);
            // passLength = Mathf.Clamp(passLength, 0f, (_count - _numVisible + 0.5f) * _prefabSize);
            // var firstIndex = Mathf.Clamp(Mathf.FloorToInt(passLength / _prefabSize), 0, _count - _numVisible);
            
            if (_previousIndex == firstIndex) return;
            // var maxVisibleIndex = firstIndex + _numVisible - 1 + _numBuffer;
            var newKeys = ListPool<int>.Get();
            for (var i = firstIndex; i <= maxVisibleIndex; i++)
            {
                if(_itemDict.ContainsKey(i)) continue;
                newKeys.Add(i);
            }
            if (newKeys.Count == 0)
            {
                ListPool<int>.Release(newKeys);
                return;
            }
            // keys中有而newKeys中没有的
            var removeKeys = _itemDict.Keys.Where(o => o < firstIndex || o > maxVisibleIndex).ToList();
            if (removeKeys.Count == 0)
            {
                ListPool<int>.Release(newKeys);
                return;
            }

            var loopCount = Mathf.Min(removeKeys.Count, newKeys.Count);
            for (var i = 0; i < loopCount; i++)
            {
                var item = _itemDict[removeKeys[i]];
                var newIndex = newKeys[i];
                MoveItemByIndex(item, newIndex);
                item.index = newIndex;
                _itemDict.Remove(removeKeys[i]);
                _itemDict[newIndex] = item;
            }
            ListPool<int>.Release(newKeys);
            _previousIndex = firstIndex;
        }

        private void MoveItemByIndex(RecycleItem item, int index)
        {
            var posIndex = (index >= 0 && index <= _dataList.Count - 1) ? index : -2;
            item.transform.anchoredPosition = _layoutHandler.GetItemLocalPos(posIndex);
            if(_dataList.Count - 1 < index) return; 
            item.listItem?.UpdateContent(index, _dataList[index], this);
        }

        /// <summary>
        /// 获取指定索引的数据
        /// Retrieves data at the specified index.
        /// </summary>
        /// <param name="index">要检索的数据的索引 - The index of the data to retrieve.</param>
        /// <returns>指定索引处的数据 - The data at the specified index.</returns>
        public object GetData(int index)
        {
            if (_dataList.Count - 1 >= index && index >= 0) return _dataList[index];
            return null;
        }

        /// <summary>
        /// 更新指定索引的列表项内容
        /// Updates the content of the list item at the specified index.
        /// </summary>
        /// <param name="index">要更新的项的索引 - The index of the item to update.</param>
        /// <param name="data">新的数据 - The new data for the item.</param>
        public void UpdateItem(int index, object data)
        {
            if (index > _dataList.Count - 1 || index < 0) return;
            _dataList[index] = data;
            if (!_itemDict.TryGetValue(index, out var item)) return;
            item.listItem?.UpdateContent(index, data, this);
        }

        /// <summary>
        /// 添加新项目到指定索引位置
        /// Adds a new item at the specified index position.
        /// </summary>
        /// <param name="index">插入新项的索引位置 - The index position to insert the new item.</param>
        /// <param name="data">要插入的新数据 - The new data to insert.</param>
        public void AddItem(int index, object data)
        {
            if (index < 0) return;
            var newdata = new List<object>();
            if (index >= _dataList.Count - 1)
            {
                newdata.AddRange(_dataList);
                newdata.Add(data);
            }
            else if (index < _dataList.Count)
            {
                for (var i = 0; i < _dataList.Count; i++)
                {
                    if(i == index) newdata.Add(data);
                    newdata.Add(_dataList[i]);
                }
            }
            UpdateList(newdata);
        }

        /// <summary>
        /// 从列表中移除指定索引的项目
        /// Removes the item at the specified index from the list.
        /// </summary>
        /// <param name="index">要移除的项的索引位置 - The index position of the item to remove.</param>
        public void RemoveItem(int index)
        {
            if (index < 0 || index > _dataList.Count - 1) return;
            var newdata = new List<object>();
            for (var i = 0; i < _dataList.Count; i++)
            {
                if(i == index) continue;
                newdata.Add(_dataList[i]);
            }
            UpdateList(newdata);
        }

        /// <summary>
        /// 清空列表中的所有项目
        /// Clears all items from the list.
        /// </summary>
        public void Clear()
        {
            var newdata = new List<object>();
            UpdateList(newdata);
        }

        public IEnumerator GetEnumerator()
        {
            return _dataList.GetEnumerator();
            // ReorderItemsByPos(scroll.normalizedPosition);
        }

        public object this[int index]
        {
            get => _dataList[index];
            set => UpdateItem(index, value);
        }
        
        /// <summary>
        /// 检查索引处的数据是否在可见范围内
        /// Checks whether the data at the specified index is within the visible range.
        /// </summary>
        /// <param name="index">检查的索引 - The index to check.</param>
        /// <param name="item">如果可见，返回项目；否则返回null - Returns the item if visible, otherwise returns null.</param>
        /// <returns>返回是否在可见范围内 - Returns whether the item is within the visible range.</returns>
        public bool IsDataInVisible(int index, out IListItem item)
        {
            if (_itemDict.TryGetValue(index, out var listItem))
            {
                item = listItem.listItem;
                return true;
            }
            item = null;
            return false;
        }
    }
}