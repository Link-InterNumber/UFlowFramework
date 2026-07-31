using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class ImageSetter: MonoBehaviour
    {
        public AssetPath<Sprite> spritePath;

        private Image _image;

        private IAssetLoader _assetLoader;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (Application.isPlaying)
            {
                _assetLoader = AssetUtils.SpawnLoader("ImageSetter");
                LoadSpriteAsync();
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                AssetUtils.DeSpawnLoader(_assetLoader);
                _assetLoader = null;
            }
            _image = null;
        }

#if UNITY_EDITOR
        private void LoadSpriteInEditor()
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath.assetPath);
            if (sprite != null)
            {
                _image.sprite = sprite;
            }
        }

        private void OnEnable()
        {
            // In edit mode, load the sprite for preview
            if (!Application.isPlaying)
            {
                LoadSpriteInEditor();
            }
        }

        private void OnDisable()
        {
            // Remove the sprite reference when the component is disabled in edit mode
            if (!Application.isPlaying)
            {
                _image.sprite = null;
            }
        }
#endif

        private void LoadSpriteAsync()
        {
            spritePath.LoadAsync(_assetLoader, OnSpriteLoaded);
        }

        private void OnSpriteLoaded(Sprite sprite)
        {
            _image.sprite = sprite;
        }

    }
}