
#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class ShowRaycastTarget : MonoBehaviour
    {
        public bool DrawDebug = true;
#if UNITY_EDITOR
        private static Vector3[] fourCorners = new Vector3[4];
        private static readonly Color interactiveFillColor = new Color(1f, 0.45f, 0.15f, 0.1f);
        private static readonly Color nonInteractiveFillColor = new Color(0.2f, 0.75f, 1f, 0.1f);
        private static GUIStyle overlapLabelStyle;

        private static void EnsureOverlapLabelStyle()
        {
            if (overlapLabelStyle != null)
                return;

            overlapLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 20
            };
        }

        private void OnDrawGizmos()
        {
            if(!DrawDebug)
                return;

            EnsureOverlapLabelStyle();
            var elements = transform.GetComponentsInChildren<Graphic>(true);
            if (elements == null || elements.Length == 0)
                return;

            var activeElements = new List<Graphic>(elements.Length);
            var elementsCount = elements.Length;
            for (var i = 0; i < elementsCount; i++)
            {
                var element = elements[i];
                if (element != null && element.raycastTarget)
                {
                    activeElements.Add(element);
                }
            }

            if (activeElements.Count == 0)
                return;

            activeElements.Sort((a, b) => a.depth.CompareTo(b.depth));

            var oldHandlesColor = Handles.color;
            var activeCount = activeElements.Count;
            for (var i = 0; i < activeCount; i++)
            {
                var graphic = activeElements[i];
                var rectTransform = graphic.rectTransform;
                if (rectTransform == null)
                    continue;

                rectTransform.GetWorldCorners(fourCorners);
                var isInteractive = HasUserInteractiveComponent(graphic.transform);
                var layerT = activeCount > 1 ? i * 1f / (activeCount - 1) : 1f;
                var baseFillColor = isInteractive ? interactiveFillColor : nonInteractiveFillColor;
                var fillColor = new Color(
                    baseFillColor.r,
                    baseFillColor.g,
                    baseFillColor.b,
                    Mathf.Lerp(baseFillColor.a * 0.6f, baseFillColor.a * 1.4f, layerT));
                var outlineColor = Color.Lerp(baseFillColor, Color.white, 0.35f);
                outlineColor.a = 0.95f;

                Handles.DrawSolidRectangleWithOutline(fourCorners, fillColor, outlineColor);

                var center = (fourCorners[0] + fourCorners[1] + fourCorners[2] + fourCorners[3]) * 0.25f;
                Handles.color = isInteractive ? new Color(1f, 0.35f, 0.1f, 0.95f) : new Color(0.15f, 0.85f, 1f, 0.95f);
                Handles.DrawSolidDisc(center, Vector3.forward, HandleUtility.GetHandleSize(center) * 0.025f);
                Handles.Label(center, (i + 1).ToString(), overlapLabelStyle);
            }

            Handles.color = oldHandlesColor;
        }
        
        [MenuItem("GameObject/ShowRaycastTarget")]
        public static void ShowNodeRaycastTarget()
        {
            if(!Selection.activeTransform)
                return;
            Selection.activeTransform.gameObject.AddComponent<ShowRaycastTarget>();
        }

        [MenuItem("GameObject/CancelRaycastTarget/TextMeshPro")]
        public static void CancelTextRaycastOfTextMeshPro()
        {
            if(!Selection.activeTransform)
                return;
            foreach (var text in Selection.activeTransform.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.raycastTarget)
                {
                    text.raycastTarget = false;
                }
            }
        }
        
        [MenuItem("GameObject/CancelRaycastTarget/Text")]
        public static void CancelTextRaycastOfText()
        {
            if(!Selection.activeTransform)
                return;
            foreach (var text in Selection.activeTransform.GetComponentsInChildren<Text>(true))
            {
                if (text.raycastTarget)
                {
                    text.raycastTarget = false;
                }
            }
        }
        
        [MenuItem("GameObject/CancelRaycastTarget/Image")]
        public static void CancelTextRaycastOfImage()
        {
            if(!Selection.activeTransform)
                return;
            foreach (var text in Selection.activeTransform.GetComponentsInChildren<Image>(true))
            {
                if (text.raycastTarget)
                {
                    text.raycastTarget = false;
                }
            }
        }
        
        [MenuItem("GameObject/CancelRaycastTarget/All")]
        public static void CancelTextRaycastOfAll()
        {
            if(!Selection.activeTransform)
                return;
            foreach (var text in Selection.activeTransform.GetComponentsInChildren<Graphic>(true))
            {
                if (text.raycastTarget)
                {
                    text.raycastTarget = false;
                }
            }
        }

        [MenuItem("GameObject/CancelRaycastTarget/NonInteractiveOnly")]
        public static void CancelTextRaycastOfNonInteractiveOnly()
        {
            if(!Selection.activeTransform)
                return;
            CancelTextRaycastOfNonInteractiveOnlyRecursive(Selection.activeTransform);
        }

        private static void CancelTextRaycastOfNonInteractiveOnlyRecursive(Transform node)
        {
            if (node == null)
                return;

            if (HasUserInteractiveComponent(node))
                return;

            foreach (var graphic in node.GetComponents<Graphic>())
            {
                if (graphic.raycastTarget)
                {
                    graphic.raycastTarget = false;
                }
            }

            var childCount = node.childCount;
            for (var i = 0; i < childCount; i++)
            {
                CancelTextRaycastOfNonInteractiveOnlyRecursive(node.GetChild(i));
            }
        }

        private static bool HasUserInteractiveComponent(Transform node)
        {
            return node.GetComponent<Selectable>() != null
                   || node.GetComponent<ScrollRect>() != null;
        }
#endif
    }
}