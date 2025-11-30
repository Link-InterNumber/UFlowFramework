using UnityEngine;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private IBgmPlayer _ambiencePlayer;
        
        public bool HasAmbienceGroupRegister(MusicGroup group)
        {
            return _ambiencePlayer?.HasGroup(group) ?? false;
        }

        public void PlayAmbience(string clipRef, MusicGroup group, 
            float fadeoutTime = 1f, float intervalTime = 1f, float fadeinTime = 1f)
        {
            if(clipRef == null) return;
            CheckPlayer(AudioSourceType.Ambience);
            if(_ambiencePlayer.IsMute) return;
            _ambiencePlayer.Play(new[] {clipRef}, group, false, true, fadeoutTime, intervalTime, fadeinTime);
        }
        
        public void SwitchAmbienceGroup(MusicGroup group)
        {
            _ambiencePlayer?.SetCurGroup(group);
        }
        
        public void PauseAmbience(MusicGroup group)
        {
            _ambiencePlayer?.Pause(group);
        }
    }
}