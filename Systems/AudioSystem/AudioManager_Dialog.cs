using System;
using UnityEngine;

namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private IDialogPlayer _dialogPlayer;

        public void PlayDialog(string clipRef, Action callback = null)
        {
            if(string.IsNullOrEmpty(clipRef)) return;
            CheckPlayer(AudioSourceType.Dialog);
            _dialogPlayer.PlayDialog(clipRef, callback);
        }

        public void StopDialog()
        {
            if (_dialogPlayer == null)
            {
                return;
            }
            _dialogPlayer.Clear();
        }
    }
}