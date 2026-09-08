using System;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class Notifier : MonoBehaviour
    {
        private Type _notifyType;
        public Type notifyType => _notifyType;
        
        public string typeName;
        public int notifyTypeIndex;
        public GameObject redPoint;
        public Text numberTxt;
        public Text valueTxt;

        private void Awake()
        {
            if (!redPoint) redPoint = gameObject;
            _notifyType = ReflectionUtils.GetTypeByName(typeName);
            if (_notifyType == null || !_notifyType.IsEnum)
            {
                Debug.LogError($"Notifier: Invalid notify type '{typeName}'");
                return;
            }
            NotifyManager.instance.Register(_notifyType, notifyTypeIndex, OnNotifyChanged);
        }

        private void OnEnable()
        {
            if (_notifyType == null) return;
            NotifyManager.instance.GetNotifyInfo(_notifyType, notifyTypeIndex, out bool isOn, out int notifyNum, out int notifyValue);
            OnNotifyChanged(isOn, notifyNum, notifyValue);
        }

        private void OnDestroy()
        {
            if (_notifyType == null) return;
            NotifyManager.instance.UnRegister(_notifyType, notifyTypeIndex, OnNotifyChanged);
        }

        private void OnNotifyChanged(bool isOn, int notifyNum, int notifyValue)
        {
            redPoint.SetActive(isOn);
            if (!isOn) return;
            if (numberTxt) numberTxt.text = notifyNum.ToString();
            if (valueTxt) valueTxt.text = notifyValue.ToString();
        }

        public void Init<T>(T type) where T : Enum
        {
            if (type == null) return;
            var enumType = typeof(T);
            var enumValues = Enum.GetValues(enumType) as T[];
            if (enumValues == null) return;
            
            NotifyManager.instance.UnRegister(_notifyType, notifyTypeIndex, OnNotifyChanged);
            _notifyType = enumType;
            notifyTypeIndex = Array.IndexOf(enumValues, type);
            NotifyManager.instance.Register(_notifyType, notifyTypeIndex, OnNotifyChanged);
            NotifyManager.instance.GetNotifyInfo(_notifyType, notifyTypeIndex, out bool isOn, out int notifyNum, out int notifyValue);
            OnNotifyChanged(isOn, notifyNum, notifyValue);
        }

    }
}