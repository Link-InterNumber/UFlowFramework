using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Image))]
    public class ImageLocalization : AssetLocalizationSwitch
    {
        public Image img;
        
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
            // img.enabled = true;
        }
    }
}