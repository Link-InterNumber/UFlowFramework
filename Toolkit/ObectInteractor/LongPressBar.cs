using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Image))]
    public class LongPressBar : MonoBehaviour
    {
        public Image longPressImage;
        public ObjectLongPress longPressInteractor;

        private void Awake()
        {
            if (!longPressImage) longPressImage = GetComponent<Image>();
            if (!longPressImage) return;
            longPressInteractor?.onActive.AddListener(OnActive);
            longPressImage.type = Image.Type.Filled;
        }

        private void OnDestroy()
        {
            longPressInteractor?.onActive.RemoveListener(OnActive);
        }

        private void OnActive(bool isActive)
        {
            if (!longPressImage) return;
            longPressImage.fillAmount = 0f;
            longPressImage.gameObject.SetActive(isActive);
        }

        private void Update()
        {
            if (longPressInteractor == null || !longPressImage) return;
            longPressImage.fillAmount = longPressInteractor.processValue;
        }
    }
}