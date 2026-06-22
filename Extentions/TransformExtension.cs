using UnityEngine;

namespace PowerCellStudio
{
    public static class TransformExtension
    {
        /// <summary>
        /// 获取指定类型的组件；如果不存在，则在当前 Transform 所属的 GameObject 上添加该组件。
        /// Gets a component of the specified type, or adds it to the current Transform's GameObject if it does not exist.
        /// </summary>
        /// <typeparam name="T">要获取或添加的组件类型。</typeparam>
        /// <param name="transform">目标 Transform。</param>
        /// <returns>获取到或新添加的组件实例。</returns>
        public static T GetOrAddComponent<T>(this Transform transform) where T : MonoBehaviour
        {
            return transform.GetComponent<T>() ?? transform.gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 重置当前 Transform 的本地位置、旋转和缩放。
        /// Resets the local position, local rotation, and local scale of the current Transform.
        /// </summary>
        /// <param name="transform">要重置的 Transform。</param>
        public static void ResetLocal(this Transform transform)
        {
            if (!transform) return;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 设置当前 Transform 世界坐标的 X 分量。
        /// Sets the X component of the current Transform's world position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="x">新的世界坐标 X 分量值。</param>
        public static void SetPositionX(this Transform transform, float x)
        {
            if (!transform) return;

            var position = transform.position;
            position.x = x;
            transform.position = position;
        }

        /// <summary>
        /// 设置当前 Transform 世界坐标的 Y 分量。
        /// Sets the Y component of the current Transform's world position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="y">新的世界坐标 Y 分量值。</param>
        public static void SetPositionY(this Transform transform, float y)
        {
            if (!transform) return;

            var position = transform.position;
            position.y = y;
            transform.position = position;
        }

        /// <summary>
        /// 设置当前 Transform 世界坐标的 Z 分量。
        /// Sets the Z component of the current Transform's world position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="z">新的世界坐标 Z 分量值。</param>
        public static void SetPositionZ(this Transform transform, float z)
        {
            if (!transform) return;

            var position = transform.position;
            position.z = z;
            transform.position = position;
        }

        /// <summary>
        /// 设置当前 Transform 本地坐标的 X 分量。
        /// Sets the X component of the current Transform's local position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="x">新的本地坐标 X 分量值。</param>
        public static void SetLocalPositionX(this Transform transform, float x)
        {
            if (!transform) return;

            var position = transform.localPosition;
            position.x = x;
            transform.localPosition = position;
        }

        /// <summary>
        /// 设置当前 Transform 本地坐标的 Y 分量。
        /// Sets the Y component of the current Transform's local position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="y">新的本地坐标 Y 分量值。</param>
        public static void SetLocalPositionY(this Transform transform, float y)
        {
            if (!transform) return;

            var position = transform.localPosition;
            position.y = y;
            transform.localPosition = position;
        }

        /// <summary>
        /// 设置当前 Transform 本地坐标的 Z 分量。
        /// Sets the Z component of the current Transform's local position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="z">新的本地坐标 Z 分量值。</param>
        public static void SetLocalPositionZ(this Transform transform, float z)
        {
            if (!transform) return;

            var position = transform.localPosition;
            position.z = z;
            transform.localPosition = position;
        }

        /// <summary>
        /// 在当前 Transform 的世界坐标上增加指定偏移量。
        /// Adds the specified offset to the current Transform's world position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="offset">要增加的世界坐标偏移量。</param>
        public static void AddPosition(this Transform transform, Vector3 offset)
        {
            if (!transform) return;

            transform.position += offset;
        }

        /// <summary>
        /// 在当前 Transform 的本地坐标上增加指定偏移量。
        /// Adds the specified offset to the current Transform's local position.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="offset">要增加的本地坐标偏移量。</param>
        public static void AddLocalPosition(this Transform transform, Vector3 offset)
        {
            if (!transform) return;

            transform.localPosition += offset;
        }

        /// <summary>
        /// 设置当前 Transform 本地缩放的 X 分量。
        /// Sets the X component of the current Transform's local scale.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="x">新的本地缩放 X 分量值。</param>
        public static void SetLocalScaleX(this Transform transform, float x)
        {
            if (!transform) return;

            var scale = transform.localScale;
            scale.x = x;
            transform.localScale = scale;
        }

        /// <summary>
        /// 设置当前 Transform 本地缩放的 Y 分量。
        /// Sets the Y component of the current Transform's local scale.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="y">新的本地缩放 Y 分量值。</param>
        public static void SetLocalScaleY(this Transform transform, float y)
        {
            if (!transform) return;

            var scale = transform.localScale;
            scale.y = y;
            transform.localScale = scale;
        }

        /// <summary>
        /// 设置当前 Transform 本地缩放的 Z 分量。
        /// Sets the Z component of the current Transform's local scale.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <param name="z">新的本地缩放 Z 分量值。</param>
        public static void SetLocalScaleZ(this Transform transform, float z)
        {
            if (!transform) return;

            var scale = transform.localScale;
            scale.z = z;
            transform.localScale = scale;
        }

        /// <summary>
        /// 在当前 Transform 的所有后代节点中递归查找指定名称的子节点。
        /// Recursively searches all descendants of the current Transform for a child with the specified name.
        /// </summary>
        /// <param name="transform">搜索起点 Transform。</param>
        /// <param name="childName">要查找的子节点名称。</param>
        /// <returns>找到的子节点 Transform；如果未找到则返回 null。</returns>
        public static Transform FindDeepChild(this Transform transform, string childName)
        {
            if (!transform || string.IsNullOrEmpty(childName)) return null;

            foreach (Transform child in transform)
            {
                if (child.name == childName) return child;

                var result = child.FindDeepChild(childName);
                if (result) return result;
            }

            return null;
        }

        /// <summary>
        /// 获取当前 Transform 从根节点到自身的层级路径。
        /// Gets the hierarchy path from the root Transform to the current Transform.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        /// <returns>层级路径；如果 Transform 无效则返回空字符串。</returns>
        public static string GetPath(this Transform transform)
        {
            if (!transform) return string.Empty;

            string path = transform.name;
            while (transform.parent)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        /// <summary>
        /// 判断两个 Transform 是否引用同一个 Unity 实例。
        /// Determines whether two Transforms reference the same Unity instance.
        /// </summary>
        /// <param name="transform">当前 Transform。</param>
        /// <param name="other">要比较的另一个 Transform。</param>
        /// <returns>如果两个 Transform 的实例 ID 相同则返回 true，否则返回 false。</returns>
        public static bool Equals(this Transform transform, Transform other)
        {
            return transform.transform.GetInstanceID() == other.GetInstanceID();
        }

        /// <summary>
        /// 销毁当前 Transform 下的所有直接子节点。
        /// Destroys all direct child GameObjects under the current Transform.
        /// </summary>
        /// <param name="transform">目标 Transform。</param>
        public static void DestroyChildren(this Transform transform)
        {
            if (!transform || transform.childCount == 0) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR  
                if (!Application.isPlaying)
                {
                    GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
                    continue;
                }
#endif
                GameObject.Destroy(transform.GetChild(i).gameObject);
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