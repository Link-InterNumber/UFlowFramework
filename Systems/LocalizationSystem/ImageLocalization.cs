using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Image))]
    public class ImageLocalization : AssetLocalizationSwitch
    {
        public Image img;
        
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