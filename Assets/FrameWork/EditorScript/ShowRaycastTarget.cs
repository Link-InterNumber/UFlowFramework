using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

namespace Wahoo.Debugs
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
            var originColor = Gizmos.color;
            Gizmos.color = Color.magenta;
            foreach (var text in transform.GetComponentsInChildren<MaskableGraphic>(true))
            {
                if (text.raycastTarget)
                {
                    RectTransform rectTransform = text.transform as RectTransform;
                    rectTransform.GetWorldCorners(fourCorners);
                    for (int i = 0; i < 4; i++)
                    {
                        Gizmos.DrawLine(fourCorners[i], fourCorners[(i+1)%4]);
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