using LinkFrameWork.DesignPatterns;
using Unity.VisualScripting;
using UnityEngine;

namespace Res.Scripts.Hero
{
    public struct PlayerInputDate
    {
        public Vector2 Dir;
        public bool IsPressJump;
        public bool IsPressThisFrame;
        public float PressTime;
    }
    
    public class PlayerCtrlInput: SingletonBase<PlayerCtrlInput>
    {
        private PlayerInputDate _inputDate;

        public void SetInput(Vector2 dir, bool isPressJump, bool isPressThisFrame, float pressTime)
        {
            _inputDate.Dir = dir;
            _inputDate.IsPressJump = isPressJump;
            _inputDate.IsPressThisFrame = isPressThisFrame;
            _inputDate.PressTime = pressTime;
        }
        
        public PlayerInputDate GetInput()
        {
            return _inputDate;
        }
    }
}