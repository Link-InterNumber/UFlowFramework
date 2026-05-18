using UnityEngine;

namespace PowerCellStudio
{
    public interface IUIChild : IUIComponent
    {
        internal IUIParent parent { get; set; }
        // internal Canvas canvas { get; set; }
        internal string prefabPath { get; set; }

        /// <summary>
        /// 当父节点UI关闭时，如果子UI是打开状态，则触发该方法
        /// </summary>
        public void OnHide();
    }
}