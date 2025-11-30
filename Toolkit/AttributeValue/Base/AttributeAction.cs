using System;
using UnityEngine;

namespace PowerCellStudio
{
    public delegate T AttributeValueChange<T>(T currentValue, T originValue);

    [Serializable]
    public class AttributeAction<T>
    {
        [SerializeField] private AttributePriority priority;
        [SerializeField] public string ActionTag;
        // [SerializeField] public string ActionDec;
        [SerializeField] public bool Enabled;

        public AttributeAction(AttributeValueChange<T> newAction, AttributePriority initPriority, string initTag = "")
        {
            priority = initPriority;
            action = newAction;
            ActionTag = initTag;
            Enabled = true;
            // ActionDec = newAction.ToString();
        }

        public AttributePriority Priority => priority;

        private AttributeValueChange<T> action;
        public AttributeValueChange<T> Action => action;

        public void SetEnable(bool enable)
        {
            Enabled = enable;
        }

        public void SetPriority(AttributePriority newValue)
        {
            priority = newValue;
        }

        public void Rebuild(AttributeValueChange<T> newAction, string actionTag = "")
        {
            action = newAction;
            // ActionDec = newAction.ToString();
            if (actionTag == "") return;
            ActionTag = actionTag;
        }
    }
}