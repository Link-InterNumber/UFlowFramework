using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PowerCellStudio
{
    public class SpriteRendererLocalization : AssetLocalizationSwitch
    {
        public SpriteRenderer img;
        
        protected override void BeforeLoaded()
        {
            // img.enabled = false;
        }

        protected override void OnLoaded(Object asset)
        {
            img.sprite = asset as Sprite;
            // img.enabled = true;
        }
        
        protected override void OnLoadFailed()
        {
            
        }
    }
}