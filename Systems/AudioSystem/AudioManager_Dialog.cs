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
            if (_dialogPlayer == null)
            {
                _dialogPlayer = DialogPlayer.Create(transform, "DialogPlayer");
            }
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