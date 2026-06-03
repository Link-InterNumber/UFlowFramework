using UnityEngine;

namespace RVO.JobSystem
{
    public sealed partial class JobSimulator
    {
        private sealed class ManagedObstacleState
        {
            public int id;
            public Vector3 point;
            public Vector3 direction;
            public int previous;
            public int next;
            public bool convex;
        }
    }
}
