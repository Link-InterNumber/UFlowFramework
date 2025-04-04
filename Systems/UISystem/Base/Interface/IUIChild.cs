using UnityEngine;

namespace PowerCellStudio
{
    public interface IUIChild : IUIComponent
    {
        internal IUIParent parent { get; set; }
        // internal Canvas canvas { get; set; }
        internal string prefabPath { get; set; }
    }
}