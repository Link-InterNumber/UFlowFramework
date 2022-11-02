using UnityEngine;

namespace Res.Scripts.Hero
{
    public class JumpAniBinder : MonoBehaviour
    {
        public HeroMoveCtrl moveCtrl;
        
        public void Jump()
        {
            moveCtrl.Jump();
        }
    }
}