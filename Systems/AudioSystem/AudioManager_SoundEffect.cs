using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private struct AudioRequest
        {
            public AudioSourceType audioType;
            public Vector3 position;
            public string clipName;
            public float requestTime;
            public float removeTime;
            public bool attachToGameObject;
            public bool full3D;
            public GameObject attachGameObject;
        }
        
        // 队列和集合用于管理音频请求
        private Queue<AudioRequest> _audioRequests;
        private List<AudioRequest> _onGoingRequests;
        private HashSet<string> _onGoingRequestSet;

        // 各种音效音量和静音状态
        private float _effectVolume = 1f;
        private float _effectMaxVolume = 0.8f;
        private float _UIEffectVolume = 1f;
        private float _UIEffectMaxVolume = 0.8f;
        private bool _muteEffect = false;
        private bool _muteUIEffect = false;

        // 各音效的加载器和音源池
        private IAssetLoader _3DEffectLoader;
        private PoolableObjectPool _poolAudioSource;

        /// <summary>
        /// 效果音之间的间隔时间。
        /// Interval time between effect sounds.
        /// </summary>
        public float effectIntervalTime = 0.1f;

        private void InitEffectPlayer()
        {
            if (_3DEffectLoader == null)
            {
                _3DEffectLoader = AssetUtils.SpawnLoader("3DEffectLoader");
            }
            _poolAudioSource = PoolManager.instance.Register<PoolableAudioSource>(PoolableAudioSource.Create, 20, 5, PoolManager.PoolGroupName.Effect);
            _onGoingRequests = new List<AudioRequest>();
            _audioRequests = new Queue<AudioRequest>();
            _onGoingRequestSet = new HashSet<string>();
        }

        private void DeinitEffectPlayer()
        {
            PoolManager.instance?.UnRegister<PoolableAudioSource>(PoolManager.PoolGroupName.Effect);
            _onGoingRequests?.Clear(); 
            _audioRequests?.Clear();
            _onGoingRequestSet?.Clear();
            _onGoingRequests = null;
            _audioRequests = null;
            _onGoingRequestSet = null;
            AssetUtils.DeSpawnLoader(_3DEffectLoader);
            _3DEffectLoader = null;
        }

        /// <summary>
        /// 请求播放效果音。
        /// Request to play an effect sound.
        /// </summary>
        /// <param name="clipRef">音频剪辑引用 / Audio clip reference</param>
        /// <param name="onUI">音效是否为UI音效 / Whether the effect is a UI effect</param>
        /// <param name="attached">附加的游戏对象 / Game object to attach</param>
        /// <param name="position">音效位置 / Position of the effect</param>
        /// <param name="full3D">音效是否为全3D / Whether the effect is full 3D</param>
        public void RequestPlayEffect(string clipRef, bool onUI, GameObject attached, Vector3 position, bool full3D)
        {
            if (!onUI && _muteEffect) return;
            if (onUI && _muteUIEffect) return;
            var currentTime = Time.unscaledTime;
            var newQuest = new AudioRequest()
            {
                audioType = onUI ? AudioSourceType.SFXUI : AudioSourceType.SFX3D,
                clipName = clipRef,
                requestTime = currentTime,
                removeTime = currentTime + effectIntervalTime,
                attachToGameObject = attached != null,
                attachGameObject =  attached,
                full3D = !onUI && full3D,
                position = position,
            };
            _audioRequests.Enqueue(newQuest);          
        }

        private void UpdateAudioRequest()
        {
            if(_onGoingRequests == null || _audioRequests == null) return;
            var currentTime = Time.unscaledTime;

            _onGoingRequests.RemoveAll(o =>
            {
                if (currentTime >= o.removeTime)
                {
                    _onGoingRequestSet.Remove(o.clipName);
                    return true;
                }
                return false;
            });

            while (_audioRequests.Count > 0)
            {
                var poped = _audioRequests.Dequeue();
                if (_onGoingRequestSet.Contains(poped.clipName)) continue;
                _onGoingRequestSet.Add(poped.clipName);
                _onGoingRequests.Add(poped);

                if (poped.audioType == AudioSourceType.SFX3D)
                {
                    if (poped.attachToGameObject)
                        Play3DEffect(poped.clipName, poped.attachGameObject, poped.full3D);
                    else
                        Play3DEffect(poped.clipName, poped.position, poped.full3D);
                }
                else if (poped.audioType == AudioSourceType.SFXUI)
                {
                    if (poped.attachToGameObject)
                        PlayUIEffect(poped.clipName, poped.attachGameObject);
                    else
                        PlayUIEffect(poped.clipName);
                }
            }
        }

        private void Play3DEffect(string clipRef, GameObject attachedGameObject, bool full3D = true)
        {
            if (_muteEffect) return;
            if (string.IsNullOrEmpty(clipRef) || !attachedGameObject) return;
            var audioSource = attachedGameObject.AddComponent<PoolableAudioSource>();
            audioSource.OnSpawn();
            audioSource.AudioSourceCom.spatialBlend = full3D ? 1f : 0f; 
            audioSource.SetVolume(_effectMaxVolume * _effectVolume);
            audioSource.Play(clipRef, _3DEffectLoader, false);
        }

        private void Play3DEffect(string clipRef, Vector3 pos, bool full3D = true)
        {
            if (_muteEffect) return;
            if (string.IsNullOrEmpty(clipRef)) return;
            var audioSource = _poolAudioSource.Get() as PoolableAudioSource;
            audioSource.AudioSourceCom.spatialBlend = full3D ? 1f : 0f; 
            audioSource.transform.position = full3D ? pos : transform.position;
            audioSource.SetVolume(_effectMaxVolume * _effectVolume);
            audioSource.Play(clipRef, _3DEffectLoader, false);
        }

        private void PlayUIEffect(string clipRef, GameObject attachedGameObject)
        {
            if (_muteUIEffect) return;
            if (string.IsNullOrEmpty(clipRef) || !attachedGameObject) return;
            var audioSource = attachedGameObject.AddComponent<PoolableAudioSource>();
            audioSource.OnSpawn();
            audioSource.AudioSourceCom.spatialBlend = 0f; 
            audioSource.SetVolume(_UIEffectMaxVolume * _UIEffectVolume);
            audioSource.Play(clipRef, _3DEffectLoader, false);
        }

        private void PlayUIEffect(string clipRef)
        {
            if (_muteUIEffect) return;
            if (string.IsNullOrEmpty(clipRef)) return;
            var audioSource = _poolAudioSource.Get() as PoolableAudioSource;
            audioSource.AudioSourceCom.spatialBlend = 0f; 
            audioSource.transform.position = transform.position;
            audioSource.SetVolume(_UIEffectMaxVolume * _UIEffectVolume);
            audioSource.Play(clipRef, _3DEffectLoader, false);
        }
    }
}