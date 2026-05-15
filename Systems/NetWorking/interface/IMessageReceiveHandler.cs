using System;
using System.Collections;

namespace PowerCellStudio
{
    public interface IMessageReceiveHandler : IDisposable
    {
        public bool invokeOnce { get; }
        public void OnReceived(object message, Type messageType);
    }
}