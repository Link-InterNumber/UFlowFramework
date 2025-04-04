using System;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public static class AssetLoaderExtension
    {
        #region Sprite

        /// <summary>
        /// 异步加载图片
        /// </summary>
        /// <param name="assetLoader">AssetLoader</param>
        /// <param name="address">文件路径</param>
        /// <param name="img">UI图片组件</param>
        /// <param name="callback">对前一个参数的回调</param>
        public static void LoadSpriteAsync(this IAssetLoader assetLoader, string address, Image img, Action<Image> callback = null)
        {
            assetLoader.LoadAsync<Sprite>(address, (loaded) =>
            {
                img.sprite = loaded;
                callback?.Invoke(img);
            });
        }
        
        /// <summary>
        /// 异步加载图片
        /// </summary>
        /// <param name="assetLoader">AssetLoader</param>
        /// <param name="address">文件路径</param>
        /// <param name="img">SpriteRenderer组件</param>
        /// <param name="callback">对前一个参数的回调</param>
        public static void LoadSpriteAsync(this IAssetLoader assetLoader, string address, SpriteRenderer img, Action<SpriteRenderer> callback = null)
        {
            assetLoader.LoadAsync<Sprite>(address, (loaded) =>
            {
                img.sprite = loaded;
                callback?.Invoke(img);
            });
        }

        // /// <summary>
        // /// 同步加载图片
        // /// </summary>
        // /// <param name="assetLoader">AssetLoader</param>
        // /// <param name="address">文件路径</param>
        // /// <param name="img">Image组件</param>
        // public static Sprite LoadSprite(this IAssetLoader assetLoader, string address, Image img = null)
        // {
        //     var loaded = assetLoader.LoadImmediately<Sprite>(address);
        //     if (img) img.sprite = loaded; 
        //     return loaded;
        // }

        // /// <summary>
        // /// 同步加载图片
        // /// </summary>
        // /// <param name="assetLoader">AssetLoader</param>
        // /// <param name="address">文件路径</param>
        // /// <param name="sprite">Sprite组件</param>
        // public static Sprite LoadSprite(this IAssetLoader assetLoader, string address, SpriteRenderer sprite)
        // {
        //     var loaded = assetLoader.LoadImmediately<Sprite>(address);
        //     if (sprite) sprite.sprite = loaded;
        //     return loaded;
        // }

        #endregion

        // #region Prefab
        //
        // /// <summary>
        // /// 同步加载预制体
        // /// </summary>
        // /// <param name="assetLoader">AssetLoader</param>
        // /// <param name="address">预制体路径</param>
        // /// <param name="parent">父节点</param>
        // public static GameObject LoadPrefab(this IAssetLoader assetLoader, string address, Transform parent = null)
        // {
        //     GameObject gameObj = null;
        //     var loaded = assetLoader.LoadImmediately<GameObject>(address);
        //     if (loaded != null)
        //     {
        //         if (parent) gameObj = GameObject.Instantiate(loaded, parent);
        //         else gameObj = GameObject.Instantiate(loaded);
        //         var autoClean = gameObj.AddComponent<ABGameObjectSelfCleanup>();
        //         autoClean.Set(assetLoader, address);
        //     }
        //     return gameObj;
        // }
        //
        // #endregion

        #region Audio

        /// <summary>
        /// 异步加载音频
        /// </summary>
        /// <param name="assetLoader">AssetLoader</param>
        /// <param name="address">音频路径</param>
        /// <param name="callback">对加载的预制体的回调</param>
        public static void LoadAudioAsync(this IAssetLoader assetLoader, string address, Action<AudioClip> callback)
        {
            assetLoader.LoadAsync<AudioClip>(address, (loaded) =>
            {
                callback?.Invoke(loaded);
            });
        }
        
        // /// <summary>
        // /// 同步加载音频
        // /// </summary>
        // /// <param name="assetLoader">AssetLoader</param>
        // /// <param name="address">音频路径</param>
        // public static AudioClip LoadAudio(this IAssetLoader assetLoader, string address)
        // {
        //     var loaded = assetLoader.LoadImmediately<AudioClip>(address);
        //     if (!loaded) AssetLog.LogError($"Can not Find AudioClip, path:<{address}>");
        //     return null;
        // }

        #endregion
    }
}