using UnityEngine;

namespace PowerCellStudio
{
    public class LissajousCurveDisplayer : MonoBehaviour
    {
#if UNITY_EDITOR
        public Color debugColor = Color.magenta;
        public LissajousCurve Oval2DDate;
        public bool useWorldPosition = false;
        [Min(365)] public int count = 365;
        
        private void OnDrawGizmos()
        {
            if (Oval2DDate ==null) return;
            var oriColor = Gizmos.color;
            Gizmos.color = debugColor;
            Vector2[] tempList = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                if(useWorldPosition) tempList[i] = Oval2DDate.Update(Mathf.PI * 2f / count);
                else tempList[i] = Oval2DDate.Update(Mathf.PI * 2f / count) + (Vector2)transform.position;
            }
            for (int i = 1; i < tempList.Length; i++)
            {
                if(i == tempList.Length - 1)
                {
                    Gizmos.DrawLine(tempList[i], tempList[0]);
                    break;
                }
                Gizmos.DrawLine(tempList[i - 1], tempList[i]);
            }
            Gizmos.color = oriColor;
        }
#endif
    }
}