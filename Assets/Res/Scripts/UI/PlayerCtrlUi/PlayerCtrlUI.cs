using System;
using Res.Scripts.GameLogicInstance;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Res.Scripts.UI.PlayerCtrlUi
{
    public class PlayerCtrlUI : MonoBehaviour
    {
        public HoldButton leftBtn;
        public HoldButton rightBtn;
        public Button jumpBtn;

        private void Start()
        {
            jumpBtn.onClick.AddListener(OnClickJump);
            // leftBtn.onPointChange.AddListener(OnPressLeft);
            // rightBtn.onPointChange.AddListener(OnPressRight);
        }

        private void OnDestroy()
        {
            jumpBtn.onClick.RemoveListener(OnClickJump);
            // leftBtn.onPointChange.RemoveListener(OnPressLeft);
            // rightBtn.onPointChange.RemoveListener(OnPressRight);
        }

        private void Update()
        {
            if(GlobalVariable.Player.IsUnityNull()) return;
            if (leftBtn.IsPressing && !rightBtn.IsPressing)
            {
                GlobalVariable.Player.MoveHorizontal(-1);
            }
            else if (!leftBtn.IsPressing && rightBtn.IsPressing)
            {
                GlobalVariable.Player.MoveHorizontal(1);
            }
            else if(!leftBtn.IsPressing && !rightBtn.IsPressing)
            {
                GlobalVariable.Player.ResetMoveHorizontal();
            }

            if (leftBtn.IsPressing && rightBtn.IsPressing)
            {
                GlobalVariable.Player.ResetMoveHorizontal();
            }
        }

        private void OnPressLeft(bool isPress)
        {
            if(GlobalVariable.Player == null) return;
            if (isPress && !rightBtn.IsPressing)
            {
                GlobalVariable.Player.MoveHorizontal(-1);
            }
            else if(!isPress && !rightBtn.IsPressing)
            {
                GlobalVariable.Player.ResetMoveHorizontal();
            }

            if (isPress && rightBtn.IsPressing)
            {
                GlobalVariable.Player.ResetMoveHorizontal();
            }
        }

        private void OnPressRight(bool isPress)
        {
            if(GlobalVariable.Player == null) return;
            if (isPress && !leftBtn.IsPressing)
            {
                GlobalVariable.Player.MoveHorizontal(1);
            }
            else if(!isPress && !leftBtn.IsPressing)
            {
                GlobalVariable.Player.ResetMoveHorizontal();
            }
            if (isPress && leftBtn.IsPressing)
            {
                GlobalVariable.Player.ResetMoveHorizontal();
            }
        }

        private void OnClickJump()
        {
            if(GlobalVariable.Player == null) return;
            GlobalVariable.Player.DoJumpAni();
        }
    }
}