using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Image))]
    public class ImageAlphaTrim : MonoBehaviour
    {
        public float threshold = 0.5f;
        
        private void Awake()
        {
            var img = GetComponent<Image>();
            if(!img) return;
            img.alphaHitTestMinimumThreshold = threshold;
        }
    }
}