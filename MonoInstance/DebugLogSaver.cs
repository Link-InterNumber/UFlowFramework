using System;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class DebugLogSaver: MonoBehaviour
    {
        public DebugSave saveLog;
        private bool _isNew = false;
        private StringBuilder _stringBuilder;
        private float _timeout = 1f;
        private int _curHour;
        
        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Init();
        }

        public void Init()
        {
            saveLog = new DebugSave();
            Application.logMessageReceivedThreaded += OnLogReceived;
            var time = DateTime.Now;
            _curHour = time.Hour;
        }

        protected void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;
        }

        private void OnLogReceived(string condition, string stacktrace, LogType type)
        {
            if(type  == LogType.Log || type == LogType.Warning) return;
            if (_stringBuilder == null) _stringBuilder = new StringBuilder();
            if (saveLog == null) saveLog = new DebugSave();
            _stringBuilder.Append(saveLog.content);
            _stringBuilder.Append(condition);
            _stringBuilder.Append("\n");
            _stringBuilder.Append(stacktrace);
            _stringBuilder.Append("\n");
            saveLog.content = _stringBuilder.ToString();
            _stringBuilder.Clear();
            _isNew = true;
        }

        private void LateUpdate()
        {
            if(!_isNew) return;
            _timeout -= Time.unscaledDeltaTime;
            if(_timeout > 0) return;
            var time = DateTime.Now;
            PlayerDataUtils.SaveDebugLog($"{time.Year}{time.Month}{time.Day}{time.Hour}", saveLog);
            if (_curHour != time.Hour)
            {
                saveLog.content = "";
                _curHour = time.Hour;
            }
            _isNew = false;
            _timeout = 1f;
        }
    }
}