using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public static class UINavigationUtils
    {
        public enum NavigationLayout
        {
            Vertical,
            VerticalCircle,
            Horizontal,
            HorizontalCircle,
            Grid,
            GridCircle
        }

        public static void SetNavigation(this Selectable selectable, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            if (selectable == null) return;
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            selectable.navigation = navigation;
        }

        public static void SetNavigationGroup(NavigationLayout layout, Vector2Int gridSize, params Selectable[] selectables)
        {
            if (selectables == null || selectables.Length == 0) return;
            if (selectables.Length == 1)
            {
                selectables[0].SetNavigation(null, null, null, null);
                return;
            }
            int length = selectables.Length;
            for (int i = 0; i < length; i++)
            {
                int row = i / gridSize.x;
                int col = i % gridSize.x;
                var current = selectables[i];
                if (current == null) continue;
                Selectable up = null, down = null, left = null, right = null;
                switch (layout)
                {
                    case NavigationLayout.Vertical:
                        up = (i > 0) ? selectables[i - 1] : null;
                        down = (i < length - 1) ? selectables[i + 1] : null;
                        break;
                    case NavigationLayout.VerticalCircle:
                        up = selectables[(i - 1 + length) % length];
                        down = selectables[(i + 1) % length];
                        break;
                    case NavigationLayout.Horizontal:
                        left = (i > 0) ? selectables[i - 1] : null;
                        right = (i < length - 1) ? selectables[i + 1] : null;
                        break;
                    case NavigationLayout.HorizontalCircle:
                        left = selectables[(i - 1 + length) % length];
                        right = selectables[(i + 1) % length];
                        break;
                    case NavigationLayout.Grid:
                        up = (row > 0) ? selectables[(row - 1) * gridSize.x + col] : null;
                        down = (row < gridSize.y - 1) ? selectables[(row + 1) * gridSize.x + col] : null;
                        left = (col > 0) ? selectables[row * gridSize.x + (col - 1)] : null;
                        right = (col < gridSize.x - 1) ? selectables[row * gridSize.x + (col + 1)] : null;
                        break;
                    case NavigationLayout.GridCircle:
                        up = selectables[((row - 1 + gridSize.y) % gridSize.y) * gridSize.x + col];
                        down = selectables[((row + 1) % gridSize.y) * gridSize.x + col];
                        left = selectables[row * gridSize.x + ((col - 1 + gridSize.x) % gridSize.x)];
                        right = selectables[row * gridSize.x + ((col + 1) % gridSize.x)];
                        break;
                }
                current.SetNavigation(up, down, left, right);
            }
        }
    }
}