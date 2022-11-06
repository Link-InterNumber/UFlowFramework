using Res.Scripts.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace Res.Scripts.UI.PlayerCtrlUi
{
    public class DebugUI : MonoBehaviour
    {
        public HeroMoveCtrl ctrlSource;
        public Button debugBtn;
        public Button closeBtn;
        public GameObject debugPad;
        public Rigidbody2D rigib;
        public Slider gravityScale;
        public Slider jumpForce;
        public Slider wallJumpForce;
        public Slider moveSpeed;
        public Slider slideSpeed;
        public Slider recoverTime;
        public Slider messNum;

        private void Start()
        {
            debugPad.SetActive(false);
            closeBtn.onClick.AddListener(OnclickClose);
            debugBtn.onClick.AddListener(OnclickDebug);
            gravityScale.value = ctrlSource.gravityScale;
            jumpForce.value = ctrlSource.jumpForce;
            wallJumpForce.value = ctrlSource.wallJumpForce;
            moveSpeed.value = ctrlSource.speed;
            slideSpeed.value = ctrlSource.slideSpeed;
            recoverTime.value = ctrlSource.wallJumpRecoverTime;
            messNum.value = rigib.mass;
            
            gravityScale.onValueChanged.AddListener(OngravityScaleChange);
            jumpForce.onValueChanged.AddListener(OnjumpForceChange);
            wallJumpForce.onValueChanged.AddListener(OnWallJumpForceChange);
            moveSpeed.onValueChanged.AddListener(OnmoveSpeedChange);
            slideSpeed.onValueChanged.AddListener(OnslideSpeedChange);
            recoverTime.onValueChanged.AddListener(OnrecoverTimeChange);
            messNum.onValueChanged.AddListener(OnmessNumChange);
        }

        private void OnDestroy()
        {
            closeBtn.onClick.RemoveListener(OnclickClose);
            debugBtn.onClick.RemoveListener(OnclickDebug);
            gravityScale.onValueChanged.RemoveListener(OngravityScaleChange);
            jumpForce.onValueChanged.RemoveListener(OnjumpForceChange);
            wallJumpForce.onValueChanged.RemoveListener(OnWallJumpForceChange);
            moveSpeed.onValueChanged.RemoveListener(OnmoveSpeedChange);
            slideSpeed.onValueChanged.RemoveListener(OnslideSpeedChange);
            recoverTime.onValueChanged.RemoveListener(OnrecoverTimeChange);
            messNum.onValueChanged.RemoveListener(OnmessNumChange);
        }

        private void OnWallJumpForceChange(float arg0)
        {
            ctrlSource.wallJumpForce = arg0;
        }

        private void OnclickClose()
        {
            debugPad.SetActive(false);
            debugBtn.gameObject.SetActive(true);
        }

        private void OnclickDebug()
        {
            debugPad.SetActive(true);
            debugBtn.gameObject.SetActive(false);
        }

        private void OngravityScaleChange(float arg0)
        {
            ctrlSource.gravityScale = arg0;
        }

        private void OnjumpForceChange(float arg0)
        {
            ctrlSource.jumpForce = arg0;
        }

        private void OnmoveSpeedChange(float arg0)
        {
            ctrlSource.speed = arg0;
        }

        private void OnslideSpeedChange(float arg0)
        {
            ctrlSource.slideSpeed = arg0;
        }

        private void OnrecoverTimeChange(float arg0)
        {
            ctrlSource.wallJumpRecoverTime = arg0;
        }

        private void OnmessNumChange(float arg0)
        {
            rigib.mass = arg0;
        }
    }
}