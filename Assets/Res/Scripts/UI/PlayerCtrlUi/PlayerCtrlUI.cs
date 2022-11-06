using System;
using Res.Scripts.GameLogicInstance;
using Res.Scripts.Hero;
using Unity.VisualScripting;
using UnityEngine;

namespace Res.Scripts.UI.PlayerCtrlUi
{

    public class PlayerCtrlUI : MonoBehaviour
    {
        public Joystick joystick;
        public HoldButton jumpBtn;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            // if(GlobalVariable.Player.IsUnityNull()) return;
            var dir = joystick.Direction.normalized;
            PlayerCtrlInput.Instance.SetInput(dir, jumpBtn.IsPressing, jumpBtn.IsPressThisFrame, jumpBtn.HoldTime);
        }
    }
}