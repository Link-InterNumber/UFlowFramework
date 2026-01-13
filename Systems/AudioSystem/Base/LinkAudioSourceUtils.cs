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
            poolable.gameObject.SetActive(false);
            return poolable;
        }

        private static PoolableObjectPool _linkAudioSourcePool;

        private static void InitAudioSourcePool()
        {
            _linkAudioSourcePool = PoolManager.instance?.Register(Create, 20, 5, PoolManager.PoolGroupName.Effect);
        }

        public static LinkAudioSource Get(AudioPipeline pipeline)
        {
            if (_linkAudioSourcePool == null) InitAudioSourcePool();
            var audioSourceCtrl = _linkAudioSourcePool.Get() as LinkAudioSource;
            if (pipeline.mixCtrl != null)
                audioSourceCtrl.audioSource.outputAudioMixerGroup = pipeline.mixCtrl.audioMixerGroup;
            else
                audioSourceCtrl.audioSource.outputAudioMixerGroup = null;
            audioSourceCtrl.setVolume = pipeline.realVolume;
            audioSourceCtrl.setPitch = pipeline.realPitch;
            audioSourceCtrl.audioSource.mute = pipeline.realMute;
            return audioSourceCtrl;
        }

        public static void DeinitAudioSourcePool()
        {
            PoolManager.instance?.UnRegister<LinkAudioSource>(PoolManager.PoolGroupName.Effect);
            _linkAudioSourcePool = null;
        }
    }
}