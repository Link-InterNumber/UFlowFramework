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
        private bool _inited;

        /// <summary>
        /// 关闭引导
        /// </summary>
        public abstract void OnDeExecute();

        private void OnEnable()
        {
            if (!_inited) return;
            GuidanceManager.instance.RegisterGuidance(this);
        }

        private void OnDisable()
        {
            if (!_inited) return;
            GuidanceManager.instance.DeregisterGuidance(guidanceIndex);
        }

        public virtual void OnWidgetEnable()
        {
            _inited = true;
            if(guidanceIndex == 0) return;
            if (gameObject.activeInHierarchy) GuidanceManager.instance.RegisterGuidance(this);
        }

        public virtual void OnWidgetDisable()
        {
            _inited = false;
            if(guidanceIndex == 0) return;
            if (gameObject.activeInHierarchy) GuidanceManager.instance.DeregisterGuidance(guidanceIndex);
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