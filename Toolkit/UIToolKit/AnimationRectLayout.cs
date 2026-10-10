using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonoComponent
{
    [RequireComponent(typeof(RectTransform))]
    public class AnimationRectLayout : UIBehaviour
    {
        public enum LayoutType
        {
            Horizontal,
            Vertical,
        }

        // 对齐方式
        public enum Alignment
        {
            Start,
            Center,
            End,
        }

        public float space = 0f;
        public LayoutType layoutType;
        public Alignment alignment;
        private List<RectTransform> _children = new List<RectTransform>();
        private List<Vector2> _targetPos = new List<Vector2>();
        private bool _isDirty = true;
        
        private static float ManhattanDistance(Vector2 v2, Vector2 target)
        {
            return Mathf.Abs(v2.x - target.x) + Mathf.Abs(v2.y - target.y);
        }


        private void Update()
        {
            if (_children.Count != transform.childCount)
            {
                _isDirty = true;
                CollectChildren();
            }
            if(!_isDirty) return;
            var isAllArrived = true;
            for (var i = 0; i < _children.Count; i++)
            {
                if (ManhattanDistance(_children[i].anchoredPosition, _targetPos[i]) < 5f)
                {
                    _children[i].anchoredPosition = _targetPos[i];
                    continue;
                }
                isAllArrived = false;
                _children[i].anchoredPosition = Vector2.Lerp(_children[i].anchoredPosition, _targetPos[i], Time.deltaTime * 10f);
            }
            _isDirty = !isAllArrived;
        }

        public void ForceUpdate()
        {
            _isDirty = true;
            CollectChildren();
        }

        private void CollectChildren()
        {
            _children.Clear();
            _targetPos.Clear();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child == null) continue;
                _children.Add(child);
            }
            
            // 将_children按照现有位置排序
            // _children.Sort((a, b) =>
            // {
            //     if (layoutType == LayoutType.Horizontal)
            //     {
            //         return a.anchoredPosition.x.CompareTo(b.anchoredPosition.x);
            //     }
            //     return a.anchoredPosition.y.CompareTo(b.anchoredPosition.y);
            // });

            var totalSize = new Vector2();
            foreach (var child in _children)
            {
                totalSize += child.sizeDelta;
            }

            var childCount = _children.Count;
            var spaceV2 = new Vector2(space * (childCount - 1), space * (childCount - 1));
            totalSize += spaceV2;
            
            // 将_children按照从左到右放置
            var startPos = Vector2.zero;
            if (layoutType == LayoutType.Horizontal)
            {
                var rectTransform = transform as RectTransform;
                switch (alignment)
                {
                    case Alignment.Start:
                        startPos = new Vector2(-rectTransform.rect.width / 2, 0f);
                        break;
                    case Alignment.Center:
                        startPos = new Vector2(-totalSize.x / 2, 0f);
                        break;
                    case Alignment.End:
                        startPos = new Vector2(rectTransform.rect.width / 2 - totalSize.x, 0f);
                        break;
                }
            }
            else
            {
                var rectTransform = transform as RectTransform;
                switch (alignment)
                {
                    case Alignment.Start:
                        startPos = new Vector2(0f, rectTransform.rect.height / 2);
                        break;
                    case Alignment.Center:
                        startPos = new Vector2(0f, totalSize.y / 2);
                        break;
                    case Alignment.End:
                        startPos = new Vector2(0f, -rectTransform.rect.height / 2 + totalSize.y);
                        break;
                }
            }
            
            for (var i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                var targetPos = startPos;
                if (layoutType == LayoutType.Horizontal)
                {
                    targetPos.x += child.sizeDelta.x * child.pivot.x;
                    startPos.x += child.sizeDelta.x + space;
                }
                else
                {
                    targetPos.y -= child.sizeDelta.y * (1f - child.pivot.y);
                    startPos.y -= child.sizeDelta.y + space;
                }

                _targetPos.Add(targetPos);
            }
        }
    }
}