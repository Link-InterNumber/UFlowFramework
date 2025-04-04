using UnityEngine;

namespace PowerCellStudio
{
    public class EaseCurveDisplayer : MonoBehaviour
    {
        public EaseType easeType;

#if UNITY_EDITOR
        public float OnGUIUpdateValue(float time)
        {
            return Ease.GetEase(easeType, time);
        }
#endif
    }
}