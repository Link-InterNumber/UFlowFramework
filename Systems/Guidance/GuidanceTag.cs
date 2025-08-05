using UnityEngine;

namespace PowerCellStudio
{
    public abstract class GuidanceTag : MonoBehaviour, IUIWidget
    {
        public int guidanceIndex;

        /// <summary>
        /// 启动引导
        /// </summary>
        public abstract void OnExecute();

        protected bool _inExecute = false;

        /// <summary>
        /// 关闭引导
        /// </summary>
        public abstract void OnDeExecute();

        public virtual void OnWidgetEnable()
        {
            if(guidanceIndex == 0) return;
            GuidanceManager.instance.RegisterGuidance(this);
        }

        public virtual void OnWidgetDisable()
        {
            GuidanceManager.instance.DeregisterGuidance(guidanceIndex);
        }

        public abstract Vector2 GetUIPosition();

        [TestButton]
        public void TestGuidance()
        {
            if (GuidanceManager.instance == null) return;
            GuidanceManager.instance.ReactiveGuidance(guidanceIndex);
        }
    }
}