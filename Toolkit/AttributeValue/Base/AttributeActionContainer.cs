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

        public AttributeAction<T>[] GetActions(Func<T, T, T> action)
        {
            if (actionList.Count == 0) return Array.Empty<AttributeAction<T>>();
            return actionList.Where(o=>o.Action == action).ToArray();
        }

        public AttributeAction<T> Push(Func<T, T, T> action, AttributePriority priority, string tag)
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
            if(actionList.Count == 0) return;
            actionList.RemoveAt(actionList.Count - 1);
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

    public enum AttributePriority
    {
        First = 0,
        Second,
        Third,
        Fourth,
        Fifth
    }

    [Serializable]
    public class AttributeAction<T>
    {
        [SerializeField] private AttributePriority priority;
        [SerializeField] public string ActionTag;
        // [SerializeField] public string ActionDec;
        [SerializeField] public bool Enabled;

        public AttributeAction(Func<T, T, T> newAction, AttributePriority initPriority, string initTag = "")
        {
            priority = initPriority;
            action = newAction;
            ActionTag = initTag;
            Enabled = true;
            // ActionDec = newAction.ToString();
        }

        public AttributePriority Priority => priority;

        private Func<T, T, T> action;
        public Func<T, T, T> Action => action;

        public void SetEnable(bool enable)
        {
            Enabled = enable;
        }

        public void SetPriority(AttributePriority newValue)
        {
            priority = newValue;
        }

        public void Rebuild(Func<T, T, T> newAction, string actionTag = "")
        {
            action = newAction;
            // ActionDec = newAction.ToString();
            if(actionTag == "") return;
            ActionTag = actionTag;
        }
    }
}