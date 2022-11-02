using DG.Tweening;
using Res.Scripts.GameLogicInstance;
using UnityEngine;

namespace Res.Scripts.Hero
{
    public class HeroMoveCtrl: RoleMoveCtlrBase
    {
        public HeroAniCtrl AniCtrl;
        public Transform display;
        public Rigidbody2D rigidbodyCom;
        public ColliderCheckerBase groundCheck;
        public ColliderCheckerBase leftWallCheck;
        public ColliderCheckerBase rightwallCheck;

        private bool _isJump = false;
        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            groundCheck.onValueChange.AddListener(AniCtrl.SetOnGround);
            leftWallCheck.onValueChange.AddListener(OnHitWall);
            rightwallCheck.onValueChange.AddListener(OnHitWall);
            GlobalVariable.Player = this;
        }

        protected override void UnRegisterEvent()
        {
            base.UnRegisterEvent();
            groundCheck.onValueChange.RemoveListener(AniCtrl.SetOnGround);
            leftWallCheck.onValueChange.AddListener(OnHitWall);
            rightwallCheck.onValueChange.AddListener(OnHitWall);
            GlobalVariable.Player = null;
        }

        private void OnHitWall(bool arg0)
        {
            AniCtrl.SetOnWall(leftWallCheck.IsOnHitting || rightwallCheck.IsOnHitting);
        }

        private bool _OnJumpAni = false;

        public void DoJumpAni()
        {
            if(_OnJumpAni) return;
            if(!groundCheck.IsOnHitting && !leftWallCheck.IsOnHitting && !rightwallCheck.IsOnHitting)
                return;
            _OnJumpAni = true;
            AniCtrl.Jump();
        }

        public void Jump()
        {
            _OnJumpAni = false;
            if (groundCheck.IsOnHitting || (leftWallCheck.IsOnHitting && rightwallCheck.IsOnHitting))
            {
                DOVirtual.DelayedCall(0.2f, () =>
                {
                    rigidbodyCom.AddForce(Vector2.up * 15);
                });
            }
            else if (leftWallCheck.IsOnHitting)
            {
                rigidbodyCom.AddForce(Vector2.one);
                display.localScale = _rightV3;
            }
            else if (rightwallCheck.IsOnHitting)
            {
                rigidbodyCom.AddForce(Vector2.up + Vector2.left);
                display.localScale = Vector3.one;
            }
        }

        public void ResetMoveHorizontal()
        {
            var curVelocity = rigidbodyCom.velocity;
            rigidbodyCom.velocity = curVelocity * Vector2.up;
        }

        private readonly Vector3 _rightV3 = new Vector3(-1, 1, 1);
        public void MoveHorizontal(float speed)
        {
            if(Mathf.Approximately(speed, 0)) return;
            
            rigidbodyCom.AddForce(speed*1.5f * Vector2.right);
            display.localScale = speed < 0 ? Vector3.one : _rightV3;
            // Debug.Log(rigidbodyCom.velocity.x);
        }

        private void FixedUpdate()
        {
            AniCtrl.Move(!Mathf.Approximately(rigidbodyCom.velocity.x, 0));
            AniCtrl.SetVSpeed(rigidbodyCom.velocity.y);
            AniCtrl.SetHSpeed(rigidbodyCom.velocity.x);
        }
        
    }
}