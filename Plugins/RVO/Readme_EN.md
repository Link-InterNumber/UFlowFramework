# RVO Avoidance System

## 🙏 Source
https://gamma.cs.unc.edu/RVO2/

Thanks to the original authors and contributors for their outstanding work.

## 😀 Overview

RVO (Reciprocal Velocity Obstacles) is a motion planning tool for dynamic multi-agent avoidance. It is suitable for scenarios where units move on the same plane, avoid each other, and avoid static obstacles.

The implementation in this project is based on `JobSimulator` and provides the following features:

- Supports reciprocal avoidance between multiple agents
- Supports avoidance against static obstacle edges
- Supports KdTree neighbor search
- Supports parallel computation with the Unity Job System
- Provides both synchronous and asynchronous simulation steps

> Note: The avoidance geometry of this implementation is essentially two-dimensional planar logic. It is suitable for units moving on a horizontal plane. Coordinate conversion may be required for input and output positions.

---

## 🛠️ Usage

### 1. Create a simulator

Use `JobSimulator` to create an avoidance simulator instance.

```csharp
using System.Collections.Generic;
using RVO.JobSystem;
using UnityEngine;

var simulator = new JobSimulator();
```

If you want to control neighbor capacity or batch size, you can also use a constructor with parameters:

```csharp
var simulator = new JobSimulator(maxNeighborsCapacity: 16, maxObstacleNeighborsCapacity: 16, batchSize: 32);
```

---

### 2. Set default agent parameters

Before adding agents, you usually call `SetAgentDefaults` to specify default parameters for agents created later.

```csharp
simulator.SetAgentDefaults(
    neighborDist: 15f,        // Neighbor search radius: the larger it is, the more units are considered
    maxNeighbors: 10,         // Maximum number of neighbors: larger values are more stable but more expensive
    timeHorizon: 5f,          // Prediction time window for other agents: larger values are more conservative
    timeHorizonObst: 5f,      // Prediction time window for static obstacles: larger values are more conservative
    radius: 1.5f,             // Agent radius: body size used for collision and avoidance calculations
    maxSpeed: 2f,             // Maximum speed: movement speed limit of the agent
    velocity: Vector3.zero);  // Initial velocity: usually zero
```

Parameter meanings:

- `neighborDist`: Determines how far away other agents are searched as avoidance targets.
- `maxNeighbors`: Limits the maximum number of recorded neighbors to avoid overly large neighbor lists each frame.
- `timeHorizon`: Controls prediction against other agents. Larger values make agents start avoiding earlier.
- `timeHorizonObst`: Controls prediction against static obstacles. Larger values make agents avoid obstacles earlier.
- `radius`: The collision radius of the agent. It affects edge clearance and yielding behavior.
- `maxSpeed`: The maximum allowed movement speed of the agent.
- `velocity`: Initial velocity, usually set to zero when spawned or to an initial movement direction.

---

### 3. Add agents

Call `AddAgent` to add an agent. The return value is the `agentId` of that agent.

```csharp
int agentId = simulator.AddAgent(new Vector3(0f, 0f, 0f));
```

You can also explicitly pass all parameters:

```csharp
int agentId = simulator.AddAgent(
    new Vector3(0f, 0f, 0f),  // Initial position
    neighborDist: 12f,        // Neighbor search radius
    maxNeighbors: 8,          // Maximum number of neighbors
    timeHorizon: 4f,          // Prediction time window for other agents
    timeHorizonObst: 4f,      // Prediction time window for obstacles
    radius: 0.5f,             // Agent radius
    maxSpeed: 3f,             // Maximum speed
    velocity: Vector3.zero);  // Initial velocity
```

---

### 4. Add static obstacles

Call `AddObstacle` with an ordered vertex list to create a set of polyline obstacle segments.

If the vertex list is provided in counterclockwise order, the system inserts a normal obstacle that agents cannot enter. If the vertex list is provided in clockwise order, the system inserts a bounding area that agents cannot leave.

```csharp
var obstacle = new List<Vector3>
{
    new Vector3(-2f, 0f, -2f),
    new Vector3(2f, 0f, -2f),
    new Vector3(2f, 0f, 2f),
    new Vector3(-2f, 0f, 2f),
};

int obstacleStart = simulator.AddObstacle(obstacle);
```

After adding obstacles, it is recommended to call `ProcessObstacles()` to build the obstacle tree structure so that obstacles are included in visibility queries. This is not required if you do not need obstacle visibility checks or have another visibility-checking mechanism.

```csharp
simulator.ProcessObstacles();
```

---

### 5. Set target velocity and step the simulation

Usually, each frame you first set the preferred velocity of each agent, then call `DoStep()` to perform a full simulation step.

```csharp
// Read agent positions and assign them to game objects
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    var pos = simulator.GetAgentPosition(id);
    pos.z = 0f;
    _agentViews[i].position = pos;
}

// Update agent velocities
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    simulator.SetAgentPrefVelocity(id, new Vector3(1f, 0f, 0f));
}

float globalTime = simulator.DoStep();
```

If you want to split the computation into asynchronous execution, use:

```csharp
// Check whether the previous job has completed
if (!simulator.CheckJobCompletion())
{
    return;
}

// Read agent positions and assign them to game objects
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    var pos = simulator.GetAgentPosition(id);
    pos.z = 0f;
    _agentViews[i].position = pos;
}

// Update agent velocities
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    simulator.SetAgentPrefVelocity(id, new Vector3(1f, 0f, 0f));
}

simulator.DoStepAsync();
```

---

### 6. Read agent state

After the simulation step is complete, you can query agent position, velocity, and neighbor information.

```csharp
Vector3 position = simulator.GetAgentPosition(agentId);
Vector3 velocity = simulator.GetAgentVelocity(agentId);
int agentNeighborCount = simulator.GetAgentNumAgentNeighbors(agentId);
int obstacleNeighborCount = simulator.GetAgentNumObstacleNeighbors(agentId);
```

To read specific neighbors:

```csharp
int neighborAgentId = simulator.GetAgentAgentNeighbor(agentId, 0);
int obstacleNeighborIndex = simulator.GetAgentObstacleNeighbor(agentId, 0);
```

---

### 7. Query visibility and nearest agent

`QueryVisibility` is used to determine whether the line between two points is blocked by obstacles.

```csharp
bool visible = simulator.QueryVisibility(
    new Vector3(-3f, 0f, 0f),
    new Vector3(3f, 0f, 0f),
    radius: 0.1f);
```

`QueryNearAgent` is used to find the nearest agent within a specified radius.

```csharp
int nearAgent = simulator.QueryNearAgent(new Vector3(0f, 0f, 0f), 5f);
```

---

### 8. Delete, clear, and dispose

Use `DelAgent` to delete an agent. The deletion is processed uniformly before the next simulation step.

```csharp
simulator.DelAgent(agentId);
```

To clear the entire simulator state, call `Clear()`.

```csharp
simulator.Clear();
```

When the simulator is no longer needed, call `Dispose()` to release native resources.

```csharp
simulator.Dispose();
```

---

## 📦 Public API Overview

### 1. Construction and lifecycle

- `JobSimulator()`
- `JobSimulator(int maxNeighborsCapacity, int batchSize)`
- `JobSimulator(int maxNeighborsCapacity, int maxObstacleNeighborsCapacity, int batchSize)`
- `Clear()`
- `Dispose()`

### 2. Step control

- `DoStep()`
- `DoStep(float timeStep)`
- `DoStepAsync()`
- `DoStepAsync(float timeStep)`
- `IsJobRunning()`
- `CheckJobCompletion()`

### 3. Agent management

- `AddAgent(Vector3 position)`
- `AddAgent(Vector3 position, float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector3 velocity)`
- `DelAgent(int agentNo)`
- `SetAgentDefaults(...)`
- `SetAgentMaxNeighbors(...)`
- `SetAgentMaxSpeed(...)`
- `SetAgentNeighborDist(...)`
- `SetAgentPosition(...)`
- `SetAgentPrefVelocity(...)`
- `SetAgentRadius(...)`
- `SetAgentTimeHorizon(...)`
- `SetAgentTimeHorizonObst(...)`
- `SetAgentVelocity(...)`

### 4. Obstacles and queries

- `AddObstacle(IList<Vector3> vertices)`
- `ProcessObstacles()`
- `QueryVisibility(Vector3 point1, Vector3 point2, float radius)`
- `QueryNearAgent(Vector3 point, float radius)`

### 5. Read state

- `GetAgentAgentNeighbor(...)`
- `GetAgentMaxNeighbors(...)`
- `GetAgentMaxSpeed(...)`
- `GetAgentNeighborDist(...)`
- `GetAgentNumAgentNeighbors(...)`
- `GetAgentNumObstacleNeighbors(...)`
- `GetAgentObstacleNeighbor(...)`
- `GetAgentOrcaLines(...)`
- `GetAgentPosition(...)`
- `GetAgentPrefVelocity(...)`
- `GetAgentRadius(...)`
- `GetAgentTimeHorizon(...)`
- `GetAgentTimeHorizonObst(...)`
- `GetAgentVelocity(...)`
- `GetGlobalTime()`
- `GetNumAgents()`
- `GetNumObstacleVertices()`
- `GetNumWorkers()`
- `GetObstacleVertex(...)`
- `GetNextObstacleVertexNo(...)`
- `GetPrevObstacleVertexNo(...)`
- `GetTimeStep()`
- `SetGlobalTime(...)`
- `SetTimeStep(...)`
- `SetNumWorkers(...)`

---

## 🌟 Example: Units follow a target and avoid obstacles automatically

```csharp
using System.Collections.Generic;
using RVO.JobSystem;
using UnityEngine;

public class RvoDemo : MonoBehaviour
{
    private JobSimulator simulator;
    private int agentId;

    private void Start()
    {
        simulator = new JobSimulator();
        simulator.SetAgentDefaults(10f, 8, 4f, 4f, 0.5f, 2f, Vector3.zero);

        agentId = simulator.AddAgent(new Vector3(-4f, 0f, 0f));

        simulator.AddObstacle(new List<Vector3>
        {
            new Vector3(-1f, 0f, -2f),
            new Vector3(1f, 0f, -2f),
            new Vector3(1f, 0f, 2f),
            new Vector3(-1f, 0f, 2f),
        });
        simulator.ProcessObstacles();
    }

    private void Update()
    {
        if (simulator == null)
        {
            return;
        }

        Vector3 target = new Vector3(6f, 0f, 0f);
        Vector3 position = simulator.GetAgentPosition(agentId);
        Vector3 desiredVelocity = (target - position).normalized * simulator.GetAgentMaxSpeed(agentId);

        simulator.SetAgentPrefVelocity(agentId, desiredVelocity);
        simulator.DoStep(Time.deltaTime);

        transform.position = simulator.GetAgentPosition(agentId);
    }

    private void OnDestroy()
    {
        simulator?.Dispose();
    }
}
```

## 🔧 Non-Job Native Version

The project also keeps an implementation using native multithreading, which requires the `ENABLE_RVO_WORKER` macro, as well as a single-threaded implementation. The interfaces are basically the same. Basic test code is provided in the `Test` directory.

---

## ⚙️ Typical Call Order

1. Create `JobSimulator`
2. Call `SetAgentDefaults`
3. Add agents with `AddAgent`
4. Add obstacles with `AddObstacle`
5. Call `ProcessObstacles`
6. Set target velocity every frame with `SetAgentPrefVelocity`
7. Call `DoStep` or `DoStepAsync`
8. Read positions with `GetAgentPosition`
9. Call `Dispose` when finished

---

## ℹ️ Notes

1. This implementation is more suitable for horizontal-plane movement and is not a full 3D terrain avoidance solution.
2. After adding obstacles, if the obstacle structure changes, it is recommended to call `ProcessObstacles()` again.
3. `DelAgent` performs delayed deletion. The actual removal happens before the next simulation step.
4. When using `DoStepAsync()`, make sure `CheckJobCompletion()` reports completion before depending on the next result.
5. Remember to call `Dispose()` when the object is destroyed to avoid NativeArray leaks.

---

## ✅ Suitable Scenarios

- Local avoidance after pathfinding for RTS or tactics-game units
- Group movement on the same plane
- Dynamic units avoiding each other
- A local collision-avoidance layer used together with navigation meshes or waypoint systems
- Multi-agent avoidance scenarios that require parallel computation with the Unity Job System
