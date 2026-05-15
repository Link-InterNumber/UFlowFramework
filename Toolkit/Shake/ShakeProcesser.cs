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
        // private List<int> _removeBuffer;

        public static float DefaultDelayDestoryDuration = 10f;

        private void Awake()
        {
            _handles = DictionaryPool<int, ShakeHandle>.Get();
            _addBuffer = ListPool<ShakeHandle>.Get();
            // _removeBuffer = ListPool<int>.Get();
            GameObject.DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            DictionaryPool<int, ShakeHandle>.Release(_handles);
            ListPool<ShakeHandle>.Release(_addBuffer);
            // ListPool<int>.Release(_removeBuffer);
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
            // if (_removeBuffer.Count > 0)
            // {
            //     for (var i = 0; i < _removeBuffer.Count; i++)
            //     {
            //         _handles[_removeBuffer[i]].Cancel();
            //         _handles.Remove(_removeBuffer[i]);
            //     }
            //     _removeBuffer.Clear();
            // }

            if (_addBuffer.Count > 0)
            {
                for (var i = 0; i < _addBuffer.Count; i++)
                {
                    _handles.Add(_addBuffer[i].hashCode, _addBuffer[i]);
                }
                _addBuffer.Clear();
            }
            
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

            if (_handles.Count == 0)
            {
                delayDestoryDuration -= Time.unscaledTime;
                if (delayDestoryDuration <= 0f)
                {
                    GameObject.Destroy(gameObject);
                }
            }
        }
    }
}