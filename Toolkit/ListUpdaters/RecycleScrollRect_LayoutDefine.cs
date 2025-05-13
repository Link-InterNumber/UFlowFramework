using System;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public interface IRecycleScrollRectLayout
    {
        public void InitScroll(ScrollRect scroll);

        public Vector2 GetContainerSize(RectTransform content, int count);

        public Vector2 GetItemLocalPos(int index);
    }

    public partial class RecycleScrollRect
    {
        private struct HorizontalLayout : IRecycleScrollRectLayout
        {
            private RectOffset _padding;
            private Vector2 _space;
            private Vector2 _prefabSize;

            public HorizontalLayout(Vector2 prefabSize, RectOffset padding, Vector2 space)
            {
                _prefabSize = prefabSize;
                _padding = padding;
                _space = space;
            }

            public void InitScroll(ScrollRect scrollRect)
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
                scrollRect.content.anchorMin = Vector2.up;
                scrollRect.content.anchorMax = Vector2.up;
                scrollRect.content.pivot = Vector2.up;
                var contentSizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter) contentSizeFitter.enabled = false;
            }

            public Vector2 GetContainerSize(RectTransform content, int count)
            {
                var w = (_prefabSize.x + _space.x) * count - _space.x + _padding.left + _padding.right;
                var h = _prefabSize.y + _padding.top + _padding.bottom;
                return new Vector2(w, h);
            }

            public Vector2 GetItemLocalPos(int index)
            {
                var startPos = new Vector2(_padding.left, - _padding.top);
                var x = startPos.x + Vector2.left * ((_prefabSize.x + _space.x) * index);
                var y = startPos.y;
                return new Vector2(x, y);
            }
        }

        private struct VerticalLayout : IRecycleScrollRectLayout
        {
            private RectOffset _padding;
            private Vector2 _space;
            private Vector2 _prefabSize;

            public HorizontalLayout(Vector2 prefabSize, RectOffset padding, Vector2 space)
            {
                _prefabSize = prefabSize;
                _padding = padding;
                _space = space;
            }

            public void InitScroll(ScrollRect scrollRect)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.content.anchorMin = Vector2.up;
                scrollRect.content.anchorMax = Vector2.up;
                scrollRect.content.pivot = Vector2.up;
                var contentSizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter) contentSizeFitter.enabled = false;
            }

            public Vector2 GetContainerSize(RectTransform content, int count)
            {
                var w = prefabRectSize.x + _padding.left + _padding.right;
                var h = (_prefabSize.y + spacing.y) * count - spacing.y + _padding.top + _padding.bottom;
                return new Vector2(w, h);
            }

            public Vector2 GetItemLocalPos(int index)
            {
                var startPos = new Vector2(_padding.left, - _padding.top);
                var x = startPos.x;
                var y = startPos.y + Vector2.down * ((_prefabSize.y + _space.y) * index);
                return new Vector2(x, y);
            }
        }

        private struct GridLayout : IRecycleScrollRectLayout
        {
            private RectOffset _padding;
            private Vector2 _space;
            private Vector2 _prefabSize;
            private GridLayoutGroup.Axis _startAxis;
            private int _numPerLine;

            public GridLayout(GridLayoutGroup.Axis startAxis, Vector2 prefabSize, RectOffset padding, Vector2 space)
            {
                __startAxis = _startAxis;
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
                    _numPerLine = Mathf.FloorToInt((scrollRect.Viewport.rect.size.x - _padding.left - _padding.right + _space.x) / (_prefabSize.x + _space.x));
                }
                else if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    scrollRect.horizontal = true;
                    scrollRect.vertical = false;
                    _numPerLine = Mathf.FloorToInt((scrollRect.Viewport.rect.size.y - _padding.top - _padding.bottom + _space.y) / (_prefabSize.y + _space.y));
                }
                _numPerLine = Mathf.Max(1, _numPerLine);
                scrollRect.content.anchorMin = Vector2.up;
                scrollRect.content.anchorMax = Vector2.up;
                scrollRect.content.pivot = Vector2.up;
                var contentSizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter) contentSizeFitter.enabled = false;
            }

            public Vector2 GetContainerSize(RectTransform content, int count)
            {
                if (_startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    var w = (prefabRectSize.x + _space.x) * _numPerLine - _space.x + _padding.left + _padding.right;
                    var h = (prefabRectSize.y + _space.y) * Mathf.CeilToInt(count * 1f / _numPerLine) - _space.y + _padding.top + _padding.bottom;
                    return new Vector2(w, h);
                }
                else if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    var w = (prefabRectSize.x + _space.x) * Mathf.CeilToInt(count * 1f / _numPerLine) - _space.x + _padding.left + _padding.right;
                    var h = (prefabRectSize.y + _space.y) * _numPerLine - _space.y + _padding.top + _padding.bottom;
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
                else if (_startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    var x = startPos.x + ((_prefabSize.x + _space.x) * Mathf.FloorToInt((index + 0.5f) / _numPerLine));
                    var y = startPos.y + -1f * (_prefabSize.y + _space.y) * (index % _numPerLine);
                    return new Vector2(x, y);
                }
                return Vector2.zero;
            }
        }
    }
}