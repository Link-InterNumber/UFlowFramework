using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public static class MonoListUpdateUtils
    {
        private static T GetPrefab<T>(List<T> itemList) where T : MonoBehaviour
        {
            // 优先使用首项；首项已销毁时使用列表中其他有效项。
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i]) return itemList[i];
            }

            Debug.LogError("ListMonoItemUtils requires at least one valid item as a prefab.");
            return null;
        }

        private static T GetOrCreateItem<T>(List<T> itemList, int index, T prefab) where T : MonoBehaviour
        {
            var item = index < itemList.Count ? itemList[index] : null;
            if (!item)
            {
                item = UnityEngine.Object.Instantiate(prefab, prefab.transform.parent);
                if (index < itemList.Count)
                {
                    itemList[index] = item;
                }
                else
                {
                    itemList.Add(item);
                }
            }

            item.gameObject.SetActive(true);
            return item;
        }

        private static void HideItems<T>(List<T> itemList, int startIndex) where T : MonoBehaviour
        {
            for (int i = startIndex; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (item) item.gameObject.SetActive(false);
            }
        }

        public static void Clear<T>(List<T> itemList)
            where T : MonoBehaviour
        {
            if (itemList == null) return;
            HideItems(itemList, 0);
        }

        public static void UpdateList<TData, TComponent>(List<TComponent> itemList, Span<TData> dataList,
            Action<TData, TComponent> updateItemAction)
            where TComponent : MonoBehaviour
        {
            if (itemList == null) throw new ArgumentNullException(nameof(itemList));
            if (dataList.Length == 0)
            {
                Clear(itemList);
                return;
            }

            var prefab = GetPrefab(itemList);
            if (!prefab) return;

            for (int i = 0; i < dataList.Length; i++)
            {
                var item = GetOrCreateItem(itemList, i, prefab);
                updateItemAction?.Invoke(dataList[i], item);
            }

            HideItems(itemList, dataList.Length);
        }

        public static void UpdateList<TData, TComponent>(List<TComponent> itemList, IEnumerable<TData> dataList,
            Action<TData, TComponent> updateItemAction)
            where TComponent : MonoBehaviour
        {
            if (itemList == null) throw new ArgumentNullException(nameof(itemList));
            if (dataList == null)
            {
                Clear(itemList);
                return;
            }

            var index = 0;
            TComponent prefab = null;
            foreach (var data in dataList)
            {
                // 延迟获取模板，空枚举只隐藏现有项。
                if (index == 0)
                {
                    prefab = GetPrefab(itemList);
                    if (!prefab) return;
                }

                var item = GetOrCreateItem(itemList, index, prefab);
                updateItemAction?.Invoke(data, item);
                index++;
            }

            HideItems(itemList, index);
        }

        public static void Foreach<T>(List<T> itemList, Action<T, int> action, bool includeInactive = false)
            where T : MonoBehaviour
        {
            if (itemList == null || action == null) return;
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (!item || (!includeInactive && !item.gameObject.activeSelf)) continue;
                action.Invoke(item, i);
            }
        }

        public static IEnumerable<T> GetAll<T>(List<T> itemList, bool includeInactive = false)
            where T : MonoBehaviour
        {
            if (itemList == null) yield break;
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (!item || (!includeInactive && !item.gameObject.activeSelf)) continue;
                yield return item;
            }
        }

        public static T Find<T>(List<T> itemList, int index, bool includeInactive = false)
            where T : MonoBehaviour
        {
            if (itemList == null || index < 0 || index >= itemList.Count) return default;

            var item = itemList[index];
            if (!item || (!includeInactive && !item.gameObject.activeSelf)) return default;
            return item;
        }

        public static void Show<T>(List<T> itemList)
            where T : MonoBehaviour
        {
            if (itemList == null || itemList.Count <= 0) return;
            var parent = itemList[0]?.transform.parent;
            parent?.gameObject.SetActive(true);
        }

        public static void Hide<T>(List<T> itemList)
            where T : MonoBehaviour
        {
            if (itemList == null || itemList.Count <= 0) return;
            var parent = itemList[0]?.transform.parent;
            parent?.gameObject.SetActive(false);
        }
    }
}