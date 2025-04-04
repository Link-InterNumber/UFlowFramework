using UnityEngine;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private IBgmPlayer _ambiencePlayer;
        
        public bool HasAmbienceGroupRegister(MusicGroup group)
        {
            return _musicPlayer?.HasGroup(group) ?? false;
        }

        public void PlayAmbience(string clipRef, MusicGroup group, 
            float fadeoutTime = 1f, float intervalTime = 1f, float fadeinTime = 1f)
        {
            if(clipRef == null) return;
            if (_ambiencePlayer == null)
            {
                _ambiencePlayer = MusicAudioSourcePlayer.Create(transform, "AmbiencePlayer");
            }
            if(_ambiencePlayer.IsMute) return;
            _ambiencePlayer.Play(new[] {clipRef}, group, false, true, fadeoutTime, intervalTime, fadeinTime);
        }
        
        public void SwitchAmbienceGroup(MusicGroup group)
        {
            _musicPlayer?.SetCurGroup(group);
        }
        
        public void PauseAmbience(MusicGroup group)
        {
            _ambiencePlayer?.Pause(group);
        }
    }
}