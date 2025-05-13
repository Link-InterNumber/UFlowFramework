using System.Linq;

namespace PowerCellStudio
{
    public class TestListManager : SceneMainBase
    {
        public RecycleScrollRect recycleScrollRect;
        public int testNumber = 20;

        protected override void ReadyForStart()
        {
            if (!recycleScrollRect) return;
            recycleScrollRect.UpdateList(Enumerable.Range(0, testNumber).ToList());
        }
    }
}