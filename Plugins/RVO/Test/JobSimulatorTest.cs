using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RVO.JobSystem
{
    /// <summary>
    /// Runtime smoke/regression tests for JobSimulator.
    /// Attach this to a GameObject and run in Play Mode.
    /// </summary>
    public sealed class JobSimulatorTest : MonoBehaviour
    {
        [Header("Run")]
        public bool verboseLog = false;

        [Header("Scenario")]
        [Min(1)] public int agentCount = 200;
        [Min(1)] public int simulationSteps = 120;
        [Min(0.1f)] public float spawnRadius = 16f;
        [Min(0.01f)] public float timeStep = 1f / 30f;
        [Min(0f)] public float maxSpeed = 3.0f;
        [Min(0f)] public float neighborDist = 4.0f;
        [Min(1)] public int maxNeighbors = 16;
        [Min(0.01f)] public float radius = 0.35f;
        public int seed = 9527;

        private void OnEnable()
        {

            RunAllTests();
        }

        [ContextMenu("Run JobSimulator Tests")]
        public void RunAllTests()
        {
            var timer = Stopwatch.StartNew();
            var passed = 0;
            var failed = 0;

            RunCase("Create/Step Stability", TestCreateAndStepStability, ref passed, ref failed);
            RunCase("Delete Agent Flow", TestDeleteAgentFlow, ref passed, ref failed);
            RunCase("Near-Agent Query", TestQueryNearAgent, ref passed, ref failed);
            RunCase("Dispose Guard", TestDisposeGuard, ref passed, ref failed);

            timer.Stop();
            var color = failed == 0 ? "green" : "red";
            UnityEngine.Debug.Log($"<color={color}>[JobSimulatorTest] done, pass={passed}, fail={failed}, elapsed={timer.Elapsed.TotalMilliseconds:F2} ms</color>");
        }

        private void RunCase(string name, Func<string> caseFn, ref int passed, ref int failed)
        {
            try
            {
                var info = caseFn();
                passed++;
                if (!string.IsNullOrEmpty(info) && verboseLog)
                {
                    UnityEngine.Debug.Log($"<color=green>[PASS]</color> {name} | {info}");
                }
                else
                {
                    UnityEngine.Debug.Log($"<color=green>[PASS]</color> {name}");
                }
            }
            catch (Exception ex)
            {
                failed++;
                UnityEngine.Debug.LogError($"<color=red>[FAIL]</color> {name}\n{ex}");
            }
        }

        private string TestCreateAndStepStability()
        {
            using var simulator = CreateSimulator();
            var ids = SpawnAgents(simulator, agentCount, spawnRadius, seed);

            for (var step = 0; step < simulationSteps; step++)
            {
                simulator.DoStep();

                for (var i = 0; i < ids.Count; i++)
                {
                    var id = ids[i];
                    var pos = simulator.GetAgentPosition(id);
                    var vel = simulator.GetAgentVelocity(id);
                    var neighborNum = simulator.GetAgentNumAgentNeighbors(id);

                    Ensure(IsFinite(pos), $"agent {id} position invalid at step {step}: {pos}");
                    Ensure(IsFinite(vel), $"agent {id} velocity invalid at step {step}: {vel}");
                    Ensure(vel.magnitude <= maxSpeed + 0.001f, $"agent {id} speed overflow at step {step}: {vel.magnitude}");
                    Ensure(neighborNum <= maxNeighbors, $"agent {id} neighbor count overflow: {neighborNum} > {maxNeighbors}");
                }
            }

            return $"agents={agentCount}, steps={simulationSteps}";
        }

        private string TestDeleteAgentFlow()
        {
            using var simulator = CreateSimulator();
            var ids = SpawnAgents(simulator, 32, spawnRadius * 0.5f, seed + 1);

            simulator.DoStep();
            var removeId = ids[ids.Count / 2];
            simulator.DelAgent(removeId);
            simulator.DoStep();

            Ensure(simulator.GetNumAgents() == 31, $"delete flow failed, count={simulator.GetNumAgents()}");

            var removedLookupThrows = false;
            try
            {
                simulator.GetAgentPosition(removeId);
            }
            catch (KeyNotFoundException)
            {
                removedLookupThrows = true;
            }

            Ensure(removedLookupThrows, "deleted agent should not be queryable");
            return "delete->step->lookup validation";
        }

        private string TestQueryNearAgent()
        {
            using var simulator = CreateSimulator();

            simulator.SetAgentDefaults(neighborDist, maxNeighbors, 2.0f, 2.0f, radius, maxSpeed, Vector3.zero);

            var idA = simulator.AddAgent(new Vector3(0f, 0f, 0f));
            var idB = simulator.AddAgent(new Vector3(10f, 0f, 0f));
            var idC = simulator.AddAgent(new Vector3(0f, 10f, 0f));

            simulator.SetAgentPrefVelocity(idA, Vector3.zero);
            simulator.SetAgentPrefVelocity(idB, Vector3.zero);
            simulator.SetAgentPrefVelocity(idC, Vector3.zero);

            var nearest = simulator.QueryNearAgent(new Vector3(0.4f, 0.2f, 0f), 2f);
            Ensure(nearest == idA, $"queryNearAgent mismatch, got={nearest}, expected={idA}");

            var none = simulator.QueryNearAgent(new Vector3(100f, 100f, 0f), 1f);
            Ensure(none == -1, $"queryNearAgent far range should return -1, got={none}");

            return "near/far query validation";
        }

        private string TestDisposeGuard()
        {
            var simulator = CreateSimulator();
            simulator.Dispose();

            var throwsDisposed = false;
            try
            {
                simulator.DoStep();
            }
            catch (ObjectDisposedException)
            {
                throwsDisposed = true;
            }

            Ensure(throwsDisposed, "calling doStep after Dispose should throw ObjectDisposedException");
            return "dispose guard validation";
        }

        private JobSimulator CreateSimulator()
        {
            var simulator = new JobSimulator(maxNeighbors, 32);
            simulator.SetTimeStep(timeStep);
            simulator.SetAgentDefaults(neighborDist, maxNeighbors, 2.0f, 2.0f, radius, maxSpeed, Vector3.zero);
            return simulator;
        }

        private List<int> SpawnAgents(JobSimulator simulator, int count, float radiusRange, int randomSeed)
        {
            var random = new System.Random(randomSeed);
            var ids = new List<int>(count);

            for (var i = 0; i < count; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2.0);
                var r = (float)(random.NextDouble() * radiusRange);
                var pos = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);

                var id = simulator.AddAgent(pos);
                ids.Add(id);

                var toCenter = (-pos);
                var prefVel = toCenter.sqrMagnitude > 1e-5f
                    ? toCenter.normalized * (maxSpeed * 0.85f)
                    : Vector3.zero;

                simulator.SetAgentPrefVelocity(id, prefVel);
            }

            return ids;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                     float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                     float.IsNaN(value.z) || float.IsInfinity(value.z));
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }
}