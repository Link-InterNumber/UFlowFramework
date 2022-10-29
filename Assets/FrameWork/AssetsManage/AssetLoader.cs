using LinkFrameWork.Define;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LinkFrameWork.AssetsManage
{
    public class AssetLoader
    {
        #region Common

        private static void LoadAssetAsync<T>(string path, string bundleName, UnityAction<T> callback) where T : Object
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            DOVirtual.DelayedCall(0.05f, () => { callback?.Invoke(asset); });
            return;
#endif
            AssetsBundleManager.Instance.GetAssetsBundleAsync(bundleName, (bundle) =>
            {
                var loaded = bundle.LoadAsset<T>(path);
                callback?.Invoke(loaded);
            });
        }

        private static T LoadAsset<T>(string path, string bundleName) where T : Object
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset;
#endif
            if (AssetsBundleManager.Instance.GetAssetBundle(bundleName, out var bundle))
            {
                return bundle.LoadAsset<T>(path);
            }

            return null;
        }

        private static void AddRefComponent(Transform asset, string bundleName, AssetType type)
        {
#if UNITY_EDITOR
            return;
#endif
            if (!AssetsBundleManager.EnableAutoUnload)
                return;
            var refCom = asset.GetOrAddComponent<AssetsRefComponent>();
            refCom.ResetRef(bundleName, type);
        }

        private static string GetBundleName(string path)
        {
#if UNITY_EDITOR
            return string.IsNullOrEmpty(path) ? path: "Unity_Editor";
#endif
            return string.IsNullOrEmpty(path) ? path : AssetsBundleManager.Instance.GetBundleNameByAsset(path);
        }

        #endregion


        #region Sprite

        /// <summary>
        /// 异步加载图片
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="img">UI图片组件</param>
        /// <param name="callback">对前一个参数的回调</param>
        public static void LoadSpriteAsync(string path, Image img, UnityAction<Image> callback)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return;
            LoadAssetAsync<Sprite>(path, bundleName, (loaded) =>
            {
                if (loaded)
                {
                    img.sprite = loaded;
                    AddRefComponent(img.transform, bundleName, AssetType.Sprite);
                    callback?.Invoke(img);
                }
                else
                    Debug.LogWarning($"Can not Find Sprite <{path}>");
            });
        }

        /// <summary>
        /// 异步加载图片
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="callback">对前一个参数的回调</param>
        public static void LoadSpriteAsync(string path, SpriteRenderer img, UnityAction<SpriteRenderer> callback)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return;
            LoadAssetAsync<Sprite>(path, bundleName, (loaded) =>
            {
                if (loaded)
                {
                    img.sprite = loaded;
                    AddRefComponent(img.transform, bundleName, AssetType.Sprite);
                    callback?.Invoke(img);
                }
                else
                    Debug.LogWarning($"Can not Find Sprite <{path}>");
            });
        }

        /// <summary>
        /// 同步加载图片
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="img">UI图片组件</param>
        public static void LoadSprite(string path, Image img)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return;
            var loaded = LoadAsset<Sprite>(path, bundleName);
            if (loaded != null)
            {
                img.sprite = loaded;
                AddRefComponent(img.transform, bundleName, AssetType.Sprite);
            }
            else
                Debug.LogWarning($"Can not Find Sprite <{path}>");
        }

        /// <summary>
        /// 同步加载图片
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="img">精灵组件</param>
        public static void LoadSprite(string path, SpriteRenderer img)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return;
            var loaded = LoadAsset<Sprite>(path, bundleName);
            if (loaded != null)
            {
                img.sprite = loaded;
                AddRefComponent(img.transform, bundleName, AssetType.Sprite);
            }
            else
                Debug.LogWarning($"Can not Find Sprite <{path}>");
        }

        #endregion

        #region Prefab

        /// <summary>
        /// 异步加载预制体
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="callback">对加载的预制体的回调</param>
        /// <param name="parent">父节点</param>
        public static void LoadPrefabAsync(string path, UnityAction<Transform> callback, Transform parent = null)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return;
            LoadAssetAsync<GameObject>(path, bundleName, (loaded) =>
            {
                if (loaded)
                {
                    var gameObj = GameObject.Instantiate(loaded);
                    if (parent)
                        gameObj.transform.SetParent(parent);
                    AddRefComponent(gameObj.transform, bundleName, AssetType.Prefab);
                    callback?.Invoke(gameObj.transform);
                }
                else
                    Debug.LogWarning($"Can not Find Prefab <{path}>");
            });
        }

        /// <summary>
        /// 同步加载预制体
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="parent">父节点</param>
        public static Transform LoadPrefab(string path, Transform parent = null)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return null;
            var loaded = LoadAsset<GameObject>(path, bundleName);
            if (loaded != null)
            {
                var gameObj = GameObject.Instantiate(loaded);
                if (parent)
                    gameObj.transform.SetParent(parent);
                AddRefComponent(gameObj.transform, bundleName, AssetType.Prefab);
                return gameObj.transform;
            }

            Debug.LogWarning($"Can not Find Prefab <{path}>");
            return null;
        }

        #endregion

        #region Audio

        /// <summary>
        /// 异步加载音频
        /// </summary>
        /// <param name="path">音频路径</param>
        /// <param name="callback">对加载的预制体的回调</param>
        /// <param name="parent">父节点</param>
        public static void LoadAudioAsync(string path, UnityAction<Transform> callback, Transform parent = null)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return;
            // TODO 
        }

        /// <summary>
        /// 同步加载音频
        /// </summary>
        /// <param name="path">音频路径</param>
        /// <param name="parent">父节点</param>
        public static Transform LoadAudio(string path, Transform parent = null)
        {
            var bundleName = GetBundleName(path);
            if (string.IsNullOrEmpty(bundleName))
                return null;
            // TODO 
            return null;
        }

        #endregion
    }
}