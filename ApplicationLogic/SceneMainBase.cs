using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public abstract class SceneMainBase : MonoBehaviour
    {
        public Text loadingString;
        public Text loadingPecent;
        public Image loadingImage;
        
        protected virtual void Awake()
        {
            if (Camera.main.GetComponent<MainCamera>())
            {
                Camera.main.gameObject.name = nameof(MainCamera);
            }
            DontDestroyOnLoad(gameObject);
            // DOTween.Init();
            AssetUtils.Init(this, OnInited);
            StartCoroutine(UpdateLoadState());
        }

        private IEnumerator UpdateLoadState()
        {
            if(!loadingString && !loadingPecent && !loadingImage) yield break;
            while (AssetUtils.initState != AssetInitState.Complete)
            {
                if (loadingString)
                {
                    switch (AssetUtils.initState)
                    {
                        case AssetInitState.InitModule:
                            loadingString.text = "初始化游戏";
                            break;
                        case AssetInitState.CheckForResourceUpdates:
                            loadingString.text = "检查更新资源";
                            break;
                        case AssetInitState.DownloadTheUpdateFile:
                            loadingString.text = "下载更新文件";
                            break;
                        case AssetInitState.Complete:
                            loadingString.text = "更新完成";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if(loadingPecent) loadingPecent.text = AssetUtils.initProcess.ToString("P1");
                if(loadingImage) loadingImage.fillAmount = AssetUtils.initProcess;
                yield return null;
            }
            if (loadingString) loadingString.text = "更新完成";
            if (loadingPecent) loadingPecent.text = "100%";
            if (loadingImage) loadingImage.fillAmount = 1f;
            yield return null;
        }

        private void OnInited()
        {
            // TODO Open UIPAGE
            // UIManager.Instance.PushPage<DebugPage>();
            // var mapModule = ModuleManager.Instance.Get<MapModule>();
            // mapModule.CreatStageMap(1);
            AssetLog.Log("AddressableManager Inited!");
            ModuleManager.Create();
            StartCoroutine(OnAddressableInited());

        }

        private IEnumerator OnAddressableInited()
        {
            yield return ConfigManager.instance.Init(null);
            yield return LocalizationManager.instance.Init(null);
            ApplicationManager.instance.SetLoading(false);
            EventManager.instance.onStartGame.Invoke();
            ReadyForStart();
        }

        protected abstract void ReadyForStart();
    }
}