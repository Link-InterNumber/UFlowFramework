using UnityEngine;

namespace PowerCellStudio
{
    public class SchedulerTask : AsyncHandlerBase, IPoolable
    {
        public bool byFrame;
        public float delayTime;
        public int delayFrames;

        public int startFrame;
        public float startTime;

        public bool ignoreTimeScale;
        public System.Action action;

        private bool _cancelled;
        public bool cancelled => _cancelled;

        public override bool keepWaiting => CanWait();

        public LinkPool<IPoolable> LinkPool { get; set; }

        private bool CanWait()
        {
            if (_cancelled) return false;
            if (byFrame)
            {
                return Time.frameCount - startFrame < delayFrames;
            }
            else
            {
                float elapsedTime = ignoreTimeScale ? Time.unscaledTime - startTime : Time.time - startTime;
                return elapsedTime < delayTime;
            }
        }

        public override void Cancel()
        {
            _cancelled = true;
        }

        public void OnSpawn()
        {
            _cancelled = false;
        }

        public void DeSpawn()
        {
            if (LinkPool != null && LinkPool.Release(this))
            {
                return;
            }
            OnDeSpawn();
            Dispose();
        }

        public void OnDeSpawn()
        {
            action = null;
        }

        public void Dispose()
        {
            action = null;
        }
    }
}