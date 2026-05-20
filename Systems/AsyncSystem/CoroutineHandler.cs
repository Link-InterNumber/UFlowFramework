using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    public class CoroutineHandler : AsyncHandlerBase, IPoolable
    {
        private MonoBehaviour _monoBehaviour;

        private Coroutine _coroutine;
        public Coroutine coroutine => _coroutine;

        public LinkPool<IPoolable> LinkPool {get; set; }

        public bool spawned => (!LinkPool?.IsInPool(this)) ?? false;

        public override bool keepWaiting => _coroutine != null;

        public void OnDeSpawn()
        {
            if (_coroutine != null && _monoBehaviour)
            {
                _monoBehaviour.StopCoroutine(_coroutine);
                _coroutine = null;
            }
            _monoBehaviour = null;
            _onComplete = null;
        }

        public void OnSpawn()
        {

        }

        /// <summary>
        /// 不要直接调用这个方法，应该调用DeSpawn方法来释放这个对象
        /// </summary>
        [System.Obsolete("Don't call this method directly, use DeSpawn instead.")]
        public void Dispose()
        {
            LinkPool = null;
            if (_coroutine != null && _monoBehaviour)
            {
                _monoBehaviour.StopCoroutine(_coroutine);
                _coroutine = null;
            }
            _monoBehaviour = null;
            _onComplete = null;
        }

        internal void Assembly(MonoBehaviour monoBehaviour, IEnumerator routine)
        {
            _monoBehaviour = monoBehaviour;
            _coroutine = _monoBehaviour.StartCoroutine(HandleCoroutine(routine));
        }

        private IEnumerator HandleCoroutine(IEnumerator routine)
        {
            yield return routine;
            _onComplete?.Invoke();
            DeSpawn();
        }

        public override void Cancel()
        {
            if (!_monoBehaviour || _coroutine == null) return;
            if (_coroutine != null)
            {
                _monoBehaviour.StopCoroutine(_coroutine);
                _coroutine = null;
            }
            DeSpawn();
        }

        public void DeSpawn()
        {
            if (LinkPool != null && LinkPool.Release(this)) return;
            OnDeSpawn();
            Dispose();
        }
    }
}