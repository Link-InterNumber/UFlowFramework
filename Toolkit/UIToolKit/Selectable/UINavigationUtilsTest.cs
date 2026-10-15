using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    /// <summary>
    /// UINavigationUtils 的运行时测试脚本。
    /// Runtime test component for UINavigationUtils.
    /// </summary>
    public sealed class UINavigationUtilsTest : RunTestMono
    {
        [SerializeField]
        private bool _runOnStart = true;

        [SerializeField]
        private Vector2 _testGroupSpacing = new Vector2(520f, 300f);

        private int _testGroupIndex;

        private void Start()
        {
            if (!_runOnStart)
            {
                return;
            }

            RunAllTests();
        }

        [ContextMenu("Run UINavigationUtils Tests")]
        public void RunAllTests()
        {
            _testGroupIndex = 0;
            Debug.Log("========== UINavigationUtils Test Suite Started ==========");

            RunTest("SetNavigation", TestSetNavigation);
            RunTest("Vertical", () => TestLinearLayout(UINavigationUtils.NavigationLayout.Vertical));
            RunTest("VerticalCircle", () => TestLinearLayout(UINavigationUtils.NavigationLayout.VerticalCircle));
            RunTest("Horizontal", () => TestLinearLayout(UINavigationUtils.NavigationLayout.Horizontal));
            RunTest("HorizontalCircle", () => TestLinearLayout(UINavigationUtils.NavigationLayout.HorizontalCircle));
            RunTest("Grid", TestGrid);
            RunTest("GridCircle", TestGridCircle);

            Debug.Log("========== UINavigationUtils Test Suite Finished ==========");
        }

        private void TestSetNavigation()
        {
            Selectable[] selectables = CreateSelectables(new Vector2Int(4, 1));
            try
            {
                selectables[0].SetNavigation(
                    selectables[1],
                    selectables[2],
                    selectables[3],
                    selectables[0]);

                Navigation navigation = selectables[0].navigation;
                Assert(navigation.mode == Navigation.Mode.Explicit, "Navigation mode should be Explicit.");
                Assert(navigation.selectOnUp == selectables[1], "Up target is incorrect.");
                Assert(navigation.selectOnDown == selectables[2], "Down target is incorrect.");
                Assert(navigation.selectOnLeft == selectables[3], "Left target is incorrect.");
                Assert(navigation.selectOnRight == selectables[0], "Right target is incorrect.");
            }
            finally
            {
                DestroySelectables(selectables);
            }
        }

        private void TestLinearLayout(UINavigationUtils.NavigationLayout layout)
        {
            Vector2Int size = layout == UINavigationUtils.NavigationLayout.Vertical ||
                layout == UINavigationUtils.NavigationLayout.VerticalCircle
                ? new Vector2Int(1, 4)
                : new Vector2Int(4, 1);
            Selectable[] selectables = CreateSelectables(size);
            try
            {
                UINavigationUtils.SetNavigationGroup(layout, new Vector2Int(4, 1), selectables);

                for (int i = 0; i < selectables.Length; i++)
                {
                    Navigation navigation = selectables[i].navigation;
                    Selectable previous = i > 0 ? selectables[i - 1] : null;
                    Selectable next = i < selectables.Length - 1 ? selectables[i + 1] : null;

                    if (layout == UINavigationUtils.NavigationLayout.Vertical ||
                        layout == UINavigationUtils.NavigationLayout.VerticalCircle)
                    {
                        Assert(navigation.selectOnUp == (layout == UINavigationUtils.NavigationLayout.VerticalCircle
                            ? selectables[(i - 1 + selectables.Length) % selectables.Length]
                            : previous), $"Vertical up target is incorrect at index {i}.");
                        Assert(navigation.selectOnDown == (layout == UINavigationUtils.NavigationLayout.VerticalCircle
                            ? selectables[(i + 1) % selectables.Length]
                            : next), $"Vertical down target is incorrect at index {i}.");
                    }
                    else
                    {
                        Assert(navigation.selectOnLeft == (layout == UINavigationUtils.NavigationLayout.HorizontalCircle
                            ? selectables[(i - 1 + selectables.Length) % selectables.Length]
                            : previous), $"Horizontal left target is incorrect at index {i}.");
                        Assert(navigation.selectOnRight == (layout == UINavigationUtils.NavigationLayout.HorizontalCircle
                            ? selectables[(i + 1) % selectables.Length]
                            : next), $"Horizontal right target is incorrect at index {i}.");
                    }
                }
            }
            finally
            {
                DestroySelectables(selectables);
            }
        }

        private void TestGrid()
        {
            const int columns = 3;
            const int rows = 2;
            Selectable[] selectables = CreateSelectables(new Vector2Int(columns, rows));
            try
            {
                UINavigationUtils.SetNavigationGroup(
                    UINavigationUtils.NavigationLayout.Grid,
                    new Vector2Int(columns, rows),
                    selectables);

                AssertNavigation(selectables[0], null, selectables[3], null, selectables[1]);
                AssertNavigation(selectables[1], null, selectables[4], selectables[0], selectables[2]);
                AssertNavigation(selectables[2], null, selectables[5], selectables[1], null);
                AssertNavigation(selectables[3], selectables[0], null, null, selectables[4]);
                AssertNavigation(selectables[4], selectables[1], null, selectables[3], selectables[5]);
                AssertNavigation(selectables[5], selectables[2], null, selectables[4], null);
            }
            finally
            {
                DestroySelectables(selectables);
            }
        }

        private void TestGridCircle()
        {
            const int columns = 3;
            const int rows = 2;
            Selectable[] selectables = CreateSelectables(new Vector2Int(columns, rows));
            try
            {
                UINavigationUtils.SetNavigationGroup(
                    UINavigationUtils.NavigationLayout.GridCircle,
                    new Vector2Int(columns, rows),
                    selectables);

                AssertNavigation(selectables[0], selectables[3], selectables[3], selectables[2], selectables[1]);
                AssertNavigation(selectables[1], selectables[4], selectables[4], selectables[0], selectables[2]);
                AssertNavigation(selectables[2], selectables[5], selectables[5], selectables[1], selectables[0]);
                AssertNavigation(selectables[3], selectables[0], selectables[0], selectables[5], selectables[4]);
                AssertNavigation(selectables[4], selectables[1], selectables[1], selectables[3], selectables[5]);
                AssertNavigation(selectables[5], selectables[2], selectables[2], selectables[4], selectables[3]);
            }
            finally
            {
                DestroySelectables(selectables);
            }
        }

        private void AssertNavigation(
            Selectable selectable,
            Selectable up,
            Selectable down,
            Selectable left,
            Selectable right)
        {
            Navigation navigation = selectable.navigation;
            Assert(navigation.mode == Navigation.Mode.Explicit, $"Navigation mode is incorrect for {selectable.name}.");
            Assert(navigation.selectOnUp == up, $"Up target is incorrect for {selectable.name}.");
            Assert(navigation.selectOnDown == down, $"Down target is incorrect for {selectable.name}.");
            Assert(navigation.selectOnLeft == left, $"Left target is incorrect for {selectable.name}.");
            Assert(navigation.selectOnRight == right, $"Right target is incorrect for {selectable.name}.");
        }

        private Selectable[] CreateSelectables(Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0)
            {
                return new Selectable[0];
            }

            const float cellSize = 100f;
            const float spacing = 20f;
            Vector2 groupOffset = new Vector2(
                (_testGroupIndex % 3) * _testGroupSpacing.x,
                -(_testGroupIndex / 3) * _testGroupSpacing.y);
            _testGroupIndex++;
            int count = size.x * size.y;
            Selectable[] selectables = new Selectable[count];
            for (int i = 0; i < count; i++)
            {
                GameObject gameObject = new GameObject($"UINavigationTest_{i}");
                RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
                rectTransform.SetParent(transform, false);
                int row = i / size.x;
                int column = i % size.x;
                rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                rectTransform.anchoredPosition = new Vector2(
                    groupOffset.x + column * (cellSize + spacing),
                    groupOffset.y - row * (cellSize + spacing));
                selectables[i] = gameObject.AddComponent<Button>();
                gameObject.AddComponent<Image>();
                var text = new GameObject("Text").AddComponent<Text>();
                text.text = i.ToString();
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 35;
                text.transform.SetParent(gameObject.transform, false);
            }

            return selectables;
        }

        private static void DestroySelectables(Selectable[] selectables)
        {
            // if (selectables == null)
            // {
            //     return;
            // }
            //
            // for (int i = 0; i < selectables.Length; i++)
            // {
            //     if (selectables[i] != null)
            //     {
            //         Destroy(selectables[i].gameObject);
            //     }
            // }
        }
    }
}
