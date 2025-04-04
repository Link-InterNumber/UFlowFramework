using UnityEngine;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private IBgmPlayer _musicPlayer;
        
        public bool HasMusicGroupRegister(MusicGroup group)
        {
            return _musicPlayer?.HasGroup(group) ?? false;
        }

        public void PlayMusic(string clipRef, MusicGroup group,
            float fadeoutTime = 1f, float intervalTime = 1f, float fadeinTime = 1f)
        {
            if(string.IsNullOrEmpty(clipRef)) return;
            PlayMusic(new[] {clipRef}, group, false, true, fadeoutTime, intervalTime, fadeinTime);
        }
        
        private void PlayMusic(string[] clipsRefs, MusicGroup group, bool randPlay, bool restart,
            float fadeoutTime = 1f, float intervalTime = 1f, float fadeinTime = 1f)
        {
            if(clipsRefs == null || clipsRefs.Length == 0) return;
            if (_musicPlayer == null) _musicPlayer = MusicAudioSourcePlayer.Create(transform, "MusicPlayer");
            if(_musicPlayer.IsMute) return;
            _musicPlayer.Play(clipsRefs, group, randPlay, restart, fadeoutTime, intervalTime, fadeinTime);
        }

        public bool SwitchMusicGroup(MusicGroup group)
        {
            return _musicPlayer?.SetCurGroup(group) ?? false;
        }

        public void PauseMusic(MusicGroup group)
        {
            _musicPlayer?.Pause(group);
        }
    }
}