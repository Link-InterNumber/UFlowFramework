using System.Collections.Generic;

namespace PowerCellStudio
{
    internal class NotifyNode
    {
        public int index;
        public bool isOn;
        public int notifyNumber;
        public int notifyValue;
        public int parent;
        public HashSet<int> children;
            
        public OtherGroupNode otherGroupParent;
        public List<OtherGroupNode> otherGroupChildren;
            
        public event OnNotifyChange onNotifyChange;

        public void Notify()
        {
            onNotifyChange?.Invoke(isOn, notifyNumber, notifyValue);
        }

        public void ClearNotify()
        {
            onNotifyChange = null;
        }
    }
}