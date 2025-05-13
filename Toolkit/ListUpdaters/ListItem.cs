using UnityEngine;

namespace PowerCellStudio
{
    public interface IListItem
    {
        public int itemIndex { get; }
        
        public void SetIndex(int index);

        public IListUpdater itemHolder { get;}

        public void UpdateContent(int index, object data, IListUpdater holder);
    }

    public class ListItem : MonoBehaviour, IListItem
    {
        private int _index;
        public int itemIndex => _index;

        public IListUpdater itemHolder {private set; get;}
    
        public virtual void UpdateContent(int index, object data, IListUpdater holder)
        {
            _index = index;
            itemHolder = holder;
        }

        void IListItem.SetIndex(int index)
        {
            _index = index;
        }
    }
}
