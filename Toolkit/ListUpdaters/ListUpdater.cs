using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public delegate void OnItemInteraction(IListItem item, int index, object passData);

    public interface IListUpdater
    {
        /// <summary>
        /// 触发注册的子节点逻辑
        /// </summary>
        /// <param name="item">子节点</param>
        /// <param name="passData">传递的数据</param>
        public void ItemInteraction(IListItem item, object passData);

        /// <summary>
        /// 将数据列表传入并刷新列表
        /// </summary>
        /// <param name="data">列表数据</param>
        /// <param name="destroyUnused">是则删除多余节点，否则只是隐藏多余节点</param>
        public void UpdateList(IList data, bool destroyUnused = false);

        /// <summary>
        /// 更新索引位上子节点
        /// </summary>
        /// <param name="index">索引位</param>
        /// <param name="data">传入数据</param>
        public void UpdateItem(int index, object data);

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="index">索引位</param>
        /// <param name="data">传入数据</param>
        public void AddItem(int index, object data);
        
        /// <summary>
        /// 移除子节点
        /// </summary>
        /// <param name="index">索引位</param>
        public void RemoveItem(int index);

        /// <summary>
        /// 隐藏所有子节点
        /// </summary>
        public void Clear();
    }

    public class ListUpdater : MonoBehaviour, IListUpdater
    {
        private GameObject _prefab;
        
        public event OnItemInteraction onItemInteraction;

        public void ItemInteraction(IListItem item, object passData)
        {
            if(item == null) return;
            onItemInteraction?.Invoke(item, item.itemIndex, passData);
        }

        public int count
        {
            get
            {
                var c = 0;
                foreach (Transform o in transform)
                {
                    if(o.gameObject.activeSelf) c++;
                }
                return c;
            }
        }
        
        private GameObject GetPrefab()
        {
            if (!_prefab && transform.childCount > 0)
            {
                _prefab = transform.GetChild(0).gameObject;
            }
            return _prefab;
        }

        public void UpdateListWithInterval(IList data, float interval, bool destroyUnused = false)
        {
            if(interval <= 0)
            {
                UpdateList(data, destroyUnused);
                return;
            }
            if (data == null || !GetPrefab()) return;
            if(_updateCoroutine != null) ApplicationManager.instance.StopCoroutine(_updateCoroutine);
            if (destroyUnused)
            {
                var toDestroy = new List<GameObject>();
                for (var i = data.Count; i < transform.childCount; i++)
                {
                    toDestroy.Add(transform.GetChild(i).gameObject);
                }
                foreach (var go in toDestroy)
                {
                    GameObject.Destroy(go);
                }
            }
            else
            {
                for (var i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }
            _updateCoroutine = ApplicationManager.instance.StartCoroutine(UpdateListWithIntervalHandler(data, interval));

        }

        private Coroutine _updateCoroutine;
        private IEnumerator UpdateListWithIntervalHandler(IList data, float interval)
        {
            for (var i = 0; i < data.Count; i++)
            {
                if(transform.childCount <= i)
                {
                    var go = Instantiate(_prefab, transform);
                    go.SetActive(true);
                }
                var item = transform.GetChild(i).GetComponent<IListItem>();
                if (item == null) continue;
                var o = data[i];
                item.itemHolder = this;
                item.UpdateContent(i, o);
                yield return new WaitForSeconds(interval);;
            }
            _updateCoroutine = null;
        }
        
        public void UpdateList(IList data, bool destroyUnused = false)
        {
            if (data == null || !GetPrefab()) return;
            for (var i = 0; i < data.Count; i++)
            {
                GameObject go;
                if(transform.childCount <= i)
                {
                    go = Instantiate(_prefab, transform);
                }
                else
                {
                    go = transform.GetChild(i).gameObject;
                }
                go.SetActive(true);
                var item = go.GetComponent<IListItem>();
                if (item == null) continue;
                var o = data[i];
                item.itemHolder = this;
                item.UpdateContent(i, o);
            }

            if (destroyUnused)
            {
                var toDestroy = new List<GameObject>();
                for (var i = data.Count; i < transform.childCount; i++)
                {
                    toDestroy.Add(transform.GetChild(i).gameObject);
                }
                foreach (var go in toDestroy)
                {
                    GameObject.Destroy(go);
                }
            }
            else
            {
                for (var i = data.Count; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// 隐藏所有子节点
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 通过索引获取子节点
        /// </summary>
        /// <param name="index">索引</param>
        /// <typeparam name="T">MonoBehaviour, IListItem</typeparam>
        /// <returns></returns>
        public T GetItem<T>(int index) 
            where T :MonoBehaviour
        {
            if (index < 0 || index >= transform.childCount) return null;
            return transform.GetChild(index).GetComponent<T>();
        }
        
        public void UpdateItem(int index, object data)
        {
            if (index < 0 || index >= transform.childCount) return;
            var item = transform.GetChild(index).GetComponent<IListItem>();
            if (item == null) return;
            item.itemHolder = this;
            item.UpdateContent(index, data);
        }

        public void AddItem(int index, object data)
        {
            if (index < 0 ) return;
            // 查找第一个未使用的节点
            var usedIndex = -1;
            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf) continue;
                usedIndex = i;
                break;
            }
            // 如果没有未使用的节点，则创建一个新的节点
            if (usedIndex == -1)
            {
                var go = Instantiate(_prefab, transform);
                var realIndex = Mathf.Min(index, transform.childCount - 1);
                go.transform.SetSiblingIndex(realIndex);
                go.SetActive(true);
                var item = go.GetComponent<IListItem>();
                if (item == null) return;
                item.itemHolder = this;
                item.UpdateContent(realIndex, data);
                
                for (int i = index; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if(!child.gameObject.activeSelf) break;
                    var item2 = child.GetComponent<IListItem>();
                    if (item2 == null) continue;
                    item2.SetIndex(i);
                }
                return;
            }
            // 如果有未使用的节点，则将其设置为使用状态
            var itemGo = transform.GetChild(usedIndex);
            itemGo.gameObject.SetActive(true);
            itemGo.SetSiblingIndex(Mathf.Min(index, usedIndex));
            var item1 = itemGo.GetComponent<IListItem>();
            if (item1 == null) return;
            item1.itemHolder = this;
            item1.UpdateContent(Mathf.Min(index, usedIndex), data);
            for (int i = index; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if(!child.gameObject.activeSelf) break;
                var item2 = child.GetComponent<IListItem>();
                if (item2 == null) continue;
                item2.SetIndex(i);
            }
        }

        public void RemoveItem(int index)
        {
            if (index < 0 ) return;
            if (index >= transform.childCount) return;
            var go = transform.GetChild(index);
            go.gameObject.SetActive(false);
            go.SetAsLastSibling();
            for (int i = index; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if(!child.gameObject.activeSelf) break;
                var item2 = child.GetComponent<IListItem>();
                if (item2 == null) continue;
                item2.SetIndex(i);
            }
        }

        /// <summary>
        /// 寻找符合条件的子点
        /// </summary>
        /// <param name="match">匹配方法</param>
        /// <typeparam name="T">MonoBehaviour, IListItem</typeparam>
        /// <returns></returns>
        public T FindItem<T>(Func<T, bool> match) 
            where T : MonoBehaviour, IListItem
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var item = transform.GetChild(i).GetComponent<T>();
                if (!item || !match.Invoke(item)) continue;
                return item;
            }
            return null;
        }
        
        /// <summary>
        /// 对所有
        /// </summary>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        public void ForEachItem<T>(Action<T> action) 
            where T : MonoBehaviour, IListItem
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var item = transform.GetChild(i).GetComponent<T>();
                if (!item) continue;
                action.Invoke(item);
            }
        }
        
        /// <summary>
        /// 删除无用节点
        /// </summary>
        public void RemoveUnusedItems()
        {
            for (var i = transform.childCount - 1; i > 0; i--)
            {
                if (!transform.GetChild(i).gameObject.activeSelf)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
            }
        }
    }
}