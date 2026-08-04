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
        
        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            while (_pageStack.Count > 0)
            {
                var page = _pageStack.Pop();
                if (page != null)
                {
                    UIUtils.ClosePageInstance(page, true, null, null);
                }
            }
            
            UIUtils.ClosePageInstance(_poolPage, true, null, null);
            UIUtils.ClosePageInstance(_standAlonePage, true, null, null);
#endif
            base.OnDestroy();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }
        
        private void OnDisable()
        {
            UnRegisterEvents();
        }

        private void RegisterEvents()
        {
            EventManager.instance?.onClearUnusedAsset.AddListener(Clear);
        }

        private void UnRegisterEvents()
        {
            EventManager.instance?.onClearUnusedAsset.RemoveListener(Clear);
        }
    }
}