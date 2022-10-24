using LinkFrameWork.Define;
using LinkFrameWork.DesignPatterns;
using Unity.VisualScripting;
using UnityEngine;

// TODO
namespace LinkFrameWork.MonoInstance
{
    public class AudioManager : MonoSingleton<ApplicationManager>
    {
        public AudioSource MusicSource;
        public AudioSource AmbienceSource;
        public AudioSource EffectSource;
        public AudioSource DialogSource;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        private AudioSource GetAudioSource(AudioSourceType type)
        {
            switch (type)
            {
                case AudioSourceType.Music:
                    if (!MusicSource)
                    {
                        var go = new GameObject($"AudioSource_Music");
                        go.transform.SetParent(transform);
                        MusicSource = go.AddComponent<AudioSource>();
                    }

                    return MusicSource;
                case AudioSourceType.Ambience:
                    if (!AmbienceSource)
                    {
                        var go = new GameObject($"AudioSource_Ambience");
                        go.transform.SetParent(transform);
                        AmbienceSource = go.AddComponent<AudioSource>();
                    }

                    return AmbienceSource;
                case AudioSourceType.Effect:
                    if (!EffectSource)
                    {
                        var go = new GameObject($"AudioSource_Effect");
                        go.transform.SetParent(transform);
                        EffectSource = go.AddComponent<AudioSource>();
                    }

                    return EffectSource;
                case AudioSourceType.Dialog:
                    if (!DialogSource)
                    {
                        var go = new GameObject($"AudioSource_Dialog");
                        go.transform.SetParent(transform);
                        DialogSource = go.AddComponent<AudioSource>();
                    }

                    return DialogSource;
                default:
                    return null;
            }
        }

        public void PlayMusic()
        {
            var source = GetAudioSource(AudioSourceType.Music);
        }

        public void PlayEffect()
        {
            var source = GetAudioSource(AudioSourceType.Effect);
        }

        public void PlayEffectOnObj(GameObject obj, bool full3D = true)
        {
            var source = obj.GetOrAddComponent<AudioSource>();
            source.spatialBlend = full3D ? 1 : 0;
        }

        public void PlayAmbience()
        {
            var source = GetAudioSource(AudioSourceType.Ambience);
        }

        public void PlayDialog()
        {
            var source = GetAudioSource(AudioSourceType.Dialog);
        }

        public void PlayDialogOnObj(GameObject obj, bool full3D = true)
        {
            var source = obj.GetOrAddComponent<AudioSource>();
            source.spatialBlend = full3D ? 1 : 0;
        }
    }
}