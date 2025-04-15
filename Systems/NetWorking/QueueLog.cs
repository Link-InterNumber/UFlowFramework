namespace GameProtocol
{
    public enum QueueLogLevel
    {
        Info,
        Warning,
        Error
    }
    
    public struct QueueLog
    {
        public QueueLogLevel logLevel;
        public string logMessage;
        public string callStack;
    }
}