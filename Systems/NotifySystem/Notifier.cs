using System;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class Notifier : MonoBehaviour
    {
        public NotifyType notifyType;
        public GameObject redPoint;
        public Text numberTxt;
        public Text valueTxt;

        private void Awake()
        {
            if (!redPoint) redPoint = gameObject;
            NotifyManager.instance.Register(notifyType, OnNotifyChanged);

        }

        private void OnEnable()
        {
            NotifyManager.instance.GetNotifyInfo(notifyType, out bool isOn, out int notifyNum, out int notifyValue);
            OnNotifyChanged(isOn, notifyNum, notifyValue);
        }

        private void OnDestroy()
        {
            NotifyManager.instance.UnRegister(notifyType, OnNotifyChanged);
        }

        private void OnNotifyChanged(bool isOn, int notifyNum, int notifyValue)
        {
            redPoint.SetActive(isOn);
            if (!isOn) return;
            if (numberTxt) numberTxt.text = notifyNum.ToString();
            if (valueTxt) valueTxt.text = notifyValue.ToString();
        }

        public void Init(NotifyType type)
        {
            if(notifyType == type || !gameObject) return;
            NotifyManager.instance.UnRegister(notifyType, OnNotifyChanged);
            notifyType = type;
            NotifyManager.instance.Register(notifyType, OnNotifyChanged);
            if (!redPoint.activeSelf) return;
            NotifyManager.instance.GetNotifyInfo(notifyType, out bool isOn, out int notifyNum, out int notifyValue);
            OnNotifyChanged(isOn, notifyNum, notifyValue);
        }

    }
}