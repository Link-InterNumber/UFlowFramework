using LinkFrameWork.Define;
using UnityEngine;

namespace Res.Scripts.Hero
{
    public class FlipDisplay : MonoBehaviour
    {
        public Transform display;
        public Direction2D rawDirection;

        [HideInInspector] public Direction2D curDir;

        private Vector3 _rawScale;
        private Vector3 _turnV3 = new Vector3(-1.0f, 1.0f, 1.0f);

        private void Start()
        {
            _rawScale = display.localScale;
            curDir = rawDirection;
        }

        public void DoFlip(Direction2D dir)
        {
            if (curDir == dir) return;
            curDir = dir;
            if (dir == Direction2D.Left)
            {
                if (rawDirection != Direction2D.Left)
                {
                    display.localScale = Vector3.Scale(_rawScale, _turnV3);
                }
                else
                {
                    display.localScale = _rawScale;
                }
            }
            else
            {
                if (rawDirection == Direction2D.Left)
                {
                    display.localScale = Vector3.Scale(_rawScale, _turnV3);
                }
                else
                {
                    display.localScale = _rawScale;
                }
            }
        }
    }
}