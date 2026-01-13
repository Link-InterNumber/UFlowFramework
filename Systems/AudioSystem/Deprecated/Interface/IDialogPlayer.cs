using System;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IDialogPlayer : IAudioPlayer
    {
        float fadeoutTime { get; set; }
        
        void PlayDialog(string clipRef, Action callback);

        void Pause();
        
        void Resume();

        void Clear();
        
        void DeInit();
    }
}