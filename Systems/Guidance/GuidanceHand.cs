using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class GuidanceHand : MonoBehaviour
    {
        public Text content;
        private GuidanceTag _guidanceTag;

        public void Init(GuidanceTag guidanceTag, string guidanceDecs)
        {
            _guidanceTag = guidanceTag;
            if (content) content.text = guidanceDecs;
        }

        private void Start()
        {
            var canvas = transform.GetComponent<Canvas>();
            if (!canvas)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
            canvas.sortingOrder = 6000;
        }

        private void Update()
        {
            if(!_guidanceTag) return;
            transform.position = _guidanceTag.GetUIPosition();
        }
    }
}