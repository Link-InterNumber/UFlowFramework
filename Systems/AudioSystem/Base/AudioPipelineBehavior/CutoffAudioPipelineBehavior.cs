using UnityEngine.Audio;

namespace PowerCellStudio
{
    public class CutoffAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public CutoffAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
            _audioSourceCtrl.autoDespawn = false;
            _audioSourceCtrl.onFree.AddListener(OnAudioSourceFree);
            _audioSourceCtrl.gameObject.SetActive(false);
        }
        
        public void Dispose()
        {
            _audioSourceCtrl?.DeSpawn();
            _audioSourceCtrl = null;
        }

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }

        private LinkAudioSource _audioSourceCtrl;

        public void ReceiveRequest(AudioRequest request)
        {
            if (!_audioSourceCtrl)
            {
                _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
                _audioSourceCtrl.autoDespawn = false;
                _audioSourceCtrl.onFree.AddListener(OnAudioSourceFree);
            }
            _audioSourceCtrl.gameObject.SetActive(true);
            _audioSourceCtrl.Play(request);
        }
        
        public void RemoveRequest(string clipPath)
        {
            if (_audioSourceCtrl == null || _audioSourceCtrl.onGoingRequest.clipPath != clipPath)
            {
                return;
            }
            ClearRequests();
        }

        public LinkEvent onCompleted = new LinkEvent();
        private void OnAudioSourceFree()
        {
            _audioSourceCtrl.gameObject.SetActive(false);
            onCompleted?.Invoke();
        }

        public void ClearRequests()
        {
            if (!IsPlaying()) return;
            _audioSourceCtrl?.FadeOutAndDespawn();
            _audioSourceCtrl = null;
        }

        public void SetMixGroup(AudioMixerGroup mixGroup)
        {
            if (!_audioSourceCtrl) return;
            _audioSourceCtrl.audioSource.outputAudioMixerGroup = mixGroup;
        }

        public bool IsPlaying()
        {
            return _audioSourceCtrl.gameObject.activeSelf;
        }

        public void Pause()
        {
            _audioSourceCtrl?.Pause();
        }
        public void Resume()
        {
            _audioSourceCtrl?.Resume();
        }

        public void SetMute(bool mute)
        {
            if (_audioSourceCtrl == null) return;
            _audioSourceCtrl.audioSource.mute = mute;
        }

        public void SetPitch(float newValue, float transferTime)
        {
            if (_audioSourceCtrl == null) return;
            _audioSourceCtrl.setPitch = newValue;
            _audioSourceCtrl.FadePitch(1f, transferTime);
        }

        public void SetVolume(float newValue, float transferTime)
        {
            if (_audioSourceCtrl == null) return;
            _audioSourceCtrl.setVolume = newValue;
            _audioSourceCtrl.Fade(_audioSourceCtrl.currentVolume, transferTime);
        }
    }
}