using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class AttributeActionContainer<T>: IEnumerable<AttributeAction<T>>
    {
        [SerializeField] private List<AttributeAction<T>> actionList;
        public int Count => actionList.Count;

        public AttributeActionContainer()
        {
            actionList = new List<AttributeAction<T>>();
        }

        public AttributeActionContainer<T> Clone()
        {
            var cloned = new AttributeActionContainer<T>();
            for (int i = 0; i < actionList.Count; i++)
            {
                cloned.Push(new AttributeAction<T>(actionList[i].Action, actionList[i].Priority, actionList[i].ActionTag));
            }
            return cloned;
        }

        public IEnumerator<AttributeAction<T>> GetEnumerator()
        {
            return EnumAction().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<AttributeAction<T>> EnumAction()
        {
            actionList.Sort((a,b) => a.Priority - b.Priority);
            for (int i = 0; i < actionList.Count; i++)
            {
                if(actionList[i].Enabled)
                    yield return actionList[i];
            }
        }
        
        public AttributeAction<T> Find(Func<AttributeAction<T>, bool> match)
        {
            if (actionList.Count == 0) return null;
            return actionList.FirstOrDefault(match);
        }
        
        public AttributeAction<T>[] GetActions(Func<AttributeAction<T>, bool> match)
        {
            if (actionList.Count == 0) return null;
            return actionList.Where(match).ToArray();
        }
        
        public AttributeAction<T>[] GetActions(string tag)
        {
            if (actionList.Count == 0) return Array.Empty<AttributeAction<T>>();
            return actionList.Where(o=>o.ActionTag == tag).ToArray();
        }
        
        public AttributeAction<T>[] GetActions(AttributePriority priority)
        {
            if (actionList.Count == 0) return Array.Empty<AttributeAction<T>>();
            return actionList.Where(o=>o.Priority == priority).ToArray();
        }

        public AttributeAction<T>[] GetActions(AttributeValueChange<T> action)
        {
            if (actionList.Count == 0) return Array.Empty<AttributeAction<T>>();
            return actionList.Where(o=>o.Action == action).ToArray();
        }

        public AttributeAction<T> Push(AttributeValueChange<T> action, AttributePriority priority, string tag)
        {
            var newAction = new AttributeAction<T>(action, priority, tag);
            actionList.Add(newAction);
            return newAction;
        }
        
        public void Push(AttributeAction<T> action)
        {
            actionList.Add(action);
        }

        public void Pop()
        {
            if (actionList.Count == 0) return;
            actionList.RemoveAt(actionList.Count - 1);
        }
        
        public void Remove(AttributeValueChange<T> action)
        {
            actionList.RemoveAll(o => o.Action == action);
        }

        public void Remove(AttributeAction<T> action)
        {
            actionList.Remove(action);
        }

        public void Remove(string actionTag)
        {
            actionList.RemoveAll(o => o.ActionTag == actionTag);
        }
        
        public void Clear(){actionList.Clear();}
    }
}