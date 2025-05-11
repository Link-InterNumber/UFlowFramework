using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PowerCellStudio
{
    public class SpriteRendererLocalization : AssetLocalizationSwitch
    {
        public SpriteRenderer img;
        
        protected override void BeforeLoaded()
        {
            img.enabled = false;
        }
        
        protected override void OnLoaded(AsyncOperationHandle<Object> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                img.sprite = handle.Result as Sprite;
            img.enabled = true;
        }
    }
}