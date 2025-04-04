using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    [RequireComponent(typeof(RectTransform))]

    public class AnimationRectLayout : UIBehaviour
    {
        public enum LayoutType
        {
            Horizontal,
            Vertical,
        }
        
        public LayoutType layoutType;
        private List<RectTransform> _children = new List<RectTransform>();
        private List<Vector2> _targetPos = new List<Vector2>();
        private bool _isDirty = false;

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
                if (_children[i].anchoredPosition.ManhattanDistance(_targetPos[i]) < 5f)
                {
                    _children[i].anchoredPosition = _targetPos[i];
                    continue;
                }
                isAllArrived = false;
                _children[i].anchoredPosition = Vector2.Lerp(_children[i].anchoredPosition, _targetPos[i], Time.deltaTime * 10f);
            }
            _isDirty = !isAllArrived;
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
            _children.Sort((a, b) =>
            {
                if (layoutType == LayoutType.Horizontal)
                {
                    return a.anchoredPosition.x.CompareTo(b.anchoredPosition.x);
                }
                return a.anchoredPosition.y.CompareTo(b.anchoredPosition.y);
            });

            var totalSize = new Vector2();
            foreach (var child in _children)
            {
                totalSize += child.sizeDelta;
            }

            // 将_children按照从左到右放置
            var startPos = new Vector2(-totalSize.x / 2, 0f);
            for (var i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                var targetPos = startPos;
                if (layoutType == LayoutType.Horizontal)
                {
                    targetPos.x += child.sizeDelta.x / 2;
                    startPos.x += child.sizeDelta.x;
                }
                else
                {
                    targetPos.y += child.sizeDelta.y / 2;
                    startPos.y += child.sizeDelta.y;
                }

                _targetPos.Add(targetPos);
            }
        }
    }
}