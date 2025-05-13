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

        public event OnItemInteraction onItemInteraction;

        public void ItemInteraction(IListItem item, object passData)
        {
            if(item == null) return;
            onItemInteraction?.Invoke(item, item.itemIndex, passData);
        }

        private void Awake()
        {
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
            if (!scroll) return;
            if (optimize) scroll.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        /// <summary>
        /// 将数据列表传入并刷新列表
        /// </summary>
        /// <param name="datas"></param>
        public void UpdateList(IList datas, bool destroyUnused = false)
        {
            if (datas == null) return;
            _count = datas.Count;
            _itemDict.Clear();
            if (_dataList == null) _dataList = new List<object>();
            else _dataList.Clear();
            if (_count < 1)
            {
                _container.gameObject.SetActive(false);
                return;
            }
            _container.gameObject.SetActive(true);
            for (var i = 0; i < datas.Count; i++)
            {
                _dataList.Add(datas[i]);
            }
            // Init();
            ApplicationManager.instance.StartCoroutine(DelayInit());
        }

        private IEnumerator DelayInit()
        {
            yield return null;
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
                _itemDict.Add(i, new RecycleItem{index = i, transform = t, listItem = li});
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
            ApplicationManager.instance.StartCoroutine(DelayReorderItemsByPos());
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
        /// 强制刷新显示范围内的数据
        /// </summary>
        public void ForceRebuild()
        {
            if(_dataList == null) return;
            
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
        /// 获取索引位上的数据
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public object GetData(int index)
        {
            if (_dataList.Count - 1 >= index && index >= 0) return _dataList[index];
            return null;
        }

        public void UpdateItem(int index, object data)
        {
            if (index > _dataList.Count - 1 || index < 0) return;
            _dataList[index] = data;
            if (!_itemDict.TryGetValue(index, out var item)) return;
            item.listItem?.UpdateContent(index, data, this);
        }

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

        public void Clear()
        {
            var newdata = new List<object>();
            UpdateList(newdata);
        }

        public IEnumerator GetEnumerator()
        {
            yield return _dataList.GetEnumerator();
            // ReorderItemsByPos(scroll.normalizedPosition);
        }

        public object this[int index]
        {
            get => _dataList[index];
            set => UpdateItem(index, value);
        }
        
        /// <summary>
        /// 索引位的数据是否在显示范围内
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns></returns>
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