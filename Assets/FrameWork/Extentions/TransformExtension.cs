using Unity.VisualScripting;
using UnityEngine;

namespace LinkFrameWork.Extentions
{
    public static class TransformExtension
    {
        public static T GetOrAddComponent<T>(this Transform transform) where T : MonoBehaviour
        {
            return transform.GetComponent<T>() ?? transform.AddComponent<T>();
        }
    }
}