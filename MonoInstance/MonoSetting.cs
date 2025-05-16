using UnityEngine;

namespace PowerCellStudio
{
    [DonotInitModuleIAutoly]
    public class MonoSetting : MonoSingleton<MonoSetting>
    {
        public Material grayMat;

        protected override void Awake()
        {
            base.Awake();
            GameObject.DontDestroyOnLoad(gameObject);
        }
    }
}