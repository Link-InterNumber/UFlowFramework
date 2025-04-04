// using System;
// using Sirenix.OdinInspector.Editor.Modules;
using UnityEngine;

namespace PowerCellStudio
{
    public class Oval2DDisplayer : MonoBehaviour
    {
#if UNITY_EDITOR
        public Color debugColor = Color.magenta;
        public Oval2D Oval2DDate;
        public bool useWorldPosition = false;
        
        private void OnDrawGizmos()
        {
            if (Oval2DDate ==null || Oval2DDate.width * Oval2DDate.height == 0) return;
            var oriColor = Gizmos.color;
            Gizmos.color = debugColor;
            Vector2[] tempList = new Vector2[359];
            for (int i = 0; i < 359; i++)
            {
                if(useWorldPosition) tempList[i] = Oval2DDate.GetValueByCentrifugalAngle(i);
                else tempList[i] = Oval2DDate.GetValueByCentrifugalAngle(i) + (Vector2)transform.position;
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