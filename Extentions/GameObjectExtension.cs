using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// 提供 GameObject 相关的组件、激活状态、层级、标签和层级设置扩展方法。
    /// Provides GameObject extension methods for components, active state, hierarchy, tags, and layers.
    /// </summary>
    public static class GameObjectExtension
    {
        /// <summary>
        /// 获取指定类型的 Behaviour 组件；如果不存在，则添加该组件。
        /// Gets a Behaviour component of the specified type, or adds it if it does not exist.
        /// </summary>
        /// <typeparam name="T">要获取或添加的 Behaviour 组件类型。Behaviour component type to get or add.</typeparam>
        /// <param name="gameObject">目标 GameObject。Target GameObject.</param>
        /// <returns>获取到或新添加的组件；如果目标无效则返回 null。Existing or newly added component, or null if the target is invalid.</returns>
        public static T TryAddComponent<T>(this GameObject gameObject) where T : Behaviour
        {
            if (!gameObject) return null;
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        /// <summary>
        /// 移除指定类型的 Behaviour 组件。
        /// Removes a Behaviour component of the specified type.
        /// </summary>
        /// <typeparam name="T">要移除的 Behaviour 组件类型。Behaviour component type to remove.</typeparam>
        /// <param name="gameObject">目标 GameObject。Target GameObject.</param>
        /// <returns>如果成功找到并移除组件则返回 true，否则返回 false。Returns true if the component is found and removed; otherwise, false.</returns>
        public static bool RemoveComponent<T>(this GameObject gameObject) where T : Behaviour
        {
            if (!gameObject) return false;
            var component = gameObject.GetComponent<T>();
            if (!component) return false;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject.DestroyImmediate(component);
                return true;
            }
#endif
            GameObject.Destroy(component);
            return true;
        }

        /// <summary>
        /// 设置指定类型 Behaviour 组件的启用状态。
        /// Sets the enabled state of a Behaviour component of the specified type.
        /// </summary>
        /// <typeparam name="T">要设置启用状态的 Behaviour 组件类型。Behaviour component type to enable or disable.</typeparam>
        /// <param name="gameObject">目标 GameObject。Target GameObject.</param>
        /// <param name="enabled">是否启用组件。Whether to enable the component.</param>
        /// <returns>如果成功找到并设置组件则返回 true，否则返回 false。Returns true if the component is found and updated; otherwise, false.</returns>
        public static bool SetComponentEnabled<T>(this GameObject gameObject, bool enabled) where T : Behaviour
        {
            if (!gameObject) return false;

            var component = gameObject.GetComponent<T>();
            if (!component) return false;

            component.enabled = enabled;
            return true;
        }

        /// <summary>
        /// 重新激活 GameObject，用于触发禁用再启用的刷新逻辑。
        /// Reactivates the GameObject to trigger disable-and-enable refresh logic.
        /// </summary>
        /// <param name="go">目标 GameObject。Target GameObject.</param>
        public static void ReActive(this GameObject go)
        {
            if (!go) return;
            if (!go.activeSelf)
            {
                go.SetActive(true);
                return;
            }

            go.SetActive(false);
            go.SetActive(true);
        }

        /// <summary>
        /// 递归设置当前 GameObject 及其所有子节点的激活状态。
        /// Recursively sets the active state of the current GameObject and all child GameObjects.
        /// </summary>
        /// <param name="obj">目标 GameObject。Target GameObject.</param>
        /// <param name="active">要设置的激活状态。Active state to set.</param>
        public static void SetActiveRecursively(this GameObject obj, bool active)
        {
            if (!obj) return;

            obj.SetActive(active);
            foreach (Transform child in obj.transform)
            {
                child.gameObject.SetActiveRecursively(active);
            }
        }

        /// <summary>
        /// 判断 GameObject 是否为预制体或预制体实例。
        /// Determines whether the GameObject is a prefab or prefab instance.
        /// </summary>
        /// <param name="gameObject">目标 GameObject。Target GameObject.</param>
        /// <param name="includeMissingAsset">是否将丢失预制体资源关联的 GameObject 视为预制体实例。Whether to treat GameObjects with missing prefab assets as prefab instances.</param>
        /// <returns>如果是预制体或预制体实例则返回 true，否则返回 false。Returns true if it is a prefab or prefab instance; otherwise, false.</returns>
        public static bool IsPrefab(this GameObject gameObject, bool includeMissingAsset = false)
        {
            if (!gameObject) return false;
            if (!gameObject.scene.IsValid()) return true;
#if UNITY_EDITOR
            var type = UnityEditor.PrefabUtility.GetPrefabAssetType(gameObject);
            if (type == UnityEditor.PrefabAssetType.NotAPrefab || (!includeMissingAsset && type == UnityEditor.PrefabAssetType.MissingAsset))
                return false;

            var status = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(gameObject);
            return status != UnityEditor.PrefabInstanceStatus.NotAPrefab;
#else
            return false;
#endif
        }

        /// <summary>
        /// 递归设置当前 GameObject 及其所有子节点的标签。
        /// Recursively sets the tag of the current GameObject and all child GameObjects.
        /// </summary>
        /// <param name="obj">目标 GameObject。Target GameObject.</param>
        /// <param name="tagName">要设置的标签名称。Tag name to set.</param>
        public static void SetTagRecursively(this GameObject obj, string tagName)
        {
            if (!obj) return;
            obj.tag = tagName;
            foreach (Transform child in obj.transform)
            {
                SetTagRecursively(child.gameObject, tagName);
            }
        }

        /// <summary>
        /// 递归判断当前 GameObject 及其所有子节点是否都具有指定标签。
        /// Recursively checks whether the current GameObject and all child GameObjects have the specified tag.
        /// </summary>
        /// <param name="obj">目标 GameObject。Target GameObject.</param>
        /// <param name="tagName">要比较的标签名称。Tag name to compare.</param>
        /// <returns>如果当前对象及所有子节点都具有指定标签则返回 true，否则返回 false。Returns true if the current object and all children have the specified tag; otherwise, false.</returns>
        public static bool CompareTagRecursively(this GameObject obj, string tagName)
        {
            if (!obj) return false;
            if (!obj.CompareTag(tagName)) return false;
            foreach (Transform child in obj.transform)
            {
                if (!CompareTagRecursively(child.gameObject, tagName)) return false;
            }

            return true;
        }

        /// <summary>
        /// 递归设置当前 GameObject 及其所有子节点的 Layer。
        /// Recursively sets the layer of the current GameObject and all child GameObjects.
        /// </summary>
        /// <param name="obj">目标 GameObject。Target GameObject.</param>
        /// <param name="layer">要设置的 Layer 索引。Layer index to set.</param>
        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            if (!obj) return;

            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        /// <summary>
        /// 根据 Layer 名称递归设置当前 GameObject 及其所有子节点的 Layer。
        /// Recursively sets the layer of the current GameObject and all child GameObjects by layer name.
        /// </summary>
        /// <param name="obj">目标 GameObject。Target GameObject.</param>
        /// <param name="layerName">要设置的 Layer 名称。Layer name to set.</param>
        public static void SetLayerRecursively(this GameObject obj, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) return;

            obj.SetLayerRecursively(layer);
        }

        /// <summary>
        /// 判断 GameObject 当前 Layer 是否包含在指定 LayerMask 中。
        /// Determines whether the GameObject's current layer is included in the specified LayerMask.
        /// </summary>
        /// <param name="gameObject">目标 GameObject。Target GameObject.</param>
        /// <param name="layerMask">用于检测的 LayerMask。LayerMask to test against.</param>
        /// <returns>如果当前 Layer 包含在 LayerMask 中则返回 true，否则返回 false。Returns true if the current layer is included in the LayerMask; otherwise, false.</returns>
        public static bool IsInLayerMask(this GameObject gameObject, LayerMask layerMask)
        {
            if (!gameObject) return false;

            return (layerMask.value & (1 << gameObject.layer)) != 0;
        }

        /// <summary>
        /// 销毁当前 GameObject 下的所有直接子节点。
        /// Destroys all direct child GameObjects under the current GameObject.
        /// </summary>
        /// <param name="obj">目标 GameObject。Target GameObject.</param>
        public static void DestroyChildren(this GameObject obj)
        {
            if (!obj || obj.transform.childCount == 0) return;
            while (obj.transform.childCount > 0)
            {
                var child = obj.transform.GetChild(0);
                child.SetParent(null);
                GameObject.Destroy(child.gameObject);
            }
        }
    }
}