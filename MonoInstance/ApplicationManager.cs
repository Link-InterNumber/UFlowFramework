using System;
using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    public class ApplicationManager : MonoSingleton<ApplicationManager>
    {
        [SerializeField] 
        private static ApplicationState _applicationState;
        public static ApplicationState appState => _applicationState;
        private ResolutionLv _curResolutionLv = ResolutionLv.Mid;
        public static bool enableLog;
        public static bool enableWarning;
        public static bool enableError = true;

        public ResolutionLv curResolutionLv
        {
            get => _curResolutionLv;
            set
            {
                _curResolutionLv = value;
                SetResolution(false);
            }
        }

        public Vector2 curResolution
        {
            get
            {
                var screenHeight = Screen.height;
                var screenWidth = Screen.width;
                var newRes = Vector2Int.zero;
                if (screenHeight < screenWidth)
                {
                    newRes.y = ConstSetting.Resolution[(int)_curResolutionLv];
                    newRes.x = Mathf.RoundToInt(newRes.y * (float)screenWidth / screenHeight);
                }
                else
                {
                    newRes.x = ConstSetting.Resolution[(int)_curResolutionLv];
                    newRes.y = Mathf.RoundToInt(newRes.x * (float)screenHeight / screenWidth);
                }
                return newRes;
            }
        }
        
        public int TargetFrameRate
        {
            get => Application.targetFrameRate;
            set
            {
                if(value <= 0) return;
                Application.targetFrameRate = value;
            }
        }

        private void SetResolution(bool fullscreen)
        {
            var screenHeight = Screen.height;
            var screenWidth = Screen.width;
            var newRes = Vector2Int.zero;
            if (screenHeight < screenWidth)
            {
                newRes.y = ConstSetting.Resolution[(int)_curResolutionLv];
                newRes.x = Mathf.RoundToInt(newRes.y * (float)screenWidth / screenHeight);
            }
            else
            {
                newRes.x = ConstSetting.Resolution[(int)_curResolutionLv];
                newRes.y = Mathf.RoundToInt(newRes.x * (float)screenHeight / screenWidth);
            }

            Screen.SetResolution(newRes.x, newRes.y, fullscreen);
            // Debug.LogError($"newRes: {newRes.x} * {newRes.y}");
            // Debug.LogError($"Resolution: {Screen.currentResolution.width} * {Screen.currentResolution.height}");
            EventManager.instance.onChangeResolution.Invoke(newRes);
        }

        public void OnInit()
        {
#if UNITY_EDITOR
            enableLog = true;
            enableWarning = true;
            enableError = true;
#elif DEBUG
            enableLog = false;
            enableWarning = false;
            enableError = true;
            var debugLogSaver = new GameObject("DebugLogSaver").AddComponent<DebugLogSaver>();
#else
            enableLog = false;
            enableWarning = false;
            enableError = false;
#endif
            AppLog.Log("AppStart");
        }

        protected override void Awake()
        {
            base.Awake();
            OnInit();
            DontDestroyOnLoad(gameObject);
            _applicationState = ApplicationState.Loading;
            SetResolution(true);
            Application.lowMemory += ClearUnusedAsset;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }

        private ScreenOrientation _currentOrientation = ScreenOrientation.AutoRotation;
        public ScreenOrientation CurrentOrientation => _currentOrientation; 
        private void Update()
        {
            var orientation = Screen.orientation;
            if (_currentOrientation == orientation) return;
            _currentOrientation = orientation;
            EventManager.instance.onScreenOrientationChange.Invoke(_currentOrientation);
        }
        
        public void ClearUnusedAsset()
        {
            PoolManager.instance?.ClearAllPool();
            UIManager.instance?.Clear();
            EventManager.instance?.onClearUnusedAsset?.Invoke();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private void OnApplicationPause(bool hasFocus)
        {
            _applicationState = hasFocus ? ApplicationState.Pause : ApplicationState.Playing;
            EventManager.instance.onPause.Invoke(_applicationState != ApplicationState.Playing);
        }

        protected void OnApplicationQuit()
        {
            _applicationState = ApplicationState.Quit;
            EventManager.instance.onQuit.Invoke();
            EventManager.instance.onLoading.RemoveAllListeners();
            EventManager.instance.onPause.RemoveAllListeners();
            EventManager.instance.onQuit.RemoveAllListeners();
            EventManager.instance.onChangeResolution.RemoveAllListeners();
        }

        public void SetLoading(bool isLoading)
        {
            _applicationState = isLoading ? ApplicationState.Loading : ApplicationState.Playing;
            if (isLoading) EventManager.instance.onLoading.Invoke();
        }

        public void ResetGame()
        {
            EventManager.instance.onResetGame.Invoke();
        }

        public Coroutine DelayedNextFrame(Action call)
        {
            return StartCoroutine(DelayedNextFrameHandler(call));
        }
        
        private static IEnumerator DelayedNextFrameHandler(Action call)
        {
            yield return null;
            call?.Invoke();
        }

        public Coroutine DelayedCall(float timeInSecond, Action call, bool ignoreTimeScale = true)
        {
            return StartCoroutine(DelayedCallHandler(timeInSecond, call, ignoreTimeScale));
        }

        private static IEnumerator DelayedCallHandler(float timeInSecond, Action call, bool ignoreTimeScale)
        {
            if (ignoreTimeScale) yield return new WaitForSecondsRealtime(timeInSecond);
            else yield return new WaitForSeconds(timeInSecond);
            call?.Invoke();
        }

#if UNITY_EDITOR

        [TestSlider(0, 10)]
        public void SetGlobalTimeScale(float scale)
        {
            TimeManager.instance.SetGlobalScale(scale);
        }
        
        [TestButton]
        public void ChangeResolution()
        {
            _curResolutionLv = (ResolutionLv)(((int)curResolutionLv + 1) % 3);
            SetResolution(false);
        }
#endif

    }
}