using UnityEngine;

namespace PowerCellStudio
{
    public interface IUIWidget
    {
        /// <summary>
        /// 在Window打开时被调用，即使节点的gameObject关闭也会调用
        /// Called when the Window is open, even if the node's gameObject is Inactive
        /// </summary>
        public void OnWidgetEnable();

        /// <summary>
        /// 在Window关闭时被调用，即使节点的gameObject关闭也会调用
        /// Called when the Window is closed, even if the node's gameObject is Inactive
        /// </summary>
        public void OnWidgetDisable();
    }
}