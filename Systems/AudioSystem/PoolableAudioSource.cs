using UnityEngine;

namespace PowerCellStudio
{
    public class PoolableAudioSource : MonoBehaviour, IPoolable
    {
        private AudioSource _audioSource;
        private IAssetLoader _assetLoader;
        private string _clipRef;

        public AudioSource AudioSourceCom
        {
            get
            {
                if (_audioSource) return _audioSource;
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                return _audioSource;
            }
        }

        private bool _waitForDespawn;
        public bool Spawned { get; set; }
        public long SpawnIndex { get; set; }

        public static PoolableAudioSource Create()
        {
            var obj = new GameObject("PoolableAudioSource");
            var poolable = obj.AddComponent<PoolableAudioSource>();
            poolable.SpawnIndex = IndexGetter.instance.Get<PoolableAudioSource>();
            poolable.transform.SetParent(PoolManager.instance.GetGroupRoot(PoolManager.PoolGroupName.Effect));
            return poolable;
        }

        public LinkPool<IPoolable> LinkPool { get; set; }

        public void OnSpawn()
        {
            if (!_audioSource)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            gameObject.SetActive(true);
            Spawned = true;
            _waitForDespawn = false;
        }

        public void DeSpawn()
        {
            _waitForDespawn = false;
            if(LinkPool != null) LinkPool.Release(this);
            else
            {
                OnDeSpawn();
                Dispose();
            }
        }

        public void OnDeSpawn()
        {
            if(_assetLoader != null) _assetLoader.Release(_clipRef);
            _assetLoader = null;
            _clipRef = null;
            gameObject.SetActive(false);
            Spawned = false;
        }

        public void Dispose()
        {
            GameObject.Destroy(_audioSource);
            GameObject.Destroy(this);
        }

        private void Update()
        {
            if (!_audioSource) return;
            if (_audioSource.isPlaying && !_waitForDespawn) _waitForDespawn = true;
            if(!_waitForDespawn) return;
            if(!_audioSource.isPlaying) DeSpawn();
        }

        public void Play(string clipRef, IAssetLoader assetLoader, bool isLoop)
        {
            if(_assetLoader != null) _assetLoader.Release(_clipRef);
            _clipRef = clipRef;
            _assetLoader = assetLoader;
            _audioSource.loop = isLoop;
            _assetLoader.LoadAsync<AudioClip>(clipRef, OnLoadedAudioClip);
        }

        private void OnLoadedAudioClip(AudioClip obj)
        {
            if(_audioSource.loop)
            {
                _audioSource.clip = obj;
                _audioSource.Play();
            }
            else _audioSource.PlayOneShot(obj);
        }
        
        public void SetVolume(float volume)
        {
            if (_audioSource) _audioSource.volume = Mathf.Clamp01(volume);
        }
    }
}