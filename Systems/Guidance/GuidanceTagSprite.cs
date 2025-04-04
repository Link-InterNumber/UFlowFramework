
using UnityEngine;

namespace PowerCellStudio
{
    public class GuidanceTagSprite : GuidanceTag
    {
        public override void OnExecute()
        {
            TimeManager.instance.PauseTime();
        }

        public override void OnDeExecute()
        {
            TimeManager.instance.ResumeTime();
        }

        public override Vector2 GetUIPosition()
        {
            return UIManager.MainCamaraPosToUIPos(transform.position);
        }
    }
}