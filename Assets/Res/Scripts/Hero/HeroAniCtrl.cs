using UnityEngine;

namespace Res.Scripts.Hero
{
    public class HeroAniCtrl: RoleAniCtrlBase
    {
        private static readonly int VSpeed = Animator.StringToHash("VSpeed");
        private static readonly int HSpeed = Animator.StringToHash("HSpeed");
        private static readonly int Jump1 = Animator.StringToHash("jump");

        public virtual void Jump()
        {
            animator.ResetTrigger(Jump1);
            animator.SetTrigger(Jump1);
        }
        
        public virtual void SetOnGround(bool onGround)
        {
            animator.SetBool(OnGround, onGround);
        }
        
        public virtual void SetOnWall(bool onGround)
        {
            animator.SetBool(OnGround, onGround);
        }
        
        public virtual void SetVSpeed(float vSpeed)
        {
            animator.SetFloat(VSpeed, vSpeed);
        }
        
        public virtual void SetHSpeed(float hSpeed)
        {
            animator.SetFloat(HSpeed, hSpeed);
        }
    }
}