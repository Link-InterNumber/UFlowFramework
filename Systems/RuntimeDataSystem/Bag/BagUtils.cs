using System.Linq;

namespace PowerCellStudio
{
    public class BagUtils
    {
        public static int GetItemNum(int id)
        {
            return RuntimeDataManager.instance.GetItemNumber(id);
        }

        public static void AddItem(RItem item, params RItem[] items)
        {
            RuntimeDataManager.instance.AddItem(item);
            if (items == null) return;
            foreach (var rItem in items)
            {
                RuntimeDataManager.instance.AddItem(rItem);
            }
        }

        public static void AddItem(int id, int num)
        {
            var item = new RItem()
            {
                id = id,
                num = num
            };
            RuntimeDataManager.instance.AddItem(item);
        }

        public static void RemoveItem(RItem item, params RItem[] items)
        {
            RuntimeDataManager.instance.RemoveItem(item);
            if (items == null) return;
            foreach (var rItem in items)
            {
                RuntimeDataManager.instance.RemoveItem(rItem);
            }
        }

        public static void RemoveItem(int id, int num)
        {
            var item = new RItem()
            {
                id = id,
                num = num
            };
            RuntimeDataManager.instance.RemoveItem(item);
        }

        public static void AddBagListener(OnRuntimeDataChange<RItem> action)
        {
            RuntimeDataManager.instance.AddBagListener(action);
        }

        public static void RemoveBagListener(OnRuntimeDataChange<RItem> action)
        {
            RuntimeDataManager.instance.RemoveBagListener(action);
        }

        public static bool IsItemEnough(int id, int needNum)
        {
            var current = RuntimeDataManager.instance.GetItemNumber(id);
            return current >= needNum;
        }

        public static bool IsItemEnough(RItem item, params RItem[] items)
        {
            if (item != null && !IsItemEnough(item.id, item.num)) return false;
            if (items == null) return true;
            return items.All(o => o != null && IsItemEnough(o.id, o.num));
        }
    }
}