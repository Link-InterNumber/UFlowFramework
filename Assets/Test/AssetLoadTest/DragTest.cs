using UnityEngine;
using UnityEngine.EventSystems;

namespace Test.AssetLoadTest
{
    public class DragTest : MonoBehaviour,IDragHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            transform.position += eventData.delta.x * Vector3.right;
        }
    }
}