using UnityEngine.Audio;

namespace PowerCellStudio
{
    public class AudioMixerGroupCtrl : IAudioMixerGroupCtrl
    {
        public AudioMixerGroupCtrl(AudioMixer audioMixer, AudioMixerGroup audioMixerGroup)
        {
            _audioMixer = audioMixer;
            _audioMixerGroup = audioMixerGroup;
        }

        private AudioMixer _audioMixer;

        private AudioMixerGroup _audioMixerGroup;
        public AudioMixerGroup audioMixerGroup => _audioMixerGroup;

        public float GetFloat(string name)
        {
            return _audioMixer.GetFloat(name, out var result) ? result : 0f;
        }

        public void SetFloat(string name, float value)
        {
            _audioMixer.SetFloat(name, value);
        }

        public void TransitionToSnapshot(string snapshot, float timeToReach)
        {
            _audioMixer.FindSnapshot(snapshot).TransitionTo(timeToReach);
        }

        public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float timeToReach)
        {
            snapshot.TransitionTo(timeToReach);
        }
    }
}