using UnityEngine;

namespace PowerCellStudio
{
    public interface IListItem
    {
        public int itemIndex { get; }
        
        public void SetIndex(int index);

        public IListUpdater itemHolder { get; set; }

        public void UpdateContent(int index, object data);
    }

    public class ListItem : MonoBehaviour, IListItem
    {
        private int _index;
        public int itemIndex => _index;

        public IListUpdater itemHolder {set; get;}
    
        public virtual void UpdateContent(int index, object data)
        {
            _index = index;
        }

        void IListItem.SetIndex(int index)
        {
            _index = index;
        }
    }
}
