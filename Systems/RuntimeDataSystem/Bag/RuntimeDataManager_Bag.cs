
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    [Serializable]
    public class RItem: ICloneT<RItem>
    {
        public int id;
        public int num;
        public RItem Clone()
        {
            return new RItem()
            {
                id = this.id,
                num = this.num
            };
        }
    }

    public partial class RuntimeDataManager
    {
        private sealed class BagData : RuntimeDataDic<int, RItem> { }

        public void InitBag()
        {
            if(GetRuntimeData<BagData>() != null) return;
            var bagData = new BagData();
            bagData.AddListener(OnItemChange);
            AddRuntimeData(bagData);
        }

        public void AddItem(RItem rItem)
        {
            if (rItem == null) return;
            var bag = GetRuntimeData<BagData>();
            var currentNum = bag?.GetData(rItem.id)?.num ?? 0;
            rItem.num = currentNum + rItem.num;
            bag?.ReplaceData(rItem.id, rItem);
        }

        public void RemoveItem(RItem rItem)
        {
            if (rItem == null) return;
            var bag = GetRuntimeData<BagData>();
            var current = bag?.GetData(rItem.id);
            if (current == null) return;
            current.num -= rItem.num;
            current.num = Math.Max(0, current.num);
            bag.ReplaceData(rItem.id, current);
            if (current.num == 0)
                bag.Remove(rItem.id);
        }

        public int GetItemNumber(int id)
        {
            var bag = GetRuntimeData<BagData>();
            return bag?.GetData(id)?.num ?? 0;
        }

        public void AddBagListener(OnRuntimeDataChange<RItem> action)
        {
            GetRuntimeData<BagData>()?.AddListener(action);
        }
        
        public void RemoveBagListener(OnRuntimeDataChange<RItem> action)
        {
            GetRuntimeData<BagData>()?.RemoveListener(action);
        }
        
        public List<RItem> GetAllItems()
        {
            var bag = GetRuntimeData<BagData>();
            return bag?.ToList()?? new List<RItem>();
        }

        private void OnItemChange(RItem oldData, RItem newData)
        {
            ModuleLog<BagUtils>.Log($"item id = {newData.id}, item number = {newData.num}.");
        }
    }
}