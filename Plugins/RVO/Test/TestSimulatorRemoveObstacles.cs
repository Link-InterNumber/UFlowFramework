using UnityEngine;
using UnityEngine.EventSystems;

namespace RVO.JobSystem
{
    public class TestSimulatorRemoveObstacles : MonoBehaviour, IPointerClickHandler
    {
        private JobSimulator _simulator;
        private int _obstacleId;
        private float _worldPlaneZ;
        
        public void Setup(JobSimulator simulator, int obstacleId, float worldPlaneZ)
        {
            _simulator = simulator;
            _obstacleId = obstacleId;
            _worldPlaneZ = worldPlaneZ;
        }

        private void OnDestroy()
        {
            _simulator = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var renderer = gameObject.GetComponent<MeshRenderer>();
            var isRendering = renderer.enabled;
            if (isRendering)
            {
                _simulator.RemoveObstacle(_obstacleId);
                renderer.enabled = false;
            }
            else
            {
                var collider = gameObject.GetComponent<BoxCollider>();
                var points = new Vector3[4];
                var minPos = collider.bounds.min;
                var maxPos = collider.bounds.max;

                points[0] = new Vector3(minPos.x, minPos.y, _worldPlaneZ);
                points[1] = new Vector3(maxPos.x, minPos.y, _worldPlaneZ);
                points[2] = new Vector3(maxPos.x, maxPos.y, _worldPlaneZ);
                points[3] = new Vector3(minPos.x, maxPos.y, _worldPlaneZ);

                var id = _simulator.AddObstacle(points);
                Setup(_simulator, id, _worldPlaneZ);
                
                renderer.enabled = true;
            }

        }
    }
}