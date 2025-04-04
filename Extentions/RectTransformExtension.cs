using UnityEngine;

namespace PowerCellStudio
{
    public static class RectTransformExtension
    {
        public static void Adapt2Parent(this RectTransform rectTransform)
        {
            if(!rectTransform || rectTransform.parent == null) return;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
