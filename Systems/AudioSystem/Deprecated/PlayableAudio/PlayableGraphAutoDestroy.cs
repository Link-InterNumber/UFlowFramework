using UnityEngine;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public class PlayableGraphAutoDestroy : MonoBehaviour
    {
        public PlayableGraph graph;

        private void OnDestroy()
        {
            if(graph.IsValid()) graph.Destroy();
        }
    }
}