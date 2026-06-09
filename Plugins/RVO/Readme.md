# RVO避障系统

## 🙏 源码来源
https://gamma.cs.unc.edu/RVO2/
感谢项目作者及贡献者的杰出工作

## 😀 概述

RVO（Reciprocal Velocity Obstacles）是一套用于多智能体动态避障的运动规划工具，适合单位在同一移动平面内相互绕行、绕开静态障碍物的场景。

当前项目中的实现基于 `JobSimulator`，特点如下：

- 支持多智能体之间的互相避让
- 支持静态障碍物边线避障
- 支持 KdTree 邻居搜索
- 支持 JobSystem 并行计算
- 提供同步与异步两种步进方式

> 说明：该实现的避障几何本质上仍是二维平面逻辑，适合单位在水平平面上的移动，使用时需要对输入/输出的坐标系进行转换

---

## 🛠️ 使用

### 1. 创建模拟器

使用 `JobSimulator` 创建一个避障模拟器实例。

```csharp
using System.Collections.Generic;
using RVO.JobSystem;
using UnityEngine;

var simulator = new JobSimulator();
```

如果你希望控制邻居容量或批处理大小，也可以使用带参数构造：

```csharp
var simulator = new JobSimulator(maxNeighborsCapacity: 16, maxObstacleNeighborsCapacity: 16, batchSize: 32);
```

---

### 2. 设置默认智能体参数

在添加智能体之前，通常先调用 `SetAgentDefaults`，为后续创建的智能体指定默认参数。

```csharp
simulator.SetAgentDefaults(
    neighborDist: 15f,        // 邻居搜索半径：越大，考虑的其他单位越多
    maxNeighbors: 10,         // 最大邻居数：越大，避障更稳，但计算更重
    timeHorizon: 5f,          // 面向其他单位的预测时间窗：越大越保守
    timeHorizonObst: 5f,      // 面向静态障碍的预测时间窗：越大越保守
    radius: 1.5f,             // 单位半径：参与碰撞与避障计算的身体尺寸
    maxSpeed: 2f,             // 最大速度：单位移动上限
    velocity: Vector3.zero);  // 初始速度：通常为零
```

参数含义可以直接理解为：

- `neighborDist`：决定从多远范围内寻找其他单位作为避障对象。
- `maxNeighbors`：限制最多记录多少个邻居，避免每帧邻居列表过大。
- `timeHorizon`：控制对其他单位的预判时间，值越大越早开始绕行。
- `timeHorizonObst`：控制对静态障碍物的预判时间，值越大越早避让。
- `radius`：单位的碰撞半径，会影响是否擦边、是否让行。
- `maxSpeed`：单位允许的最高移动速度。
- `velocity`：初始速度，通常在生成时设为零或给一个初始移动方向。

---

### 3. 添加智能体

调用 `AddAgent` 添加一个智能体。返回值是该智能体的 `agentId`。

```csharp
int agentId = simulator.AddAgent(new Vector3(0f, 0f, 0f));
```

也可以显式传入完整参数：

```csharp
int agentId = simulator.AddAgent(
    new Vector3(0f, 0f, 0f),  // 初始位置
    neighborDist: 12f,        // 邻居搜索半径
    maxNeighbors: 8,          // 最大邻居数
    timeHorizon: 4f,          // 其他单位预测时间窗
    timeHorizonObst: 4f,      // 障碍物预测时间窗
    radius: 0.5f,             // 单位半径
    maxSpeed: 3f,             // 最大速度
    velocity: Vector3.zero);  // 初始速度
```

---

### 4. 添加静态障碍物

调用 `AddObstacle` 传入按顺序排列的顶点列表，构成一个折线障碍物段集合。
以逆时针顺序输入顶点列表，会在系统中插入一个正常障碍物（agent不能进入其中）。以顺时针顺序输入顶点列表，会在系统中插入一个包围盒（agent不能离开其中）。

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

添加完成后，建议调用 `ProcessObstacles()` 会构建障碍物树结构，让障碍物进入可见性查询流程。如果不需要/有其他障碍物可见性检查则不需要。

```csharp
simulator.ProcessObstacles();
```

---

### 5. 设置目标速度并执行步进

通常每帧先设置智能体的期望速度，再调用 `DoStep()` 执行一次完整模拟。

```csharp
// 读取agent位置赋值给gameObject
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    var pos = simulator.GetAgentPosition(id);
    pos.z = 0f;
    _agentViews[i].position = pos;
}
// 更新agent速度
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    simulator.SetAgentPrefVelocity(id, new Vector3(1f, 0f, 0f));
}
float globalTime = simulator.DoStep();
```

如果你希望把计算拆成异步执行，可以使用：

```csharp

// 检查前一个job是否完成
if (!simulator.CheckJobCompletion())
{
    return;
}
// 读取agent位置赋值给gameObject
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    var pos = simulator.GetAgentPosition(id);
    pos.z = 0f;
    _agentViews[i].position = pos;
}
// 更新agent速度
for (var i = 0; i < _agentIds.Count; i++)
{
    var id = _agentIds[i];
    simulator.SetAgentPrefVelocity(id, new Vector3(1f, 0f, 0f));
}

simulator.DoStepAsync();

```

---

### 6. 读取智能体状态

模拟完成后，可以查询智能体的位置、速度和邻居信息。

```csharp
Vector3 position = simulator.GetAgentPosition(agentId);
Vector3 velocity = simulator.GetAgentVelocity(agentId);
int agentNeighborCount = simulator.GetAgentNumAgentNeighbors(agentId);
int obstacleNeighborCount = simulator.GetAgentNumObstacleNeighbors(agentId);
```

如果需要读取具体邻居：

```csharp
int neighborAgentId = simulator.GetAgentAgentNeighbor(agentId, 0);
int obstacleNeighborIndex = simulator.GetAgentObstacleNeighbor(agentId, 0);
```

---

### 7. 查询可见性和最近智能体

`QueryVisibility` 用于判断两点之间是否被障碍物阻挡。

```csharp
bool visible = simulator.QueryVisibility(
    new Vector3(-3f, 0f, 0f),
    new Vector3(3f, 0f, 0f),
    radius: 0.1f);
```

`QueryNearAgent` 用于在指定半径内寻找最近智能体。

```csharp
int nearAgent = simulator.QueryNearAgent(new Vector3(0f, 0f, 0f), 5f);
```

---

### 8. 删除、清空和释放

删除智能体使用 `DelAgent`，它会在下一次步进前统一处理。

```csharp
simulator.DelAgent(agentId);
```

如果要清空整个模拟器状态，调用 `Clear()`。

```csharp
simulator.Clear();
```

当模拟器不再使用时，调用 `Dispose()` 释放 Native 资源。

```csharp
simulator.Dispose();
```

### 9. AgentType Feature
可以为agent添加配类型标识，在**simulator**中使用一维 `NativeArray<float>` 扁平化存二维矩阵，保存agentType之间的额外半径，推荐映射一个enum表示类型关系且从0开始顺序递增，避免二维矩阵过大。

使用方法：
```csharp
// 定义AgentType枚举
public enum TestAgentType
{
    Red = 0,
    Blue,
    Green
}

// 首先向**simulator**注册AgentType数量
simulator.ConfigAgentTypes(Enum.GetValues(typeof(TestAgentType)).Length); 

// 注册agentType对其他agentType的半径
simulator.ConfigAgentExtraRadii((int)TestAgentType.Red, (int)TestAgentType.Green, 0.7f);
simulator.ConfigAgentExtraRadii((int)TestAgentType.Red, (int)TestAgentType.Blue, 2f);
simulator.ConfigAgentExtraRadii((int)TestAgentType.Green, (int)TestAgentType.Blue, 1.5f);
simulator.ConfigAgentExtraRadii((int)TestAgentType.Red, (int)TestAgentType.Red, 0.5f);
simulator.ConfigAgentExtraRadii((int)TestAgentType.Blue, (int)TestAgentType.Red, 0.8f);

```

PS：如果只指定 `agentTypeA` -> `agentTypeB` 有额外半径，在运行时会出现 `agentTypeA` 和 `agentTypeB` 相遇时，A提前避开而B继续靠近，如果想获得更好的避障效果，最好同时设置 `agentTypeB` -> `agentTypeA` 的额外半径。

---

## 📦 公开接口一览

### 1. 构造与生命周期

- `JobSimulator()`
- `JobSimulator(int maxNeighborsCapacity, int batchSize)`
- `JobSimulator(int maxNeighborsCapacity, int maxObstacleNeighborsCapacity, int batchSize)`
- `Clear()`
- `Dispose()`

### 2. 步进控制

- `DoStep()`
- `DoStep(float timeStep)`
- `DoStepAsync()`
- `DoStepAsync(float timeStep)`
- `IsJobRunning()`
- `CheckJobCompletion()`

### 3. 智能体管理

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

### 4. 障碍物与查询

- `AddObstacle(IList<Vector3> vertices)`
- `ProcessObstacles()`
- `QueryVisibility(Vector3 point1, Vector3 point2, float radius)`
- `QueryNearAgent(Vector3 point, float radius)`

### 5. 读取状态

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

## 🌟 示例：单位跟随目标并自动避障

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

## 🔧 非Job原生版本
项目中保留了使用原生多线程（需要开启`ENABLE_RVO_WORKER`宏）/单线程的实现，接口基本一致。在【Test】目录中提供基础测试代码。

---

## ⚙️ 典型调用顺序

1. 创建 `JobSimulator`
2. 调用 `SetAgentDefaults`
3. 添加智能体 `AddAgent`
4. 添加障碍物 `AddObstacle`
5. 调用 `ProcessObstacles`
6. 每帧设置目标速度 `SetAgentPrefVelocity`
7. 调用 `DoStep` 或 `DoStepAsync`
8. 读取位置 `GetAgentPosition`
9. 结束时调用 `Dispose`

---

## ℹ️ 注意事项

1. 这套实现更适合水平平面移动，不是完整的三维地形避障。
2. 添加障碍物后，如果障碍结构变化，建议重新调用 `ProcessObstacles()`。
3. `DelAgent` 是延迟删除，真正移除发生在下一次步进前。
4. 若使用 `DoStepAsync()`，请在下一次依赖结果前先确认 `CheckJobCompletion()` 为完成状态。
5. 记得在对象销毁时调用 `Dispose()`，避免 NativeArray 泄漏。

---

## ✅ 适用场景

- RTS 或战棋单位寻路后的局部避障
- 同一平面上的群体移动
- 动态单位互相绕行
- 配合导航网格、路径点系统使用的局部避碰层
- 需要 JobSystem 并行计算的多智能体避障场景
