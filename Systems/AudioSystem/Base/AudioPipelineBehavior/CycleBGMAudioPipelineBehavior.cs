
using UnityEngine.Audio;

namespace PowerCellStudio
{
    public class CycleBGMAudioPipelineBehavior : IAudioPipelineBehavior
    {
        public CycleBGMAudioPipelineBehavior(AudioPipeline pipeline)
        {
            _pipeline = pipeline;
            _musicRecord = new MusicRecord();
            _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
            _audioSourceCtrl.autoDespawn = false;
            _audioSourceCtrl.onFree.AddListener(TryPostRequest);
            _audioSourceCtrl.gameObject.SetActive(false);                                                
        }
        
        public void Dispose()
        {
            ClearRequests();
            _audioSourceCtrl.DeSpawn();
            _audioSourceCtrl = null;
        }

        private LinkAudioSource _audioSourceCtrl;

        private AudioPipeline _pipeline;
        public AudioPipeline pipeline { get => _pipeline; set => _pipeline = value; }

        private MusicRecord _musicRecord;
        public MusicRecord musicRecord => _musicRecord;

        private void TryPostRequest()
        {
            if (!_audioSourceCtrl) return;
            _audioSourceCtrl.gameObject.SetActive(_musicRecord.Count > 0);
            var nextRequest = _musicRecord.GetCurrent();
            _musicRecord.MoveNext();
            if (string.IsNullOrEmpty(nextRequest.clipPath)) return;
            _audioSourceCtrl.gameObject.SetActive(true);
            _audioSourceCtrl.Play(nextRequest);
        }

        public void ReceiveRequest(AudioRequest request)
        {
            if (string.IsNullOrEmpty(request.clipPath))
            {
                _audioSourceCtrl.Play(request);
                return;
            }
            _musicRecord.AddClip(request, _musicRecord.Count);
            if (_audioSourceCtrl && string.IsNullOrEmpty(_audioSourceCtrl.onGoingRequest.clipPath))
            {
                TryPostRequest();
            }
        }
        
        public void RemoveRequest(string clipPath)
        {
            _musicRecord.RemoveClip(clipPath);
            if (_audioSourceCtrl == null) return;
            if (_audioSourceCtrl.onGoingRequest.clipPath != clipPath) return;
            if (_musicRecord.Count <= 0)
            {
                _audioSourceCtrl.FadeOutAndClear();
            }
            else 
                TryPostRequest();
        }

        public void ClearRequests()
        {
            if (!IsPlaying()) return;
            _audioSourceCtrl?.FadeOutAndDespawn();
            _audioSourceCtrl = LinkAudioSourceUtils.Get(_pipeline);
            _audioSourceCtrl.autoDespawn = false;
            _audioSourceCtrl.onFree.AddListener(TryPostRequest);
            _audioSourceCtrl.gameObject.SetActive(false);
            _musicRecord.Clear();
        }
        
        public void SetMixGroup(AudioMixerGroup mixGroup)
        {
            if (!_audioSourceCtrl) return;
            _audioSourceCtrl.audioSource.outputAudioMixerGroup = mixGroup;
        }

        public bool IsPlaying()
        {
            return _audioSourceCtrl && !string.IsNullOrEmpty(_audioSourceCtrl.onGoingRequest.clipPath);
        }

        public void Pause()
        {
            _audioSourceCtrl?.Pause();
            _musicRecord.Stop(_audioSourceCtrl.audioSource.time);
        }
        public void Resume()
        {
            _musicRecord.GetLastMusic(out var lastMusic);
            if (lastMusic > 0 && _audioSourceCtrl) _audioSourceCtrl.audioSource.time = lastMusic;
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