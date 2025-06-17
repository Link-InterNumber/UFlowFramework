using PowerCellStudio;
using System.Collections.Generic;

namespace GameProtocol
{
    public partial class NetClientManager
    {
        private Queue<QueueLog> _logQueue = new Queue<QueueLog>();

        private void AppendLog(QueueLogLevel logLevel, string message)
        {
            var log = new QueueLog()
            {
                logLevel = logLevel,
                logMessage = message
            };
            _logQueue.Enqueue(log);
        }

        private void DisplayLog()
        {
            while (_logQueue.Count > 0)
            {
                var log = _logQueue.Dequeue();
                switch (log.logLevel)
                {
                    case QueueLogLevel.Info:
                        NetWorkLog.Log(log.logMessage);
                        break;
                    case QueueLogLevel.Warning:
                        NetWorkLog.LogWarning(log.logMessage);
                        break;
                    case QueueLogLevel.Error:
                        NetWorkLog.LogError(log.logMessage);
                        break;
                }
            }
        }
    }
}
