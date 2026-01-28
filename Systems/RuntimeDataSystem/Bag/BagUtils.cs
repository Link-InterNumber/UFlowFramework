using System.Linq;

namespace PowerCellStudio
{
    public class BagUtils
    {
        public static int GetItemNum(int id)
        {
            var itemData = RuntimeDataManager.instance.GetData<RItem>(id);
            return itemData.num;
        }

        public static void AddItem(RItem item, params RItem[] items)
        {
            RuntimeDataManager.instance.AddData(item);
            if (items == null) return;
            foreach (var rItem in items)
            {
                RuntimeDataManager.instance.AddData(rItem);
            }
        }

        public static void AddItem(int id, int num)
        {
            var item = new RItem()
            {
                id = id,
                num = num
            };
            RuntimeDataManager.instance.AddData(item);
        }

        public static void RemoveItem(RItem item, params RItem[] items)
        {
            RuntimeDataManager.instance.RemoveData(item);
            if (items == null) return;
            foreach (var rItem in items)
            {
                RuntimeDataManager.instance.RemoveData(rItem);
            }
        }

        public static void RemoveItem(int id, int num)
        {
            var item = new RItem()
            {
                id = id,
                num = num
            };
            RuntimeDataManager.instance.RemoveData(item);
        }

        public static void AddBagListener(OnRuntimeDataChange<RItem> action)
        {
            RuntimeDataManager.instance.AddChangeListener(action);
        }

        public static void RemoveBagListener(OnRuntimeDataChange<RItem> action)
        {
            RuntimeDataManager.instance.RemoveChangeListener(action);
        }

        public static bool IsItemEnough(int id, int needNum)
        {
            var current = GetItemNum(id);
            return current >= needNum;
        }

        public static bool IsItemEnough(RItem item, params RItem[] items)
        {
            if (!IsItemEnough(item.id, item.num)) return false;
            if (items == null) return true;
            return items.All(o => IsItemEnough(o.id, o.num));
        }
    }
}