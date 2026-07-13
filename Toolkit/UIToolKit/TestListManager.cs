using System;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    public class TestListManager : MonoBehaviour
    {
        public RecycleScrollRect recycleScrollRect;
        public int testNumber = 20;

        private void Awake()
        {
            IndexGetter.instance = new IndexGetter();
        }

        private void OnEnable()
        {
            if (!recycleScrollRect) return;
            recycleScrollRect.UpdateList(Enumerable.Range(0, testNumber));
        }
    }
}