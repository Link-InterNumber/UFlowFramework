using UnityEngine;

namespace PowerCellStudio
{
    public class PIDCurveDisplayer : MonoBehaviour
    {
        public PIDCurve curveData;

#if UNITY_EDITOR
        public void OnGUIInit()
        {
            curveData.OnGUIInit();
        }

        public float OnGUIUpdateValue(float dt, float input)
        {
            return curveData.OnGUIUpdateValue(dt, input);
        }
#endif
    }
}