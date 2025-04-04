using UnityEngine;

namespace PowerCellStudio
{
    public class ProceduralCurveDisplayer: MonoBehaviour
    {
        public ProceduralCurve curveData;

#if UNITY_EDITOR
        public void OnGUIInit()
        {
            curveData.OnGUIInit();
        }

        public float OnGUIUpdateValue(float dt, float input, float inputDelta = 0)
        {
            return curveData.OnGUIUpdateValue(dt, input, inputDelta);
        }
#endif

    }
}
