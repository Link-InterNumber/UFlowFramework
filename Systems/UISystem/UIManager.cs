using System;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class UIManager : MonoSingleton<UIManager>
    {
        private void Start()
        {
            GameObject.DontDestroyOnLoad(gameObject);
            Init();
            gameObject.SetLayerRecursively("UI");
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            EventManager.instance.onUIOpen.AddListener(OnUIWindowOpened);
            EventManager.instance.onClearUnusedAsset.AddListener(Clear);
        }

        private void OnDisable()
        {
            UnRegisterEvents();
        }

        private void UnRegisterEvents()
        {
            EventManager.instance.onUIOpen.RemoveListener(OnUIWindowOpened);
            EventManager.instance.onClearUnusedAsset.RemoveListener(Clear);
        }
    }
}