using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridManager : MonoBehaviour
    {
        #region define

        public enum GridDirection
        {
            Horizontal = 0,
            Vertical = 1
        }
        
        private struct ListItemNRt
        {
            public int index;
            public Vector2Int pos;
            public RectTransform transform;
            public ListItem listItem;
        }        

        #endregion
        
        public GridLayoutGroup grid;
        public ScrollRect scroll;
        public Mask maskObj;
        public RectTransform prefab;
        public GridDirection direction = GridDirection.Horizontal;
        public bool optimize = true;
        
        private Vector2 _prefabSize;
        private Vector2 _startPos;

        private Dictionary<int, ListItemNRt> _itemDict = new Dictionary<int, ListItemNRt>();
        private List<object> _dataList;
        private RectTransform _container => scroll.content;
        private RectTransform _maskRT;
        private RectOffset _padding;
        private float _containerHalfSize;
        private int _count = 1;
        private int _numVisible;
        private int _lineCount;
        private int _itemCountInLine;
        private int _numItems = 0;
        private int _buffer = 1;
        
        public int count => _count;

        private void Awake()
        {
            grid.enabled = false;
            _padding = grid.padding;
            if(!grid) grid = GetComponent<GridLayoutGroup>();
            if (!scroll) return;
            if (optimize) scroll.onValueChanged.AddListener(ReorderItemsByPos);
            scroll.horizontal = direction == GridDirection.Horizontal;
            scroll.vertical = direction == GridDirection.Vertical;
            _container.anchorMin = Vector2.up;
            _container.anchorMax = Vector2.up;
            _container.pivot = Vector2.up;
        }
        
        private void OnDestroy()
        {
            if (!scroll) return;
            if (optimize) scroll.onValueChanged.RemoveListener(ReorderItemsByPos);
        }

        public void UpdateList(IList datas)
        {
            _count = datas.Count;
            _itemDict.Clear();
            if (_dataList == null) _dataList = new List<object>();
            else _dataList.Clear();
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
        
        private void Init()
        {
            // _container.anchoredPosition3D = new Vector3(0, 0, 0);
            _maskRT = maskObj.GetComponent<RectTransform>(); 
            _prefabSize = grid.cellSize + grid.spacing;
            var isHorizontal = direction == GridDirection.Horizontal;
            _lineCount = isHorizontal 
                ? Mathf.CeilToInt((_maskRT.rect.size.x - _padding.left - _padding.right) / _prefabSize.x)
                : Mathf.CeilToInt((_maskRT.rect.size.y - _padding.top - _padding.bottom) / _prefabSize.y);
            if (_lineCount == 0)
            {
                LinkLog.LogError($"GridManager the size of content is too small, GameObject name: {gameObject.name}");
                return;
            }
            _itemCountInLine = isHorizontal
                ? Mathf.FloorToInt((_maskRT.rect.size.y - _padding.top - _padding.bottom) / _prefabSize.y)
                : Mathf.FloorToInt((_maskRT.rect.size.x - _padding.left - _padding.right) / _prefabSize.x);

            _container.sizeDelta = isHorizontal
                ? (new Vector2(_prefabSize.x * Mathf.CeilToInt((float)_count / _itemCountInLine) - grid.spacing.x + _padding.left + _padding.right,
                    _prefabSize.y * _itemCountInLine +  + _padding.top + _padding.bottom - grid.spacing.y))
                : (new Vector2(_prefabSize.x * _itemCountInLine + + _padding.left + _padding.right - grid.spacing.x, 
                    _prefabSize.y * Mathf.CeilToInt((float)_count / _itemCountInLine) - grid.spacing.y + _padding.top + _padding.bottom));
            _containerHalfSize = isHorizontal
                ? (_container.rect.size.x * 0.5f)
                : (_container.rect.size.y * 0.5f);

            _numVisible = (_lineCount + _buffer + 1) * _itemCountInLine;
            _startPos = new Vector2(_padding.left, -1f * _padding.top);
            _numItems = optimize ? Mathf.Min(_count, _numVisible) : _count;
            var anchorValue = Vector2.up;
            for (int i = 0; i < _numItems; i++)
            {
                GameObject obj = transform.childCount > i
                    ? transform.GetChild(i).gameObject
                    : Instantiate(prefab.gameObject, _container.transform);
                var t = obj.GetComponent<RectTransform>();
                t.anchorMax = anchorValue;
                t.anchorMin = anchorValue;
                t.pivot = anchorValue;
                t.sizeDelta = grid.cellSize;
                obj.SetActive(true);
                var li = obj.GetComponent<ListItem>();
                if(li) li.UpdateContent(i, _dataList[i]);
                var temp = new ListItemNRt { index = i, transform = t, listItem = li };
                _itemDict.Add(i, temp);
                var pos = new Vector2Int
                {
                    x = isHorizontal
                        ? Mathf.FloorToInt(i * 1f / _itemCountInLine)
                        : i % _itemCountInLine,
                    y = isHorizontal
                        ? i % _itemCountInLine
                        : Mathf.FloorToInt(i * 1f / _itemCountInLine)
                };
                MoveItemByPos(temp, i, pos);
            }

            while (transform.childCount > _numItems)
            {
                GameObject.Destroy(transform.GetChild(_numItems).gameObject);
            }
            // _container.anchoredPosition = Vector2.zero;
            // ReorderItemsByPos(scroll.normalizedPosition);
            // ApplicationManager.instance.StartCoroutine(DelayReorderItemsByPos());
            ReorderItemsByPos(scroll.normalizedPosition);

        }

        private IEnumerator DelayReorderItemsByPos()
        {
            yield return null;
            ReorderItemsByPos(scroll.normalizedPosition);
        }
        
        private int _previousIndex;

        private void ReorderItemsByPos(Vector2 normVector)
        {
            var isHorizontal = direction == GridDirection.Horizontal;
            var prefabSize = isHorizontal ? _prefabSize.x : _prefabSize.y;
            var passLength = isHorizontal
                ? - _container.anchoredPosition.x - _padding.left
                : _container.anchoredPosition.y - _padding.top;
            passLength = Mathf.Clamp(passLength, 0f, isHorizontal ? _container.rect.width : _container.rect.height);
            var firstIndex = Mathf.Clamp( Mathf.FloorToInt(passLength / prefabSize) * _itemCountInLine, 0, _dataList.Count - _numVisible);
            if (_previousIndex == firstIndex) return;
            var previousMax = _previousIndex + _numVisible - 1;
            var maxVisibleIndex = firstIndex + _numVisible - 1;

            if (firstIndex > _previousIndex && firstIndex <= previousMax)
            {
                var indexStep = 1;
                for (int i = _previousIndex; i < firstIndex; i++)
                {
                    if (!_itemDict.TryGetValue(i, out var item)) continue;
                    var newIndex = previousMax + indexStep;
                    var pos = new Vector2Int
                    {
                        x = isHorizontal
                            ? Mathf.FloorToInt(newIndex * 1f / _itemCountInLine)
                            : newIndex % _itemCountInLine,
                        y = isHorizontal
                            ? newIndex % _itemCountInLine
                            : Mathf.FloorToInt(newIndex * 1f / _itemCountInLine)
                    };
                    MoveItemByPos(item, newIndex, pos);
                    item.index = newIndex;
                    _itemDict.Remove(i);
                    _itemDict[newIndex] = item;
                    indexStep++;
                }
            }
            else if (firstIndex < _previousIndex && maxVisibleIndex >= _previousIndex)
            {
                var indexStep = 1;
                for (int i = previousMax; i > maxVisibleIndex; i--)
                {
                    if (!_itemDict.TryGetValue(i, out var item)) continue;
                    var newIndex = _previousIndex - indexStep;
                    var pos = new Vector2Int
                    {
                        x = isHorizontal
                            ? Mathf.FloorToInt(newIndex * 1f / _itemCountInLine)
                            : newIndex % _itemCountInLine,
                        y = isHorizontal
                            ? newIndex % _itemCountInLine
                            : Mathf.FloorToInt(newIndex * 1f / _itemCountInLine)
                    };
                    MoveItemByPos(item, newIndex, pos);
                    item.index = newIndex;
                    _itemDict.Remove(i);
                    _itemDict[newIndex] = item;
                    indexStep++;
                }
            }
            else
            {
                var keys = _itemDict.Keys.ToList();
                for (var i = 0; i < keys.Count; i++)
                {
                    var oldIndex = keys[i];
                    var item = _itemDict[oldIndex];
                    var newIndex = firstIndex + i;
                    var pos = new Vector2Int
                    {
                        x = isHorizontal
                            ? Mathf.FloorToInt(newIndex * 1f / _itemCountInLine)
                            : newIndex % _itemCountInLine,
                        y = isHorizontal
                            ? newIndex % _itemCountInLine
                            : Mathf.FloorToInt(newIndex * 1f / _itemCountInLine)
                    };
                    MoveItemByPos(item, newIndex, pos);
                    item.index = newIndex;
                    _itemDict.Remove(oldIndex);
                    _itemDict[newIndex] = item;
                }
            }
            _previousIndex = firstIndex;
        }
        
        private void MoveItemByPos(ListItemNRt item, int index, Vector2Int pos)
        {
            item.transform.anchoredPosition = _startPos + Vector2.right * (pos.x * _prefabSize.x) + Vector2.down * (pos.y * _prefabSize.y);
            if(_dataList.Count - 1 >= index && item.listItem) item.listItem.UpdateContent(index, _dataList[index]);
        }
        
        public object GetData(int index)
        {
            if (_dataList.Count - 1 >= index && index >= 0) return _dataList[index];
            return null;
        }

        public void ReplaceData(int index, object data)
        {
            if (index > _dataList.Count - 1 || index < 0) return;
            _dataList[index] = data;
            if (!_itemDict.TryGetValue(index, out var item)) return;
            item.listItem.UpdateContent(index, data);
        }
        
        public IEnumerator GetEnumerator()
        {
            yield return _dataList.GetEnumerator();
            ReorderItemsByPos(scroll.normalizedPosition);
        }

        public object this[int index]
        {
            get => _dataList[index];
            set => ReplaceData(index, value);
        }

        public bool IsDataInVisible(int index, out ListItem item)
        {
            if (!_itemDict.TryGetValue(index, out var listItem))
            {
                item = listItem.listItem;
                return false;
            }
            item = null;
            return true;
        }
    }
}