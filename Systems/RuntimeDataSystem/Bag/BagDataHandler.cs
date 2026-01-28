
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    [Serializable]
    public struct RItem: ICloneT<RItem>
    {
        public int id;
        public int num;
        public RItem Clone()
        {
            return new RItem
            {
                id = this.id,
                num = this.num
            };
        }
    }

    public class BagDataHandler: IRuntimeDataHandler<RItem>
    {
        private sealed class BagData : RuntimeDataManager.RuntimeDataDic<int, RItem> { }

        public void InitData()
        {
            if(RuntimeDataManager.instance.GetRuntimeStorage<BagData>() != null) return;
            var bagData = new BagData();
            RuntimeDataManager.instance.AddRuntimeStorage(bagData);
        }

        public void AddData(RItem rItem)
        {
            var bag = RuntimeDataManager.instance.GetRuntimeStorage<BagData>();
            var currentNum = bag?.GetData(rItem.id).num ?? 0;
            rItem.num = currentNum + rItem.num;
            bag?.ReplaceData(rItem.id, rItem);
        }

        public void RemoveData(RItem rItem)
        {
            var bag = RuntimeDataManager.instance.GetRuntimeStorage<BagData>();
            if (bag == null) return;
            var current = bag.GetData(rItem.id);
            current.num -= rItem.num;
            current.num = Math.Max(0, current.num);
            bag.ReplaceData(rItem.id, current);
            if (current.num == 0)
                bag.Remove(rItem.id);
        }
        
        public int GetKey(RItem rItem)
        {
            return rItem.id;
        }

        public RItem GetData(int key)
        {
            var bag = RuntimeDataManager.instance.GetRuntimeStorage<BagData>();
            if (bag == null) return default(RItem);
            var current = bag.GetData(key);
            return current;
        }

        public void AddListener(OnRuntimeDataChange<RItem> action)
        {
            RuntimeDataManager.instance.GetRuntimeStorage<BagData>()?.AddListener(action);
        }
        
        public void RemoveListener(OnRuntimeDataChange<RItem> action)
        {
            RuntimeDataManager.instance.GetRuntimeStorage<BagData>()?.RemoveListener(action);
        }
        
        public List<RItem> GetAllData()
        {
            var bag = RuntimeDataManager.instance.GetRuntimeStorage<BagData>();
            return bag?.ToList()?? new List<RItem>();
        }
    }
}