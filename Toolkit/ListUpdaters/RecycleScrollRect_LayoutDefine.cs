using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public interface IRecycleScrollRectLayout
    {
        public int visibleNum { get; }
        
        public void InitScroll(ScrollRect scroll);

        public void CalVisibleNum(Vector2 viewPortSize, int buffer, out int bufferedNum);

        public Vector2 GetContainerSize(int count);

        public Vector2 GetItemLocalPos(int index);

        public void GetViewIndexRange(float passLength, int buffer, int count, ref int min, ref int max);
    }

    public partial class RecycleScrollRect
    {
        private class RSHorizontalLayout : IRecycleScrollRectLayout
        {
            private RectOffset _padding;
            private Vector2 _space;
            private Vector2 _prefabSize;
            private int _visibleNum;
            public int visibleNum => _visibleNum;

            public RSHorizontalLayout(Vector2 prefabSize, RectOffset padding, Vector2 space)
            {
                _prefabSize = prefabSize;
                _padding = padding;
                _space = space;
            }

            public void InitScroll(ScrollRect scrollRect)
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
            }

            public void CalVisibleNum(Vector2 viewPortSize, int buffer, out int bufferedNum)
            {
                _visibleNum = Mathf.CeilToInt(viewPortSize.x  / (_prefabSize.x + _space.x));
                bufferedNum = _visibleNum + buffer;
            }

            public Vector2 GetContainerSize(int count)
            {
                var w = (_prefabSize.x + _space.x) * count - _space.x + _padding.left + _padding.right;
                var h = _prefabSize.y + _padding.top + _padding.bottom;
                return new Vector2(w, h);
            }

            public Vector2 GetItemLocalPos(int index)
            {
                var startPos = new Vector2(_padding.left, - _padding.top);
                var x = startPos.x + (_prefabSize.x + _space.x) * index;
                var y = startPos.y;
                return new Vector2(x, y);
            }

            public void GetViewIndexRange(float passLength, int buffer, int count, ref int min, ref int max)
            {
                var passLengthAdapted = passLength - _padding.left;
                min = Mathf.Clamp(Mathf.FloorToInt(passLengthAdapted / (_prefabSize.x + _space.x)), 0, count - _visibleNum);
                max = Mathf.Min(count -1, min + _visibleNum - 1 + buffer);
            }
        }

        private class RSVerticalLayout : IRecycleScrollRectLayout
        {
            private RectOffset _padding;
            private Vector2 _space;
            private Vector2 _prefabSize;
            private int _visibleNum;
            public int visibleNum => _visibleNum;

            public RSVerticalLayout(Vector2 prefabSize, RectOffset padding, Vector2 space)
            {
                _prefabSize = prefabSize;
                _padding = padding;
                _space = space;
            }

            public void InitScroll(ScrollRect scrollRect)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
            
            public void CalVisibleNum(Vector2 viewPortSize, int buffer, out int bufferedNum)
            {
                _visibleNum = Mathf.CeilToInt(viewPortSize.y  / (_prefabSize.y + _space.y));
                bufferedNum = _visibleNum + buffer;
            }
            
            public Vector2 GetContainerSize(int count)
            {
                var w = _prefabSize.x + _padding.left + _padding.right;
                var h = (_prefabSize.y + _space.y) * count - _space.y + _padding.top + _padding.bottom;
                return new Vector2(w, h);
            }

            public Vector2 GetItemLocalPos(int index)
            {
                var startPos = new Vector2(_padding.left, - _padding.top);
                var x = startPos.x;
                var y = startPos.y + -1f * ((_prefabSize.y + _space.y) * index);
                return new Vector2(x, y);
            }
            
            public void GetViewIndexRange(float passLength, int buffer, int count, ref int min, ref int max)
            {
                var passLengthAdapted = passLength - _padding.left;
                min = Mathf.Clamp(Mathf.FloorToInt(passLengthAdapted / (_prefabSize.y + _space.y)), 0, count - _visibleNum);
                max = Mathf.Min(count -1, min + _visibleNum - 1 + buffer);
            }
        }

        private class RSGridLayout : IRecycleScrollRectLayout
        {
            private RectOffset _padding;
            private Vector2 _space;
            private Vector2 _prefabSize;
            private GridLayoutGroup.Axis _startAxis;
            private int _numPerLine;
            private int _visibleNum;
            public int visibleNum => _visibleNum;

            public RSGridLayout(GridLayoutGroup.Axis startAxis, Vector2 prefabSize, RectOffset padding, Vector2 space)
            {
                _startAxis = startAxis;
                _prefabSize = prefabSize;
                _padding = padding;
                _space = space;
                _numPerLine = 1;
            }

            public void InitScroll(ScrollRect scrollRect)
            {
                if (_startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    _numPerLine = Mathf.FloorToInt((scrollRect.viewport.rect.size.x - _padding.left - _padding.right + _space.x) / (_prefabSize.x + _space.x));
                }
                else if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    scrollRect.horizontal = true;
                    scrollRect.vertical = false;
                    _numPerLine = Mathf.FloorToInt((scrollRect.viewport.rect.size.y - _padding.top - _padding.bottom + _space.y) / (_prefabSize.y + _space.y));
                }
                _numPerLine = Mathf.Max(1, _numPerLine);
            }
            
            public void CalVisibleNum(Vector2 viewPortSize, int buffer, out int bufferedNum)
            {
                _visibleNum = _numPerLine;
                if (_startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    var lineCount = Mathf.CeilToInt(viewPortSize.y  / (_prefabSize.y + _space.y));
                    _visibleNum = lineCount * _numPerLine;
                }
                if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    var lineCount = Mathf.CeilToInt(viewPortSize.x  / (_prefabSize.x + _space.x));
                    _visibleNum = lineCount * _numPerLine;
                }
                bufferedNum = _visibleNum + buffer * _numPerLine;
            }
            
            public Vector2 GetContainerSize(int count)
            {
                if (_startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    var w = (_prefabSize.x + _space.x) * _numPerLine - _space.x + _padding.left + _padding.right;
                    var h = (_prefabSize.y + _space.y) * Mathf.CeilToInt(count * 1f / _numPerLine) - _space.y + _padding.top + _padding.bottom;
                    return new Vector2(w, h);
                }
                if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    var w = (_prefabSize.x + _space.x) * Mathf.CeilToInt(count * 1f / _numPerLine) - _space.x + _padding.left + _padding.right;
                    var h = (_prefabSize.y + _space.y) * _numPerLine - _space.y + _padding.top + _padding.bottom;
                    return new Vector2(w, h);
                }
                return Vector2.zero;
            }

            public Vector2 GetItemLocalPos(int index)
            {
                var startPos = new Vector2(_padding.left, - _padding.top);

                if (_startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    var x = startPos.x + (_prefabSize.x + _space.x) * (index % _numPerLine);
                    var y = startPos.y + -1f * ((_prefabSize.y + _space.y) * Mathf.FloorToInt((index + 0.5f) / _numPerLine));
                    return new Vector2(x, y);
                }
                if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    var x = startPos.x + ((_prefabSize.x + _space.x) * Mathf.FloorToInt((index + 0.5f) / _numPerLine));
                    var y = startPos.y + -1f * (_prefabSize.y + _space.y) * (index % _numPerLine);
                    return new Vector2(x, y);
                }
                return Vector2.zero;
            }
            
            public void GetViewIndexRange(float passLength, int buffer, int count, ref int min, ref int max)
            {
                var adaptedCount = Mathf.Max(count, 1);
                var indexMax = Mathf.CeilToInt(adaptedCount - 0.5f / _numPerLine) * _numPerLine - _visibleNum;
                if (_startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    var passLengthAdapted = passLength - _padding.top;
                    min = Mathf.Clamp(Mathf.FloorToInt(passLengthAdapted / (_prefabSize.y + _space.y)) * _numPerLine, 0, indexMax);
                    max = Mathf.Min(min + _visibleNum - 1 + buffer * _numPerLine, count - 1);
                    return;
                }
                if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    var passLengthAdapted = passLength - _padding.left;
                    min = Mathf.Clamp(Mathf.FloorToInt(passLengthAdapted / (_prefabSize.x + _space.x)) * _numPerLine, 0, indexMax);
                    max = Mathf.Min(min + _visibleNum - 1 + buffer * _numPerLine, count - 1);
                }
            }
        }
    }
}