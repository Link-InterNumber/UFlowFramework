#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LinkFrameWork.Extentions
{
    public static class GameObjectExtension
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : MonoBehaviour
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
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
    }
}