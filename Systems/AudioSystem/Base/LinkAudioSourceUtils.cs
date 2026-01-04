using UnityEngine;

namespace PowerCellStudio
{
    public class LinkAudioSourceUtils
    {
        private static LinkAudioSource Create()
        {
            var obj = new GameObject("LinkAudioSource");
            var poolable = obj.AddComponent<LinkAudioSource>();
            poolable.transform.SetParent(AudioManager.instance.transform);
            return poolable;
        }

        private static PoolableObjectPool _LinkAudioSourcepool;

        private static void InitAudioSourcePool()
        {
            _LinkAudioSourcepool = PoolManager.instance?.Register(Create, 20, 5, PoolManager.PoolGroupName.Effect);
        }

        public static LinkAudioSource Get()
        {
            if (_LinkAudioSourcepool == null) InitAudioSourcePool();
            return _LinkAudioSourcepool.Get() as LinkAudioSource;
        }

        public static void DeinitAudioSourcePool()
        {
            PoolManager.instance?.UnRegister<PoolableAudioSource>(PoolManager.PoolGroupName.Effect);
            _LinkAudioSourcepool = null;
        }
    }
}