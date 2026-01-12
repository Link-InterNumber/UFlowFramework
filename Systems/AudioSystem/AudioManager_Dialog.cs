using System;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private IDialogPlayer _dialogPlayer;

        public void PlayDialog(string clipRef, Action callback = null)
        {
            if(string.IsNullOrEmpty(clipRef)) return;
            var request = new AudioRequest(clipRef, (int)AudioSourceType.Dialog, false);
            PushRequest(request);
        }
    }
}