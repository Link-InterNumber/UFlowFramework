using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EmptyRaycast : MaskableGraphic
    {
        protected EmptyRaycast()
        {
            useLegacyMeshGeneration = false;
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            toFill.Clear();
        }
    }
}