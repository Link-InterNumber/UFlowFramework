using UnityEngine;

namespace Res.Scripts.Hero
{
    public class RoleAniCtrlBase: MonoBehaviour
    {
        public Animator animator;
        
        protected static readonly int OnGround = Animator.StringToHash("onGround");
        protected static readonly int OnWall = Animator.StringToHash("onWall");
        private static readonly int MoveOnGround = Animator.StringToHash("moveOnGround");
        private static readonly int Atk = Animator.StringToHash("atk");
        private static readonly int Die1 = Animator.StringToHash("die");


        public void Move(bool isTrue)
        {
            animator.SetBool(MoveOnGround, isTrue);
        }
        
        public void Attack()
        {
            animator.SetTrigger(Atk);
        }
        
        public void Die()
        {
            animator.SetTrigger(Die1);
        }
    }
}