using DG.Tweening;
using LinkFrameWork.Define;
using Unity.VisualScripting;
using UnityEngine;

namespace Res.Scripts.Hero
{
    public enum MoveState
    {
        Stay,
        Walk,
        JumpingUp, // 操作定义
        FallDown,
        WallJump, // 操作定义
        GrabWall, // 操作定义
        SlideWall,
        ClimbWall, // 操作定义
        Dash // 操作定义
    }

    public class HeroMoveCtrl : RoleMoveCtlrBase
    {
        [Header("Collider")]
        public ColliderCheckerBase groundCheck;
        public ColliderCheckerBase leftCheck;
        public ColliderCheckerBase rightCheck;
        public Rigidbody2D rb;
        public BetterJumping betterJumping;

        [Header("Display")]
        public FlipDisplay flipDisplay;
        public HeroAniCtrl anim;

        [Space] [Header("Setting")]
        public float speed = 10;
        public float jumpForce = 50;
        public float wallJumpForce = 2;
        public float slideSpeed = 5;
        public float wallJumpRecoverTime = 0.1f;
        public float dashSpeed = 20;
        public float gravityScale = 1;

        [Space] [Header("State")] [SerializeField]
        private MoveState _moveState;

        public MoveState curMoveState => _moveState;
        public bool canMove = true;
        public bool wallGrab;
        public bool wallSlide;
        private float _wallJumpLerp = 0;

        [Space] private bool groundTouch;
        private bool hasDashed;

        public Direction2D side = Direction2D.Left;
        [SerializeField] private bool _againstWall;

        private float _ctrlLerpValue = 1;
        private Tweener _jumpLerpDotween;
        public bool onWall => !groundCheck.IsOnHitting && (leftCheck.IsOnHitting || rightCheck.IsOnHitting);
        public bool hitLeftWall => !groundCheck.IsOnHitting && leftCheck.IsOnHitting;
        public bool hitRightWall => !groundCheck.IsOnHitting && rightCheck.IsOnHitting;
        public bool onGround => groundCheck.IsOnHitting && rb.velocity.y < 0.1f;


        // Update is called once per frame

        private PlayerInputDate GetInput()
        {
            return PlayerCtrlInput.Instance.GetInput();
        }

        void Update()
        {
            var input = PlayerCtrlInput.Instance.GetInput();

            float x = input.Dir.x;
            float y = input.Dir.y;
            // float xRaw = input.Dir.x;
            // float yRaw = input.Dir.y;
            Vector2 dir = input.Dir;

            _againstWall = !Mathf.Approximately(x, 0) && ((x > 0 && hitRightWall) || (x < 0 && hitLeftWall));
            Walk(dir);

            if (onGround && input.Dir == Vector2.zero && Mathf.Approximately(rb.velocity.x, 0))
                _moveState = MoveState.Stay;
            if (!Mathf.Approximately(rb.velocity.x, 0) && rb.velocity.y < 0.1f && onGround)
            {
                _moveState = MoveState.Walk;
                anim.Move(true);
            }
            else
            {
                anim.Move(false);
            }

            if (rb.velocity.y < 0 && !onGround && _moveState != MoveState.SlideWall)
            {
                _moveState = MoveState.FallDown;
            }

            if (onWall && !onGround)
            {
                switch (_moveState)
                {
                    case MoveState.FallDown:
                        if (_againstWall) _moveState = MoveState.SlideWall;
                        break;
                    case MoveState.WallJump:
                        if((rb.velocity.x > 0 && hitRightWall) || (rb.velocity.x < 0 && hitLeftWall))
                            _moveState = MoveState.SlideWall;
                        break;
                    case MoveState.GrabWall:
                        betterJumping.enabled = false;
                        _moveState = input.IsPressJump? MoveState.GrabWall: MoveState.SlideWall;
                        break;
                    case MoveState.SlideWall:
                        betterJumping.enabled = false;
                        if (input.IsPressJump)
                            _moveState = MoveState.GrabWall;
                        break;
                    case MoveState.ClimbWall:
                        betterJumping.enabled = false;
                        _moveState = input.IsPressJump? MoveState.ClimbWall: MoveState.SlideWall;
                        break;
                    default:
                        break;
                }
            }
            
            if (rb.velocity.y > 0 && !onWall && _moveState != MoveState.WallJump)
                _moveState = MoveState.JumpingUp;

            if (_moveState == MoveState.SlideWall || _moveState == MoveState.ClimbWall || _moveState == MoveState.GrabWall)
            {
                rb.gravityScale = 0;
            }
            else
            {
                rb.gravityScale = gravityScale;
            }

            // if (!input.IsPressJump || onWall && !Mathf.Approximately(x, 0) && _moveState != MoveState.GrabWall)
            // {
            //     _moveState = MoveState.SlideWall;
            //     wallSlide = true;
            //     WallSlide(y);
            // }

            if (input.IsPressThisFrame)
            {
                // anim.SetTrigger("jump");
                if (onGround && _moveState != MoveState.JumpingUp)
                {
                    betterJumping.enabled = true;
                    _moveState = MoveState.JumpingUp;
                    Jump(Vector2.up);
                }
                else if (_moveState == MoveState.SlideWall || _moveState == MoveState.ClimbWall || _moveState == MoveState.GrabWall)
                {
                    betterJumping.enabled = true;
                    _moveState = MoveState.WallJump;
                    WallJump();
                }
            }

            switch (_moveState)
            {
                case MoveState.Stay:
                    break;
                case MoveState.Walk:
                    break;
                case MoveState.JumpingUp:
                    break;
                case MoveState.FallDown:
                    break;
                case MoveState.WallJump:
                    break;
                case MoveState.GrabWall:
                    rb.velocity = Vector2.zero;
                    break;
                case MoveState.SlideWall:
                    if(onWall)
                        WallSlide(x, y);
                    else
                        _moveState = MoveState.FallDown;
                    break;
                case MoveState.ClimbWall:
                    if(onWall)
                        rb.velocity = speed * 0.5f * Vector2.up;
                    else
                    {
                        rb.velocity = rb.velocity * Vector2.right + jumpForce * 0.5f * Vector2.up;
                        _moveState = MoveState.JumpingUp;
                    }
                    break;
                case MoveState.Dash:
                    break;
                default:
                    break;
            }
            
            anim.SetHSpeed(rb.velocity.x);
            anim.SetVSpeed(rb.velocity.y);
            anim.SetOnGround(onGround);
            anim.SetOnWall(onWall);

            // if (Input.GetButtonDown("Fire1") && !hasDashed)
            // {
            //     if (xRaw != 0 || yRaw != 0)
            //         Dash(xRaw, yRaw);
            // }

            // Fall to ground again
            if (groundCheck.IsOnHitting && !groundTouch)
            {
                GroundTouch();
                groundTouch = true;
            }

            // if (!groundCheck.IsOnHitting && groundTouch)
            // {
            //     groundTouch = false;
            // }

            // WallParticle(y);

            // if (wallGrab || wallSlide || !canMove)
            //     return;

            if (x > 0)
            {
                side = Direction2D.Right;
            }

            if (x < 0)
            {
                side = Direction2D.Left;
            }

            flipDisplay.DoFlip(side);
        }

        void GroundTouch()
        {
            hasDashed = false;
            flipDisplay.DoFlip(side);
            betterJumping.enabled = false;
        }

        // private void Dash(float x, float y)
        // {
        //     Camera.main.transform.DOComplete();
        //     Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        //     // FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));
        //
        //     hasDashed = true;
        //
        //     // anim.SetTrigger("dash");
        //
        //     rb.velocity = Vector2.zero;
        //     Vector2 dir = new Vector2(x, y);
        //
        //     rb.velocity += dir.normalized * dashSpeed;
        //     StartCoroutine(DashWait());
        // }

        // IEnumerator DashWait()
        // {
        //     // FindObjectOfType<GhostTrail>().ShowGhost();
        //     StartCoroutine(GroundDash());
        //     DOVirtual.Float(14, 0, .8f, RigidbodyDrag);
        //
        //     rb.gravityScale = 0;
        //     betterJumping.enabled = false;
        //     wallJumped = true;
        //     isDashing = true;
        //
        //     yield return new WaitForSeconds(.3f);
        //
        //     rb.gravityScale = 3;
        //     betterJumping.enabled = true;
        //     wallJumped = false;
        //     isDashing = false;
        // }

        // IEnumerator GroundDash()
        // {
        //     yield return new WaitForSeconds(.15f);
        //     if (groundCheck.IsOnHitting)
        //         hasDashed = false;
        // }

        private void WallJump()
        {
            if(!onWall) return;
            if (hitRightWall)
            {
                side = Direction2D.Left;
            }else if (hitLeftWall)
            {
                side = Direction2D.Right;
            }
            flipDisplay.DoFlip(side);

            if(!_jumpLerpDotween.IsUnityNull())
                _jumpLerpDotween.Kill();
            _jumpLerpDotween = DOVirtual.Float(
                0, 
                1, 
                wallJumpRecoverTime, 
                value => _wallJumpLerp = value);
            var wallDir = hitRightWall ? Vector2.left : Vector2.right;
            Jump(0.70712f * wallJumpForce * (Vector2.up + wallDir));
        }
        

        private void WallSlide(float inputX ,float inputY)
        {
            if (!canMove)
                return;
            
            var push = (!_againstWall && Mathf.Abs(inputX) > 0.5f) ?  rb.velocity.x : 0;
            var slideModSpeed = inputY < -0.5f ? 2 * slideSpeed : slideSpeed;
            
            rb.velocity = new Vector2(push, -slideModSpeed);
        }

        private void Walk(Vector2 dir)
        {
            if (!canMove)
                return;

            if (_moveState == MoveState.GrabWall || _moveState == MoveState.Dash)
                return;

            if (_moveState == MoveState.WallJump)
                rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * speed, rb.velocity.y)),
                    _wallJumpLerp * Time.deltaTime);
            else
                rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        }

        private void Jump(Vector2 dir)
        {
            rb.velocity = rb.velocity * Vector2.right + dir * jumpForce;
            anim.Jump();
        }
        
        void RigidbodyDrag(float x)
        {
            rb.drag = x;
        }
    }
}