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

        public static LinkAudioSource Get(AudioPipeline pipeline)
        {
            if (_LinkAudioSourcepool == null) InitAudioSourcePool();
            var audioSourceCtrl = _LinkAudioSourcepool.Get() as LinkAudioSource;
            if (pipeline.mixCtrl != null)
                audioSourceCtrl.audioSource.outputAudioMixerGroup = pipeline.mixCtrl.audioMixerGroup;
            audioSourceCtrl.setVolume = pipeline.realVolume;
            audioSourceCtrl.setPitch = pipeline.realPitch;
            audioSourceCtrl.audioSource.mute = pipeline.realMute;
            return audioSourceCtrl;
        }

        public static void DeinitAudioSourcePool()
        {
            PoolManager.instance?.UnRegister<PoolableAudioSource>(PoolManager.PoolGroupName.Effect);
            _LinkAudioSourcepool = null;
        }
    }
}