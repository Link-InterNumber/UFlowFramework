using UnityEngine;

namespace PowerCellStudio
{
    public static class TransformExtension
    {
        // public static T GetOrAddComponent<T>(this Transform transform) where T : MonoBehaviour
        // {
        //     return transform.GetComponent<T>() ?? transform.AddComponent<T>();
        // }

        public static bool Equals(this Transform transform, Transform other)
        {
            return transform.transform.GetInstanceID() == other.GetInstanceID();
        }

        public static void DestroyChildren(this Transform transform)
        {
            if (!transform || transform.childCount == 0) return;
            while (transform.childCount > 0)
            {
                var child = transform.GetChild(0);
                child.SetParent(null);
                GameObject.Destroy(child.gameObject);
            }
        }
        
        // public static Vector3 GetUIPosition(this Transform transform, bool zeroZ = true)
        // {
        //     if(transform.IsUnityNull()) return Vector3.zero;
        //     if (transform is RectTransform rectTransform) return rectTransform.position;
        //     var screenPos = MainCamera.instance.CameraCom.WorldToScreenPoint(transform.position);
        //     var uiPos = UICamera.instance.cameraCom.ScreenToWorldPoint(screenPos);
        //     if (zeroZ) uiPos.z = 0;
        //     return uiPos;
        // }
    }
}