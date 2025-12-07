using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class IntervalTaskRunner : MonoSingleton<IntervalTaskRunner>
    {
        private class IntervalTask
        {
            public float interval;
            public float elapsedTime;
            public System.Action action;
        }

        private readonly LinkedList<IntervalTask> _tasks = new LinkedList<IntervalTask>();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _tasks.Clear();
        }

        public void RegisterTask(float interval, System.Action action)
        {
            if (interval <= 0 || action == null) return;
            _tasks.AddLast(new IntervalTask()
            {
                interval = interval,
                elapsedTime = 0f,
                action = action
            });
        }

        public void UnregisterTask(System.Action action)
        {
            var node = _tasks.First;
            while (node != null)
            {
                var next = node.Next;
                if (node.Value.action == action)
                {
                    _tasks.Remove(node);
                }
                node = next;
            }
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            if (_tasks.Count == 0) return;
            var node = _tasks.First;
            while (node != null)
            {
                var next = node.Next;
                var task = node.Value;

                task.elapsedTime += deltaTime;
                if (task.elapsedTime >= task.interval)
                {
                    task.action?.Invoke();
                    task.elapsedTime = 0f;
                }

                node = next;
            }
        }
    }
}