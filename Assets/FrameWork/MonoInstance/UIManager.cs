using LinkFrameWork.DesignPatterns;

namespace LinkFrameWork.MonoInstance
{
    public class UIManager : MonoSingleton<UIManager>
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }
}