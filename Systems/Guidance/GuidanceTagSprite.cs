
using UnityEngine;

namespace PowerCellStudio
{
    public class GuidanceTagSprite : GuidanceTag
    {
        private string _oldLayerName;

        public override void OnExecute()
        {
            TimeManager.instance.PauseTime();
            _oldLayerName = LayerMask.LayerToName(gameObject.layer);
            gameObject.SetLayerRecursively("UI");
        }

        public override void OnDeExecute()
        {
            TimeManager.instance.ResumeTime();
            gameObject.SetLayerRecursively(_oldLayerName);
        }

        public override Vector2 GetUIPosition()
        {
            return UIManager.MainCamaraPosToUIPos(transform.position);
        }
    }
}