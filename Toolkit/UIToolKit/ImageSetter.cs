using System;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class ImageSetter: MonoBehaviour
    {
        public AssetPath<Sprite> spritePath;

        private Image _image;

        private Assetloader _assetLoader;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void LoadSpriteInEditor()
        {
#if UNITY_EDITOR
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                _image.sprite = sprite;
            }
#endif
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            // In edit mode, load the sprite for preview
            if (!Application.isPlaying)
            {
                LoadSpriteInEditor();
            }
#endif
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            // Remove the sprite reference when the component is disabled in edit mode
            if (!Application.isPlaying)
            {
                _image.sprite = null;
            }
        }
#endif

        private void Start()
        {
            // In play mode, start asynchronous loading
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
        }

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