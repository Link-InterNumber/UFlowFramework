using System;
using System.Collections.Generic;
using UnityEngine;
using UFlowFramework.DataStructure;

namespace PowerCellStudio
{
    public static class TreeLayoutUtility
    {
        public enum LayoutDirection
        {
            Horizontal,
            Vertical
        }

        public struct LayoutSettings
        {
            public float horizontalSpacing;
            public float verticalSpacing;
            public Vector2 startOffset;
            public Vector2 defaultNodeSize;
            public LayoutDirection Direction;

            /// <summary>
            /// 返回默认的树布局参数。
            /// Returns the default settings for tree layout.
            /// </summary>
            public static LayoutSettings Default => new LayoutSettings
            {
                horizontalSpacing = 220f,
                verticalSpacing = 80f,
                startOffset = new Vector2(100f, 100f),
                defaultNodeSize = new Vector2(180f, 120f),
                Direction = LayoutDirection.Horizontal
            };
        }

        /// <summary>
        /// 计算树布局并将结果写回到树节点的位置属性。
        /// Calculates a tree layout and writes the result back to each tree node position.
        /// </summary>
        /// <typeparam name="TNode">节点类型。The node type.</typeparam>
        /// <param name="node">树的根节点。The root node of the tree.</param>
        /// <param name="settings">可选的布局参数。Optional layout settings.</param>
        /// <returns>树布局占用的总区域。The total area occupied by the tree layout.</returns>
        public static Rect CalculateLayout<TNode>(
            TreeNode<TNode> node,
            LayoutSettings? settings = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var resolvedSettings = settings ?? LayoutSettings.Default;
            if (resolvedSettings.Direction == LayoutDirection.Horizontal)
            {
                return CalculateHorizontalLayout(node, resolvedSettings);
            }

            return CalculateVerticalLayout(node, resolvedSettings);
        }

        private static Rect CalculateHorizontalLayout<TNode>(
            TreeNode<TNode> node,
            LayoutSettings? settings = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var resolvedSettings = settings ?? LayoutSettings.Default;
            var layoutRoot = node.Root;
            var subtreeHeights = new Dictionary<TreeNode<TNode>, float>();

            float MeasureSubtree(TreeNode<TNode> node, HashSet<TreeNode<TNode>> visiting)
            {
                if (subtreeHeights.TryGetValue(node, out var cachedHeight))
                {
                    return cachedHeight;
                }

                if (!visiting.Add(node))
                {
                    var cycleHeight = resolvedSettings.defaultNodeSize.y;
                    subtreeHeights[node] = cycleHeight;
                    return cycleHeight;
                }

                float childrenHeight = 0f;
                for (int i = 0; i < node.Child.Count; i++)
                {
                    childrenHeight += MeasureSubtree(node.Child[i], visiting);
                }

                if (node.Child.Count > 1)
                {
                    childrenHeight += resolvedSettings.verticalSpacing * (node.Child.Count - 1);
                }

                var subtreeHeight = Mathf.Max(resolvedSettings.defaultNodeSize.y, childrenHeight);
                subtreeHeights[node] = subtreeHeight;
                visiting.Remove(node);
                return subtreeHeight;
            }

            void LayoutSubtree(TreeNode<TNode> node, int depth, float top, HashSet<TreeNode<TNode>> visiting)
            {
                if (!visiting.Add(node))
                {
                    return;
                }

                var size = resolvedSettings.defaultNodeSize;
                var subtreeHeight = subtreeHeights[node];
                var centerY = top + subtreeHeight * 0.5f;
                var position = new Vector2(
                    resolvedSettings.startOffset.x + depth * resolvedSettings.horizontalSpacing,
                    centerY - size.y * 0.5f);
                node.Position = new Vector2(position.x, position.y);

                if (node.Child.Count > 0)
                {
                    float childrenHeight = 0f;
                    for (int i = 0; i < node.Child.Count; i++)
                    {
                        childrenHeight += subtreeHeights[node.Child[i]];
                    }

                    if (node.Child.Count > 1)
                    {
                        childrenHeight += resolvedSettings.verticalSpacing * (node.Child.Count - 1);
                    }

                    var childTop = top + (subtreeHeight - childrenHeight) * 0.5f;
                    for (int i = 0; i < node.Child.Count; i++)
                    {
                        var child = node.Child[i];
                        LayoutSubtree(child, depth + 1, childTop, visiting);
                        childTop += subtreeHeights[child] + resolvedSettings.verticalSpacing;
                    }
                }

                visiting.Remove(node);
            }

            MeasureSubtree(layoutRoot, new HashSet<TreeNode<TNode>>());
            LayoutSubtree(layoutRoot, 0, resolvedSettings.startOffset.y, new HashSet<TreeNode<TNode>>());

            return CalculateBounds(layoutRoot, resolvedSettings.defaultNodeSize, new HashSet<TreeNode<TNode>>());
        }

        private static Rect CalculateVerticalLayout<TNode>(
            TreeNode<TNode> node,
            LayoutSettings? settings = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var resolvedSettings = settings ?? LayoutSettings.Default;
            var layoutRoot = node.Root;
            var subtreeWidths = new Dictionary<TreeNode<TNode>, float>();

            float MeasureSubtree(TreeNode<TNode> current, HashSet<TreeNode<TNode>> visiting)
            {
                if (subtreeWidths.TryGetValue(current, out var cachedWidth))
                {
                    return cachedWidth;
                }

                if (!visiting.Add(current))
                {
                    var cycleWidth = resolvedSettings.defaultNodeSize.x;
                    subtreeWidths[current] = cycleWidth;
                    return cycleWidth;
                }

                float childrenWidth = 0f;
                for (int i = 0; i < current.Child.Count; i++)
                {
                    childrenWidth += MeasureSubtree(current.Child[i], visiting);
                }

                if (current.Child.Count > 1)
                {
                    childrenWidth += resolvedSettings.horizontalSpacing * (current.Child.Count - 1);
                }

                var subtreeWidth = Mathf.Max(resolvedSettings.defaultNodeSize.x, childrenWidth);
                subtreeWidths[current] = subtreeWidth;
                visiting.Remove(current);
                return subtreeWidth;
            }

            void LayoutSubtree(TreeNode<TNode> current, int depth, float left, HashSet<TreeNode<TNode>> visiting)
            {
                if (!visiting.Add(current))
                {
                    return;
                }

                var size = resolvedSettings.defaultNodeSize;
                var subtreeWidth = subtreeWidths[current];
                var centerX = left + subtreeWidth * 0.5f;
                var position = new Vector2(
                    centerX - size.x * 0.5f,
                    resolvedSettings.startOffset.y + depth * resolvedSettings.verticalSpacing);
                current.Position = position;

                if (current.Child.Count > 0)
                {
                    float childrenWidth = 0f;
                    for (int i = 0; i < current.Child.Count; i++)
                    {
                        childrenWidth += subtreeWidths[current.Child[i]];
                    }

                    if (current.Child.Count > 1)
                    {
                        childrenWidth += resolvedSettings.horizontalSpacing * (current.Child.Count - 1);
                    }

                    var childLeft = left + (subtreeWidth - childrenWidth) * 0.5f;
                    for (int i = 0; i < current.Child.Count; i++)
                    {
                        var child = current.Child[i];
                        LayoutSubtree(child, depth + 1, childLeft, visiting);
                        childLeft += subtreeWidths[child] + resolvedSettings.horizontalSpacing;
                    }
                }

                visiting.Remove(current);
            }

            MeasureSubtree(layoutRoot, new HashSet<TreeNode<TNode>>());
            LayoutSubtree(
                layoutRoot,
                0,
                resolvedSettings.startOffset.x,
                new HashSet<TreeNode<TNode>>());

            return CalculateBounds(layoutRoot, resolvedSettings.defaultNodeSize, new HashSet<TreeNode<TNode>>());
        }

        private static Rect CalculateBounds<TNode>(
            TreeNode<TNode> node,
            Vector2 nodeSize,
            HashSet<TreeNode<TNode>> visiting)
        {
            if (node == null || !visiting.Add(node))
            {
                return new Rect();
            }

            var minX = node.Position.x;
            var minY = node.Position.y;
            var maxX = node.Position.x + nodeSize.x;
            var maxY = node.Position.y + nodeSize.y;

            for (int i = 0; i < node.Child.Count; i++)
            {
                var childBounds = CalculateBounds(node.Child[i], nodeSize, visiting);
                minX = Mathf.Min(minX, childBounds.xMin);
                minY = Mathf.Min(minY, childBounds.yMin);
                maxX = Mathf.Max(maxX, childBounds.xMax);
                maxY = Mathf.Max(maxY, childBounds.yMax);
            }

            visiting.Remove(node);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}