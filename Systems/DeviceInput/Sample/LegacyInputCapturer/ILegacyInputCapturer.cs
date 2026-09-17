using System;

namespace UFlowFramework.Sample
{
    public interface ILegacyInputCapturer<TKey> : IDisposable
    {
        bool TryGetEvent(out InputEvent<TKey> inputEvent);
    }
}