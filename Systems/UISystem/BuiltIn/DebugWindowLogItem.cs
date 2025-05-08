using System;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class DebugWindowLogItem : ListItem
    {
        public Image typeImg;
        public Text txtContent;
        public Text txtStack;
        public ScrollRect scrollRect;

        public override void UpdateContent(int index, object data)
        {
            base.UpdateContent(index, data);
            var logInfo = (DebugBtn.LogInfo)data;
            switch (logInfo.logType)
            {
                case LogType.Error:
                    typeImg.color = Color.red;
                    break;
                case LogType.Assert:
                    typeImg.color = Color.red;
                    break;
                case LogType.Warning:
                    typeImg.color = Color.yellow;
                    break;
                case LogType.Log:
                    typeImg.color = Color.white;
                    break;
                case LogType.Exception:
                    typeImg.color = Color.red;
                    break;
                default:
                    break;
            }
            txtContent.text = logInfo.condition;
            txtStack.text = logInfo.stacktrace;
            scrollRect.normalizedPosition = Vector2.up;
        }
    }
}