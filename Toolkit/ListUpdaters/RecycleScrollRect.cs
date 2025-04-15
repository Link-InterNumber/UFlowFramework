using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public sealed class RecycleScrollRect : MonoBehaviour, IEnumerable, IListUpdater
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
        
        public LayoutGroup layoutGroup;
        public ScrollRect scroll;
        public Mask maskObj;
        public RectTransform prefab;
        public ListDirection direction = ListDirection.HORIZONTAL;
        public float spacing;
        public bool optimize = true;
        
        private int _count = 1;
        public int count => _count;
        private RectTransform _container => scroll.content;
        private RectTransform _maskRT;
        private int _numVisible;
        private int _numBuffer = 1;
        public int numberBuffer
        {
            get => _numBuffer;
            set => _numBuffer = Math.Max(1, value);
        }
        // private float _containerHalfSize;
        private float _prefabSize;
        private RectOffset _padding;

        private Dictionary<int, RecycleItem> _itemDict = new Dictionary<int, RecycleItem>();
        private int _numItems = 0;
        private Vector2 _startPos;
        private Vector2 _offsetVec;
        private List<object> _dataList;

        public event OnItemInteraction onItemInteraction;

        public void ItemInteraction(IListItem item, object passData)
        {
            if(item == null) return;
            onItemInteraction?.Invoke(item, item.itemIndex, passData);
        }

        private void Awake()
        {
            layoutGroup.enabled = false;
            _padding = layoutGroup.padding;
            if (!scroll) return;
            if (optimize) scroll.onValueChanged.AddListener(ReorderItemsByPos);
            scroll.horizontal = direction == ListDirection.HORIZONTAL;
            scroll.vertical = direction == ListDirection.VERTICAL;
            _container.anchorMin = Vector2.up;
            _container.anchorMax = Vector2.up;
            _container.pivot = Vector2.up;
            var ContentSizeFitter = _container.GetComponent<ContentSizeFitter>();
            if(ContentSizeFitter) ContentSizeFitter.enabled = false;
        }
        
        private void OnDestroy()
        {
            if (!scroll) return;
            if (optimize) scroll.onValueChanged.RemoveListener(ReorderItemsByPos);
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
            Init();
            // ApplicationManager.instance.StartCoroutine(DelayInit());
        }

        private IEnumerator DelayInit()
        {
            yield return null;
            Init();
        }

        // Use this for initialization
        private void Init()
        {
            _maskRT = maskObj.GetComponent<RectTransform>(); 
            Vector2 prefabRectSize = prefab.rect.size;
            _prefabSize = (direction == ListDirection.HORIZONTAL ? prefabRectSize.x : prefabRectSize.y) + spacing;

            _container.sizeDelta = direction == ListDirection.HORIZONTAL
                ? (new Vector2(_prefabSize * _count - spacing + _padding.left + _padding.right, prefabRectSize.y + _padding.top * 2f))
                : (new Vector2(prefabRectSize.x + _padding.left * 2f, _prefabSize * _count - spacing + _padding.top + _padding.bottom));
            // _containerHalfSize = direction == ListDirection.HORIZONTAL
            //     ? (_container.rect.size.x * 0.5f)
            //     : (_container.rect.size.y * 0.5f);

            _numVisible = Mathf.CeilToInt((direction == ListDirection.HORIZONTAL ? _maskRT.rect.width : _maskRT.rect.height) / _prefabSize);

            _offsetVec = direction == ListDirection.HORIZONTAL ? Vector2.right : Vector2.down;
            _startPos = _offsetVec * (direction == ListDirection.HORIZONTAL ? _padding.left : _padding.top);
            _numItems = optimize ? Mathf.Min(_count, _numVisible + _numBuffer) : _count;
            var anchorValue = Vector2.up;
            for (int i = 0; i < _numItems; i++)
            {
                var obj = _container.transform.childCount > i
                    ? _container.transform.GetChild(i).gameObject
                    : Instantiate(prefab.gameObject, _container.transform);
                var t = obj.GetComponent<RectTransform>();
                t.anchorMax = anchorValue;
                t.anchorMin = anchorValue;
                t.pivot = anchorValue;
                t.anchoredPosition = _startPos + (_offsetVec * (i * _prefabSize));
                obj.SetActive(true);
                var li = obj.GetComponent<IListItem>();
                li.itemHolder = this;
                li.UpdateContent(i, _dataList[i]);
                _itemDict.Add(i, new RecycleItem{index = i, transform = t, listItem = li});
            }
            var removeNumber = _container.transform.childCount;
            if (_numItems < removeNumber)
            {
                var toDestroy = ListPool<GameObject>.Get();
                for (int i = _numItems; i < removeNumber; i++)
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
            // _container.anchoredPosition += _offsetVec * (_containerHalfSize - ((direction == ListDirection.HORIZONTAL ? _maskRT.rect.width : _maskRT.rect.height) * 0.5f));
            ApplicationManager.instance.StartCoroutine(DelayReorderItemsByPos());
        }
        
        private IEnumerator DelayReorderItemsByPos()
        {
            yield return null;
            ForceRebuild();
        }

        private void ReorderItemsByPos(Vector2 normVector)
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
                ? -_container.localPosition.x - _padding.left
                : _container.localPosition.y - _padding.top;
            passLength = Mathf.Clamp(passLength, 0f, (_count - _numVisible + 0.5f) * _prefabSize);
            var firstIndex = Mathf.Clamp(Mathf.FloorToInt(passLength / _prefabSize), 0, _dataList.Count - _numVisible);
            if (_previousIndex == firstIndex) return;
            var maxVisibleIndex = firstIndex + _numVisible - 1 + _numBuffer;
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
            for (var i = 0; i < removeKeys.Count; i++)
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
            item.transform.anchoredPosition = _startPos + (_offsetVec * (posIndex * _prefabSize));
            if(_dataList.Count - 1 < index) return; 
            item.listItem.itemHolder = this;
            item.listItem.UpdateContent(index, _dataList[index]);
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
            item.listItem.itemHolder = this;
            item.listItem.UpdateContent(index, data);
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

        public IEnumerator GetEnumerator()
        {
            yield return _dataList.GetEnumerator();
            ReorderItemsByPos(scroll.normalizedPosition);
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