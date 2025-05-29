
#if UNITY_EDITOR
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
        private void OnDrawGizmos()
        {
            if(!DrawDebug)
                return;
            // Gizmos.color = Color.magenta;
            var elements = transform.GetComponentsInChildren<MaskableGraphic>(true);
            if (elements == null || elements.Length == 0) return; 
            var originColor = Gizmos.color;
            var elementsCount = elements.Length;
            for (var i = 0; i < elementsCount; i++)
            {
                var text = elements[i];
                if (text.raycastTarget)
                {
                    RectTransform rectTransform = text.transform as RectTransform;
                    rectTransform.GetWorldCorners(fourCorners);
                    Gizmos.color = Color.Lerp(Color.blue, Color.magenta, i * 1f / Mathf.Max(1, elementsCount - 1));
                    for (int j = 0; j < 4; j++)
                    {
                        Gizmos.DrawLine(fourCorners[j], fourCorners[(j+1)%4]);
                    }
                }
            }
            Gizmos.color = originColor;
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
            foreach (var text in Selection.activeTransform.GetComponentsInChildren<MaskableGraphic>(true))
            {
                if (text.raycastTarget)
                {
                    text.raycastTarget = false;
                }
            }
        }
#endif
    }
}