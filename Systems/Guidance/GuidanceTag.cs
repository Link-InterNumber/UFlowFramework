using System;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class GuidanceTag : MonoBehaviour
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

        protected virtual void OnEnable()
        {
            if(guidanceIndex == 0) return;
            GuidanceManager.instance.RegisterGuidance(this);
        }

        protected virtual void OnDisable()
        {
            GuidanceManager.instance.DeregisterGuidance(guidanceIndex);
        }

        public abstract Vector2 GetUIPosition();
    }
}