using System.Buffers;
using UnityEngine;

namespace PowerCellStudio
{
    public class LissajousCurveDisplayer : MonoBehaviour
    {
#if UNITY_EDITOR
        public Color debugColor = Color.magenta;
        public LissajousCurve curveData;
        public bool useWorldPosition = false;
        [Min(365)] public int count = 365;
        
        private void OnDrawGizmos()
        {
            if (curveData ==null) return;
            var oriColor = Gizmos.color;
            Gizmos.color = debugColor;
            Vector2[] tempList = ArrayPool<Vector2>.Shared.Rent(count);
            for (int i = 0; i < count; i++)
            {
                if(useWorldPosition) tempList[i] = curveData.Update(Mathf.PI * 2f / count);
                else tempList[i] = curveData.Update(Mathf.PI * 2f / count) + (Vector2)transform.position;
            }
            for (int i = 1; i < count; i++)
            {
                if(i == count - 1)
                {
                    break;
                }
                Gizmos.DrawLine(tempList[i - 1], tempList[i]);
            }
            ArrayPool<Vector2>.Shared.Return(tempList);
            Gizmos.color = oriColor;
        }
#endif
    }
}