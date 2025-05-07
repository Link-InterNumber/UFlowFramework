using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.ColorPicker;
using System.Collections;

namespace PowerCellStudio
{
    public class DebugBtn : MonoBehaviour, IDragHandler,IBeginDragHandler, IEndDragHandler
    {
        private Image img;
        private Color _yellow = new Color(1f, 0.92f, 0.016f, 0.5f);
        private Color _red = new Color(1f, 0, 0, 0.5f);
        private int _errorCount;
        private void Awake()
        {
            var btnRect = transform as RectTransform;
            btnRect.localScale = Vector3.one;
            var size = Mathf.Min(UIManager.ScreenSize.x, UIManager.ScreenSize.y) * 0.1f; 
            btnRect.sizeDelta = new Vector2(size, size);
            btnRect.anchorMin = Vector2.one;
            btnRect.anchorMax = Vector2.one;
            
            var safeArea = Screen.safeArea;
            var scale = UIManager.PixelScale;
            btnRect.anchoredPosition = safeArea.max * scale - UIManager.ScreenSize - new Vector2(size/2, size/2);
            
            img = gameObject.AddComponent<Image>();
            img.color = _yellow;
            var debugBtn = gameObject.AddComponent<Button>();
            debugBtn.onClick.AddListener(() =>
            {
                _errorCount = 0;
                img.color = _yellow;
                UIManager.instance.OpenWindow<DebugWindow>();
            });
            logInfos = new List<LogInfo>();
            Application.logMessageReceivedThreaded += OnLogReceived;
        }
        
        protected void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;
            logInfos = null;
        }

        public struct LogInfo
        {
            public LogType logType;
            public string condition;
            public string stacktrace;
        }

        public static List<LogInfo> logInfos;
        public static Action<LogInfo> onLoged;
        
        private void OnLogReceived(string condition, string stacktrace, LogType type)
        {
            var newLog = new LogInfo()
            {
                logType = type,
                condition = condition,
                stacktrace = stacktrace,
            };
            logInfos.Add(newLog);
            onLoged?.Invoke(newLog);
            if (type == LogType.Log || type == LogType.Warning) return;
            _errorCount++;
            img.color = _red;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = UIManager.ScreenPosToUIPos(eventData.position);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            
        }
    }
}