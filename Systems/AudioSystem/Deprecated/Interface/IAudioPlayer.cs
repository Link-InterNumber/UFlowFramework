using System;

namespace PowerCellStudio
{
    public interface IAudioPlayer
    {
        void SetVolume(float volume, float transferTime, Action onComplete = null);
        void SetMaxVolume(float maxVolume);
        public float GetVolume(bool isReal);
        public float GetMaxVolume();
        void Restart();
        void SetSpeed(float speedValue);
        void Mute(float transferTime);
        void Unmute(float transferTime);
        public bool IsMute { get; }
        // public IAssetLoader GetLoader();
    }
}