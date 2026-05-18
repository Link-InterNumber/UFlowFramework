using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class ShakeProcesser : MonoBehaviour
    { 
        private Dictionary<int, ShakeHandle> _handles;
        private List<ShakeHandle> _addBuffer;
        public float delayDestoryDuration = 10f;

        public static float DefaultDelayDestoryDuration = 10f;

        private void Awake()
        {
            _handles = DictionaryPool<int, ShakeHandle>.Get();
            _addBuffer = ListPool<ShakeHandle>.Get();
            GameObject.DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            DictionaryPool<int, ShakeHandle>.Release(_handles);
            ListPool<ShakeHandle>.Release(_addBuffer);
        }

        public void PushHandle(ShakeHandle handle)
        {
            if (_handles.TryGetValue(handle.hashCode, out var currentHandle))
            {
                currentHandle.Merge(handle);
            }
            else
            {
                _addBuffer.Add(handle);
            }
            delayDestoryDuration = DefaultDelayDestoryDuration;
        }

        private void Update()
        {
            if (_addBuffer.Count > 0)
            {
                for (var i = 0; i < _addBuffer.Count; i++)
                {
                    _handles.Add(_addBuffer[i].hashCode, _addBuffer[i]);
                }
                _addBuffer.Clear();
            }

            if (_handles.Count == 0)
            {
                delayDestoryDuration -= Time.unscaledTime;
                if (delayDestoryDuration <= 0f)
                {
                    GameObject.Destroy(gameObject);
                }
            }
            else
            {
                var tempList = ListPool<int>.Get();
                foreach (var (hashCode, handler) in _handles)
                {
                    if (handler.isDone)
                        tempList.Add(hashCode);
                    else
                    {
                        var dt = handler.isUnscaleTime ? Time.unscaledTime : Time.time;
                        handler.Process(dt);
                    }
                }
                if (tempList.Count > 0)
                {
                    for (var i = 0; i < tempList.Count; i++)
                    {
                        _handles[tempList[i]].Cancel();
                        _handles.Remove(tempList[i]);
                    }
                }
                ListPool<int>.Release(tempList);
            }
        }
    }
}