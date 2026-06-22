using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Random = System.Random;

namespace RVO.JobSystem
{
    /// <summary>
    /// Visual runtime test for JobSimulator.
    /// Spawns agents from a prefab and drives them toward mouse world position.
    /// </summary>
    public sealed class JobSimulatorMouseFollowVisualTest : MonoBehaviour
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
        public bool runInSync = true;

        private JobSimulator _simulator;
        private readonly List<int> _agentIds = new List<int>();
        private readonly List<Transform> _agentViews = new List<Transform>();
        private Random _random;

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
                Debug.LogError("[JobSimulatorMouseFollowVisualTest] Agent prefab is not assigned.");
                return;
            }

            var parent = agentRoot != null ? agentRoot : transform;

            _simulator = new JobSimulator(maxNeighbors, batchSize);
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

                    var id = _simulator.AddObstacle(points);
                    var mono = obstacle.gameObject.AddComponent<TestSimulatorRemoveObstacles>();
                    mono.Setup(_simulator, id, worldPlaneZ);
                }

                _simulator.ProcessObstacles();
            }
            _random = new System.Random(seed);
            _agentIds.Capacity = agentCount;
            _agentViews.Capacity = agentCount;
            _simulator.ConfigAgentTypes(Enum.GetValues(typeof(TestAgentType)).Length); // Assuming 3 agent types for testing

            // _simulator.ConfigAgentExtraRadii((int)TestAgentType.Red, (int)TestAgentType.Green, 0.7f);
            // _simulator.ConfigAgentExtraRadii((int)TestAgentType.Red, (int)TestAgentType.Blue, 2f);
            // _simulator.ConfigAgentExtraRadii((int)TestAgentType.Green, (int)TestAgentType.Blue, 1.5f);
            // _simulator.ConfigAgentExtraRadii((int)TestAgentType.Red, (int)TestAgentType.Red, 0.5f);
            // _simulator.ConfigAgentExtraRadii((int)TestAgentType.Blue, (int)TestAgentType.Red, 0.8f);
            // _simulator.ConfigAgentExtraRadii((int)TestAgentType.Blue, (int)TestAgentType.Green, 1.5f);

            for (var i = 0; i < agentCount; i++)
            {
                var angle = (float)(_random.NextDouble() * Mathf.PI * 2f);
                var r = (float)(_random.NextDouble() * spawnRadius);
                var pos = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, worldPlaneZ);
                var agentType = _random.Next(0, 3); // Assuming 3 agent types for testing
                var id = _simulator.AddAgent(pos, neighborDist, maxNeighbors, timeHorizon, timeHorizonObst, radius, maxSpeed, Vector3.zero, agentType);
                _agentIds.Add(id);
                if (!showDisplay) continue;
                var view = Instantiate(agentPrefab, pos, Quaternion.identity, parent).transform;
                _agentViews.Add(view);
                var material = view.GetComponent<Renderer>().material;
                var colorType = (TestAgentType)agentType;
                switch (colorType)
                {
                    case TestAgentType.Red:
                        material.SetColor("_BaseColor", Color.red);
                        break;
                    case TestAgentType.Green:
                        material.SetColor("_BaseColor", Color.green);
                        break;
                    case TestAgentType.Blue:
                        material.SetColor("_BaseColor", Color.deepSkyBlue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Debug.Log($"[JobSimulatorMouseFollowVisualTest] Initialized with {agentCount} agents.");
        }

        [ContextMenu("Rebuild Agents")]
        public void Rebuild()
        {
            Initialize();
        }

        private void TickSimulation()
        {
            if (_simulator == null)
            {
                return;
            }

            if (!runInSync && !_simulator.CheckJobCompletion())
            {
                return;
            }
            var mouseWorld = GetMouseWorldPosition();
            var mouseScroll = GetMouseScroll();

            if (mouseScroll > 0f)
            {
                // 添加agent
                var agentType = _random.Next(0, 3); // Assuming 3 agent types for testing
                var id = _simulator.AddAgent(mouseWorld, neighborDist, maxNeighbors, timeHorizon, timeHorizonObst, radius, maxSpeed, Vector3.zero, agentType);
                _agentIds.Add(id);
                if (showDisplay)
                {
                    var parent = agentRoot != null ? agentRoot : transform;
                    var view = Instantiate(agentPrefab, mouseWorld, Quaternion.identity, parent).transform;
                    _agentViews.Add(view);
                    var material = view.GetComponent<Renderer>().material;
                    var colorType = (TestAgentType)agentType;
                    switch (colorType)
                    {
                        case TestAgentType.Red:
                            material.SetColor("_BaseColor", Color.red);
                            break;
                        case TestAgentType.Green:
                            material.SetColor("_BaseColor", Color.green);
                            break;
                        case TestAgentType.Blue:
                            material.SetColor("_BaseColor", Color.deepSkyBlue);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else if(mouseScroll < 0)
            {
                // 删除agent
                if (_agentIds.Count > 0)
                {
                    var lastId = _agentIds[_agentIds.Count - 1];
                    _simulator.DelAgent(lastId);
                    _agentIds.RemoveAt(_agentIds.Count - 1);
                    if (showDisplay)
                    {
                        var lastView = _agentViews[_agentViews.Count - 1];
                        if (lastView != null)
                        {
                            Destroy(lastView.gameObject);
                        }
                        _agentViews.RemoveAt(_agentViews.Count - 1);
                    }
                }
            }
            
            
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

            if (runInSync)
            {
                _simulator.DoStep();
            }
            else
            {
                _simulator.DoStepAsync();
            }
            
            if (!showDisplay) return;
            for (var i = 0; i < _agentIds.Count; i++)
            {
                var id = _agentIds[i];
                var velocity = _simulator.GetAgentVelocity(id);
                
                // pos.z = worldPlaneZ;
                _agentViews[i].position = velocity * timeStep + _agentViews[i].position;
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return new Vector3(0f, 0f, worldPlaneZ);
            }

#if ENABLE_INPUT_SYSTEM
            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
#else
            var ray = cam.ScreenPointToRay(Input.mousePosition);
#endif
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, worldPlaneZ));
            if (plane.Raycast(ray, out var enter))
            {
                var hit = ray.GetPoint(enter);
                hit.z = worldPlaneZ;
                return hit;
            }

            return new Vector3(0f, 0f, worldPlaneZ);
        }

        private float GetMouseScroll()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.scroll.ReadValue().y;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        private void Shutdown()
        {
            if (_simulator != null)
            {
                _simulator.Dispose();
                _simulator = null;
            }

            for (var i = 0; i < _agentViews.Count; i++)
            {
                if (_agentViews[i] != null)
                {
                    Destroy(_agentViews[i].gameObject);
                }
            }

            foreach (var obstacle in obstacles)
            {
                if (!obstacle) continue;
                var mono = obstacle.gameObject.GetComponent<TestSimulatorRemoveObstacles>();
                if (mono)
                    GameObject.Destroy(mono);
            }

            _agentViews.Clear();
            _agentIds.Clear();
        }
    }
}