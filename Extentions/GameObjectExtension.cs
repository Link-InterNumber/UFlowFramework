#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PowerCellStudio
{
    public static class GameObjectExtension
    {
        public static T TryAddComponent<T>(this GameObject gameObject) where T : Behaviour
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }


        public static void ReActive(this GameObject go)
        {
            if(!go) return;
            if (!go.activeSelf)
            {
                go.SetActive(true);
                return;
            }
            go.SetActive(false);
            go.SetActive(true);
        }

        /// <summary>
        /// 是不是预制体
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="includeMissingAsset">是否将丢失预制体关联的GameObject视为预制体实例？</param>
        /// <returns></returns>
        public static bool IsPrefab(this GameObject gameObject, bool includeMissingAsset = false)
        {
            if (!gameObject) return false;
            if (!gameObject.scene.IsValid()) return true;
#if UNITY_EDITOR
            var type = PrefabUtility.GetPrefabAssetType(gameObject);
            if (type == PrefabAssetType.NotAPrefab || (!includeMissingAsset && type == PrefabAssetType.MissingAsset))
                return false;

            var status = PrefabUtility.GetPrefabInstanceStatus(gameObject);
            return status != PrefabInstanceStatus.NotAPrefab;
#else
            return false;
#endif
        }
        
        public static void SetLayerRecursively(this GameObject obj, string layerName)
        {
            if(!obj) return;
            obj.layer = LayerMask.NameToLayer(layerName);
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layerName);
            }
        }
        
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