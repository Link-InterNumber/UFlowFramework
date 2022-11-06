
using Res.Scripts.Hero;
using UnityEngine;
using UnityEngine.UI;

public class HeroStateDisplay : MonoBehaviour
{
    public HeroMoveCtrl ctrl;
    public Text name;

    // Update is called once per frame
    void Update()
    {
        switch (ctrl.curMoveState)
        {
            case MoveState.Stay:
                name.text = "呆";
                break;
            case MoveState.Walk:
                name.text = "走";
                break;
            case MoveState.JumpingUp:
                name.text = "跳";
                break;
            case MoveState.FallDown:
                name.text = "落";
                break;
            case MoveState.WallJump:
                name.text = "蹬";
                break;
            case MoveState.GrabWall:
                name.text = "扒";
                break;
            case MoveState.SlideWall:
                name.text = "滑";
                break;
            case MoveState.ClimbWall:
                name.text = "爬";
                break;
            case MoveState.Dash:
                name.text = "冲";
                break;
            default:
                name.text = "!?";
                break;
        }
        
    }
}
