using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace PowerCellStudio
{
    public class TestListManager : MonoBehaviour
    {
        [FormerlySerializedAs("listManager")] public RecycleScrollRect recycleScrollRect;
        public GridManager gridManager;
        public int testNumber = 20;

        private void OnEnable()
        {
            if(recycleScrollRect) recycleScrollRect.UpdateList(Enumerable.Repeat(1, testNumber).ToList());
            if(gridManager) gridManager.UpdateList(Enumerable.Repeat(1, testNumber).ToList());
        }
    }
}