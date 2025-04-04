using System;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IBgmPlayer : IAudioPlayer
    {
        bool SetCurGroup(MusicGroup audioType);
        bool HasGroup(MusicGroup audioType);
        void Play(string[] clipsRefs, MusicGroup group, bool randPlay, bool restart, float fadeoutTime = 1f, float intervalTime = 1f,
            float fadeinTime = 1f);
        void Pause(MusicGroup audioType);
        void PauseAll();
        void Resume(MusicGroup audioType);
        void Clear(MusicGroup audioType);
        MusicGroup GetCurGroup();
        void DeInit();
        AudioClip GetCurClip(MusicGroup audioType);
        
        
    }
}