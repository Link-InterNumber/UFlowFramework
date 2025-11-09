using UnityEngine;

namespace PowerCellStudio
{
    [DonotInitModuleAutoly]
    public partial class MonoSetting : MonoSingleton<MonoSetting>
    {
        public Material grayMat;

        protected override void Awake()
        {
            base.Awake();
            GameObject.DontDestroyOnLoad(gameObject);
        }
    }
}