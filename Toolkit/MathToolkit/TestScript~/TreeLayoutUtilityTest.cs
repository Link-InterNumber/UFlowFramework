using UFlowFramework.DataStructure;
using UnityEngine;

namespace PowerCellStudio
{
    public class TreeLayoutUtilityTest : RunTestMono
    {
        private const float Tolerance = 0.001f;

        [Header("Gizmos Preview")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private TreeLayoutUtility.LayoutDirection _previewDirection = TreeLayoutUtility.LayoutDirection.Horizontal;
        [SerializeField] private Vector2 _previewStartOffset = new Vector2(0f, 0f);
        [SerializeField] private Vector2 _previewNodeSize = new Vector2(100f, 50f);
        [SerializeField] private float _previewHorizontalSpacing = 120f;
        [SerializeField] private float _previewVerticalSpacing = 90f;
        [SerializeField] private Vector3 _previewOrigin = Vector3.zero;
        [SerializeField] private Color _nodeColor = new Color(0.2f, 0.85f, 0.65f, 1f);
        [SerializeField] private Color _lineColor = new Color(1f, 0.7f, 0.2f, 1f);

        private void Start()
        {
            RunAllTests();
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos)
            {
                return;
            }

            var tree = CreateSampleTree();
            var settings = new TreeLayoutUtility.LayoutSettings
            {
                horizontalSpacing = _previewHorizontalSpacing,
                verticalSpacing = _previewVerticalSpacing,
                startOffset = _previewStartOffset,
                defaultNodeSize = _previewNodeSize,
                Direction = _previewDirection
            };

            TreeLayoutUtility.CalculateLayout(tree, settings);
            DrawTreeGizmos(tree, settings.defaultNodeSize);
        }

        [ContextMenu("Run TreeLayoutUtility Tests")]
        public void RunAllTests()
        {
            RunTest("TreeLayoutUtility Horizontal Layout", TestHorizontalLayout);
            RunTest("TreeLayoutUtility Vertical Layout", TestVerticalLayout);
            RunTest("TreeLayoutUtility Starts From Root", TestLayoutStartsFromRoot);
        }

        private void TestHorizontalLayout()
        {
            var tree = CreateSampleTree();
            var settings = new TreeLayoutUtility.LayoutSettings
            {
                horizontalSpacing = 120f,
                verticalSpacing = 40f,
                startOffset = new Vector2(10f, 20f),
                defaultNodeSize = new Vector2(100f, 50f),
                Direction = TreeLayoutUtility.LayoutDirection.Horizontal
            };

            TreeLayoutUtility.CalculateLayout(tree.Root, settings);

            var root = tree.Root;
            var left = root.Child[0];
            var right = root.Child[1];
            var leaf = left.Child[0];

            AssertVector2(root.Position, new Vector2(10f, 65f), "Horizontal root position");
            AssertVector2(left.Position, new Vector2(130f, 20f), "Horizontal left child position");
            AssertVector2(right.Position, new Vector2(130f, 110f), "Horizontal right child position");
            AssertVector2(leaf.Position, new Vector2(250f, 20f), "Horizontal grandchild position");
        }

        private void TestVerticalLayout()
        {
            var tree = CreateSampleTree();
            var settings = new TreeLayoutUtility.LayoutSettings
            {
                horizontalSpacing = 40f,
                verticalSpacing = 90f,
                startOffset = new Vector2(10f, 20f),
                defaultNodeSize = new Vector2(100f, 50f),
                Direction = TreeLayoutUtility.LayoutDirection.Vertical
            };

            TreeLayoutUtility.CalculateLayout(tree.Root, settings);

            var root = tree.Root;
            var left = root.Child[0];
            var right = root.Child[1];
            var leaf = left.Child[0];

            AssertVector2(root.Position, new Vector2(80f, 20f), "Vertical root position");
            AssertVector2(left.Position, new Vector2(10f, 110f), "Vertical left child position");
            AssertVector2(right.Position, new Vector2(150f, 110f), "Vertical right child position");
            AssertVector2(leaf.Position, new Vector2(10f, 200f), "Vertical grandchild position");
        }

        private void TestLayoutStartsFromRoot()
        {
            var tree = CreateSampleTree();
            var root = tree.Root;
            var left = root.Child[0];
            var leaf = left.Child[0];
            var settings = new TreeLayoutUtility.LayoutSettings
            {
                horizontalSpacing = 120f,
                verticalSpacing = 40f,
                startOffset = new Vector2(10f, 20f),
                defaultNodeSize = new Vector2(100f, 50f),
                Direction = TreeLayoutUtility.LayoutDirection.Horizontal
            };

            TreeLayoutUtility.CalculateLayout(leaf, settings);

            AssertVector2(root.Position, new Vector2(10f, 65f), "Layout should begin from root even when a descendant is passed in");
        }

        private TreeNode<string> CreateSampleTree()
        {
            var root = new TreeNode<string>("Root");
            var left = new TreeNode<string>("Left");
            var right = new TreeNode<string>("Right");
            var leaf = new TreeNode<string>("Leaf");

            root.AddChild(left);
            root.AddChild(right);
            left.AddChild(leaf);
            return root;
        }

        private void DrawTreeGizmos(TreeNode<string> node, Vector2 nodeSize)
        {
            if (node == null)
            {
                return;
            }

            var nodeCenter = ToWorldCenter(node.Position, nodeSize);
            Gizmos.color = _nodeColor;
            Gizmos.DrawWireCube(nodeCenter, new Vector3(nodeSize.x, nodeSize.y, 0f));

            Gizmos.color = _lineColor;
            for (int i = 0; i < node.Child.Count; i++)
            {
                var child = node.Child[i];
                var childCenter = ToWorldCenter(child.Position, nodeSize);
                Gizmos.DrawLine(nodeCenter, childCenter);
                DrawTreeGizmos(child, nodeSize);
            }
        }

        private Vector3 ToWorldCenter(Vector2 nodePosition, Vector2 nodeSize)
        {
            return _previewOrigin + new Vector3(nodePosition.x + nodeSize.x * 0.5f, -nodePosition.y - nodeSize.y * 0.5f, 0f);
        }

        private void AssertVector2(Vector2 actual, Vector2 expected, string message)
        {
            Assert(Mathf.Abs(actual.x - expected.x) < Tolerance, $"{message} x mismatch. expected={expected.x}, actual={actual.x}");
            Assert(Mathf.Abs(actual.y - expected.y) < Tolerance, $"{message} y mismatch. expected={expected.y}, actual={actual.y}");
        }
    }
}