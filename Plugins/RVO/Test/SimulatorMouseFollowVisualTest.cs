using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RVO.JobSystem
{
    /// <summary>
    /// Visual runtime test for JobSimulator.
    /// Spawns agents from a prefab and drives them toward mouse world position.
    /// </summary>
    public sealed class SimulatorMouseFollowVisualTest : MonoBehaviour
    {
        [Header("Prefab")]
        public GameObject agentPrefab;
        public BoxCollider[] obstacles;
        public Transform agentRoot;

        [Header("Simulation")]
        [Min(1)] public int agentCount = 300;
        [Min(0.1f)] public float spawnRadius = 10f;
        [Min(0.01f)] public float timeStep = 1f / 30f;
        [Min(0f)] public float maxSpeed = 5f;
        [Min(0f)] public float neighborDist = 4f;
        [Min(1)] public int maxNeighbors = 16;
        [Min(0.01f)] public float radius = 0.3f;
        [Min(0.01f)] public float timeHorizon = 2.0f;
        [Min(0.01f)] public float timeHorizonObst = 2.0f;
        [Min(1)] public int batchSize = 64;
        public int seed = 9527;

        [Header("Mouse Plane")]
        public Camera targetCamera;
        public float worldPlaneZ = 0f;

        [Header("Run")]
        public bool runOnEnable = true;
        public bool simulateInFixedUpdate = false;
        public bool showDisplay = true;

        private Simulator _simulator;
        private readonly List<int> _agentIds = new List<int>();
        private readonly List<Transform> _agentViews = new List<Transform>();

        private void OnEnable()
        {
            // Application.targetFrameRate = 60;


            Initialize();
        }

        private void Update()
        {
            if (simulateInFixedUpdate)
            {
                return;
            }

            TickSimulation();
        }

        private void FixedUpdate()
        {
            if (!simulateInFixedUpdate)
            {
                return;
            }

            TickSimulation();
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        [ContextMenu("Initialize Mouse Follow Test")]
        public void Initialize()
        {
            Shutdown();

            if (agentPrefab == null)
            {
                Debug.LogError("[SimulatorMouseFollowVisualTest] Agent prefab is not assigned.");
                return;
            }

            var parent = agentRoot != null ? agentRoot : transform;

            _simulator = new Simulator();
            _simulator.SetTimeStep(timeStep);
            _simulator.SetAgentDefaults(neighborDist, maxNeighbors, timeHorizon, timeHorizonObst, radius, maxSpeed, Vector3.zero);
            if (obstacles != null)
            {
                foreach (var obstacle in obstacles)
                {
                    var points = new Vector3[4];
                    var minPos = obstacle.bounds.min;
                    var maxPos = obstacle.bounds.max;

                    points[0] = new Vector3(minPos.x, minPos.y, worldPlaneZ);
                    points[1] = new Vector3(maxPos.x, minPos.y, worldPlaneZ);
                    points[2] = new Vector3(maxPos.x, maxPos.y, worldPlaneZ);
                    points[3] = new Vector3(minPos.x, maxPos.y, worldPlaneZ);

                    _simulator.addObstacle(points);
                }

                _simulator.ProcessObstacles();
            }
            var random = new System.Random(seed);
            _agentIds.Capacity = agentCount;
            _agentViews.Capacity = agentCount;

            for (var i = 0; i < agentCount; i++)
            {
                var angle = (float)(random.NextDouble() * Mathf.PI * 2f);
                var r = (float)(random.NextDouble() * spawnRadius);
                var pos = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, worldPlaneZ);

                var id = _simulator.addAgent(pos, neighborDist, maxNeighbors, timeHorizon, timeHorizonObst, radius, maxSpeed, Vector3.zero);
                _agentIds.Add(id);
                if (!showDisplay) continue;
                var view = Instantiate(agentPrefab, pos, Quaternion.identity, parent).transform;
                _agentViews.Add(view);
            }

            Debug.Log($"[SimulatorMouseFollowVisualTest] Initialized with {agentCount} agents.");
        }

        [ContextMenu("Rebuild Agents")]
        public void Rebuild()
        {
            Initialize();
        }

        private void TickSimulation()
        {
            if (_simulator == null || _agentIds.Count == 0)
            {
                return;
            }
            if (!_simulator.CheckJobCompletion())
            {
                return;
            }

            var mouseWorld = GetMouseWorldPosition();

            for (var i = 0; i < _agentIds.Count; i++)
            {
                var id = _agentIds[i];
                var pos = _simulator.GetAgentPosition(id);
                var desired = mouseWorld - pos;
                desired.z = 0f;

                var prefVelocity = desired.sqrMagnitude > 1e-6f
                    ? desired.normalized * maxSpeed
                    : Vector3.zero;

                _simulator.SetAgentPrefVelocity(id, prefVelocity);
            }

            _simulator.doStepAsync();
            if (!showDisplay) return;
            for (var i = 0; i < _agentIds.Count; i++)
            {
                var id = _agentIds[i];
                var pos = _simulator.GetAgentPosition(id);
                pos.z = worldPlaneZ;
                _agentViews[i].position = pos;
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return new Vector3(0f, 0f, worldPlaneZ);
            }

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, worldPlaneZ));
            if (plane.Raycast(ray, out var enter))
            {
                var hit = ray.GetPoint(enter);
                hit.z = worldPlaneZ;
                return hit;
            }

            return new Vector3(0f, 0f, worldPlaneZ);
        }

        private void Shutdown()
        {
            if (_simulator != null)
            {
                _simulator.Clear();
                _simulator = null;
            }

            for (var i = 0; i < _agentViews.Count; i++)
            {
                if (_agentViews[i] != null)
                {
                    Destroy(_agentViews[i].gameObject);
                }
            }

            _agentViews.Clear();
            _agentIds.Clear();
        }
    }
}