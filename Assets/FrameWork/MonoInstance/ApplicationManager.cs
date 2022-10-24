using LinkFrameWork.Define;
using LinkFrameWork.DesignPatterns;
using Unity.VisualScripting;
using UnityEngine;

namespace LinkFrameWork.MonoInstance
{
    public class ApplicationManager : MonoSingleton<ApplicationManager>
    {
        [SerializeField] private ApplicationState _applicationState;
        public ApplicationState AppState => _applicationState;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            _applicationState = ApplicationState.Loading;
        }

        private void Start()
        {
            _applicationState = ApplicationState.Playing;
        }


        private void OnApplicationPause(bool hasFocus)
        {
            _applicationState = hasFocus ? ApplicationState.Playing : ApplicationState.Pause;
        }

        protected override void OnApplicationQuit()
        {
            _applicationState = ApplicationState.Quit;
            base.OnApplicationQuit();
        }

        public void SetLoading(bool isLoading)
        {
            _applicationState = isLoading ? ApplicationState.Loading : ApplicationState.Playing;
        }
    }
}