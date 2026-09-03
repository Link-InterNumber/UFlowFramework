# Loader Profiler 加载分析工具

## 1. 系统概述

Loader Profiler 是基于 Unity 原生 Profiler 模块 API 实现的资源加载分析工具，用于记录加载请求，并在 Profiler 选中的帧中查看 Asset、AssetBundle、依赖深度和加载状态等信息。

系统的 Runtime 部分负责收集加载样本、维护活动加载索引并写入 Profiler；Editor 部分负责注册 Profiler 模块，通过 `FrameDataView` 读取指定帧的元数据并构建详情界面。

当前实现主要面向 Unity Editor 中的 Profiler 调试，不是运行时资源加载器，也不会替代 AssetBundle、Addressables 或其他资源管理系统。

### 主要特点

- 使用 `ProfilerModule` 和 `ProfilerModuleViewController` 集成到 Unity Profiler，而不是创建独立窗口。
- 使用 `ProfilerCounterValue<int>` 提供加载数量、Bundle 数量和依赖深度等时间线数据。
- 使用 `Profiler.EmitFrameMetaData` 保存选中帧的详细加载记录。
- 使用 `FixedString4096Bytes` 和整数类型组成可传输的 blittable 元数据结构。
- 使用 `LoadSamplePool` 回收活动加载样本，减少重复创建对象。
- Editor 侧提供样本列表、Bundle 汇总、状态分布图和依赖信息显示。

### 主要组件 / 接口

- `LoadSampleCollector`：收集和维护活动加载样本，并向 Unity Profiler 写入计数器与帧元数据。
- `LoadSample`：表示一个资源加载请求及其状态、路径和依赖信息。
- `LoadSamplePool`：获取、重置和回收 `LoadSample`。
- `ILoadDependencyProvider`：定义资源依赖和 AssetBundle 依赖查询能力。
- `EditorLoadDependencyProvider`：使用 `AssetDatabase` 实现 Editor 依赖查询。
- `LoadSampleCollectorLifecycle`：通过 Unity 生命周期创建、刷新和释放 `LoadSampleCollector`。
- `LoadProfilerModule`：注册 Loader Profiler 图表计数器并创建详情视图控制器。
- `LoaderProfilerModuleViewController`：读取选中帧的元数据并显示分析结果。
- `LoadProfilerFrameData`：用于 `Profiler.EmitFrameMetaData` 的 blittable 帧数据结构。

### 组件依赖关系

```text
LoadSampleCollectorLifecycle
    |
    └──> LoadSampleCollector
            |
            |──> LoadSample
            |       |
            |       └──> LoadSamplePool
            |
            |──> ILoadDependencyProvider
            |       |
            |       └──> EditorLoadDependencyProvider
            |               |
            |               └──> UnityEditor.AssetDatabase
            |
            |──> LoadProfilerFrameData
            |
            └──> Unity Profiler APIs
                    |
                    |──> ProfilerCounterValue<int>
                    |──> ProfilerMarker
                    └──> Profiler.EmitFrameMetaData

LoadProfilerModule
    |
    └──> LoaderProfilerModuleViewController
            |
            |──> UnityEditorInternal.ProfilerWindow
            |──> UnityEditorInternal.ProfilerDriver
            |──> LoadProfilerFrameData
            └──> LoadSampleCollector
                    |
                    └──> ILoadDependencyProvider
```

其中，组件树表示代码中的引用和持有关系；初始化、采集和释放顺序见下一节，不将执行时序与组件依赖混为一谈。

## 2. 工作原理与优化

### 2.1 初始化和生命周期

在 Unity Editor 中，场景中的 `LoadSampleCollectorLifecycle` 会在 `Awake()` 中创建 `LoadSampleCollector`，并传入 `EditorLoadDependencyProvider`：

```csharp
_collector = new LoadSampleCollector(new EditorLoadDependencyProvider());
LoadSampleCollector.instance = _collector;
```

`LoadSampleCollectorLifecycle` 使用 `DontDestroyOnLoad` 保持对象跨场景存在，并在 `OnDestroy()` 或 `OnApplicationQuit()` 中释放收集器。

当前脚本将生命周期实现放在 `#if UNITY_EDITOR` 中，因此 Player 构建不会执行这套收集流程。若需要在 Development Build 或远程 Player 中采集，需要提供不依赖 `UnityEditor` 的 Runtime 生命周期和依赖查询实现。

### 2.2 加载样本采集

开始加载时调用 `LoadSampleCollector.BeginLoad`。收集器会：

1. 初始化当前 Profiler 帧的 Bundle 统计状态；
2. 检查 `hashCode` 是否已经存在；
3. 检查 `Profiler.enabled`；
4. 从 `LoadSamplePool` 获取 `LoadSample`；
5. 保存资源路径、Bundle 名称、Unity 帧号和请求哈希值；
6. 通过 `ILoadDependencyProvider` 查询依赖；
7. 将样本加入活动列表和哈希索引。

活动样本同时由两个集合管理：

- `_loadSamples`：用于遍历当前活动样本和构造元数据；
- `_loadSampleDict`：用于通过 `hashCode` 快速查询状态。

### 2.3 状态更新和清理

`LoadState` 使用 `[Flags]` 枚举表示加载阶段。调用 `SetLoadState` 时，新的状态通过按位或合并到现有状态中：

```csharp
sample.loadState = sample.loadState | state;
```

当样本包含 `LoadState.End` 时，`ClearEndSample` 会在帧末将其从活动列表和哈希索引移除，并归还到 `LoadSamplePool`。

帧末流程由 `LoadSampleCollectorLifecycle.LateUpdate()` 驱动：

1. `FlushProfilerFrame()` 写入当前活动样本和计数器；
2. `ClearEndSample()` 清理已结束样本；
3. `EnsureProfilerFrame()` 准备下一帧的 Bundle 统计。

### 2.4 Profiler 数据写入

`LoadProfilerModule` 注册以下五个图表计数器：

- `Loader Active Loads`
- `Loader Begin Loads`
- `Loader Completed Loads`
- `Loader AssetBundles`
- `Loader Max Dependency Depth`

详细样本则通过 `LoadProfilerFrameData` 写入帧元数据。由于 `Profiler.EmitFrameMetaData<T>` 要求数据结构满足 blittable 约束，元数据不直接保存 `string` 或 `string[]`，而是使用：

- `FixedString4096Bytes` 保存路径和 Bundle 名称；
- `int` 保存帧号、对象哈希、状态和是否为本帧新建样本。

Editor 读取后，将固定字符串转换为普通字符串，并通过依赖提供器获取用于界面展示的依赖数组。

### 2.5 Editor 选帧查询

`LoaderProfilerModuleViewController` 监听 `ProfilerWindow.SelectedFrameIndexChanged`。选中帧变化后，使用：

```csharp
ProfilerDriver.GetRawFrameDataView(frameIndex, 0)
```

获取对应的 `FrameDataView`，再使用 `GetFrameMetaData<LoadProfilerFrameData>` 查询 Loader Profiler 写入的元数据。

详情视图将数据转换为 `LoadProfilerModuleViewController.LoadProfilerFrameDataDisplay`，然后完成：

- 样本数量和活动数量统计；
- Bundle 分组汇总；
- 最大依赖深度计算；
- Begin、Loading、End 状态分布；
- 依赖列表格式化；
- `ListView` 虚拟化显示。

### 2.6 缓存和分配控制

系统当前的主要优化包括：

- 使用哈希字典避免按列表线性查找加载请求；
- 使用对象池减少 `LoadSample` 的频繁分配；
- `EditorLoadDependencyProvider` 缓存已经查询过的依赖结果；
- 使用 `_lastMetadataFrame` 避免同一帧重复写入元数据；
- 使用 `ListView` 虚拟化列表显示样本；
- 使用固定字符串结构满足 Profiler 元数据的非托管传输要求。

这些优化要求请求哈希值在活动加载期间保持唯一，且回收到对象池的 `LoadSample` 不能再被外部持有或使用。

## 3. 使用方法

### 3.1 接入生命周期组件

在场景中创建一个 GameObject，例如 `LoaderProfilerRuntime`，并添加组件：

```csharp
LoadSampleCollectorLifecycle
```

当前实现仅在 Unity Editor 中启用。进入 Play Mode 后，该组件会自动创建收集器并设置：

```csharp
LoadSampleCollector.instance
```

### 3.2 记录一次加载流程

下面示例展示一个最小的加载记录闭环：

```csharp
using PowerCellStudio;

var collector = LoadSampleCollector.instance;
if (collector == null)
    return;

collector.BeginLoad("Assets/Characters/Hero.prefab", "characters", requestHashCode);

collector.SetLoadState(requestHashCode, LoadState.LoadingBundle);

collector.SetLoadState(requestHashCode, LoadState.LoadingAsset);

// 资源加载完成
collector.SetLoadState(requestHashCode, LoadState.End);
```

示例中的 `requestHashCode` 必须在活动加载期间唯一，并且 `BeginLoad` 与后续 `SetLoadState` 使用相同的值。

### 3.3 在 Profiler 中查看

1. 在 Unity Editor 中打开 Profiler 窗口。
2. 进入 Play Mode 或运行场景。
3. 触发资源加载流程。
4. 在 Profiler 模块列表中选择 `Loader Profiler`。
5. 在时间线上选择目标帧。
6. 查看计数器曲线和详情视图中的样本、Bundle、依赖深度及状态分布。
7. 需要查看依赖明细时，启用 `Show Dependencies`。

### 3.4 生命周期结束

通常不需要手动释放场景组件。`LoadSampleCollectorLifecycle` 会在对象销毁或应用退出时调用 `Dispose`，清理：

- 活动 `LoadSample`；
- 等待加入的样本；
- `LoadSamplePool`；
- Bundle 统计字典；
- 依赖提供器缓存。

## 4. 扩展方法

### 4.1 自定义依赖查询实现

系统提供的公共扩展点是 `ILoadDependencyProvider`。如果资源系统不是基于 Editor `AssetDatabase`，可以实现自己的依赖查询器。

下面示例是一个基于预先提供的依赖表的最小实现：

```csharp
using System;
using System.Collections.Generic;
using PowerCellStudio;

public sealed class TableLoadDependencyProvider : ILoadDependencyProvider
{
    private readonly IReadOnlyDictionary<string, string[]> _assetDependencies;
    private readonly IReadOnlyDictionary<string, string[]> _bundleDependencies;

    public TableLoadDependencyProvider(
        IReadOnlyDictionary<string, string[]> assetDependencies,
        IReadOnlyDictionary<string, string[]> bundleDependencies)
    {
        _assetDependencies = assetDependencies;
        _bundleDependencies = bundleDependencies;
    }

    public string[] GetAssetDependencies(string assetPath)
    {
        return assetPath != null && _assetDependencies.TryGetValue(assetPath, out var dependencies)
            ? dependencies
            : null;
    }

    public string[] GetAssetBundleDependencies(string assetBundleName)
    {
        return assetBundleName != null && _bundleDependencies.TryGetValue(assetBundleName, out var dependencies)
            ? dependencies
            : null;
    }

    public void Dispose()
    {
    }
}
```

将自定义实现传入收集器即可：

```csharp
var collector = new LoadSampleCollector(new TableLoadDependencyProvider(assetDependencies, bundleDependencies));
```

扩展实现需要遵守以下不变量：

- `GetAssetDependencies` 和 `GetAssetBundleDependencies` 不应修改传入的 key；
- 空路径或空 Bundle 名称应返回 `null` 或空数组；
- 返回的依赖数组在收集器使用期间必须保持有效；
- 如果实现持有缓存、文件句柄或其他资源，必须在 `Dispose()` 中释放；
- 依赖查询应在 Unity 主线程执行，除非自定义实现明确保证线程安全。

### 4.2 扩展 Profiler 展示

`LoaderProfilerModule` 可以通过增加 `ProfilerCounterDescriptor` 扩展图表指标，`LoaderProfilerModuleViewController` 可以扩展详情面板和数据显示结构。

新增 Runtime 元数据字段时必须同时修改：

1. `LoadProfilerFrameData`；
2. Runtime 的元数据写入逻辑；
3. Editor 的 `GetFrameMetaData<T>` 读取类型；
4. Editor 侧的数据转换和界面绑定。

Runtime 和 Editor 使用的元数据结构必须保持完全一致，否则 `FrameDataView` 无法正确解释历史帧数据。

## 5. 注意事项

### Editor 与 Runtime 边界

`EditorLoadDependencyProvider` 使用 `UnityEditor.AssetDatabase`，且当前被 `#if UNITY_EDITOR` 包围。不要在 Runtime 程序集的非条件代码中直接引用该类型。

`LoadSampleCollectorLifecycle` 当前也只在 `UNITY_EDITOR` 下执行。因此 Player 构建中不会创建收集器、查询 Editor 依赖或写入这套 Profiler 元数据。

### 空值和固定字符串

`FixedString4096Bytes` 不能接收 `null` 字符串。写入元数据前，资源路径和 Bundle 名称需要使用非空字符串，例如 `string.Empty`。

路径长度还必须满足 `FixedString4096Bytes` 的容量限制，超过容量时应在业务层决定截断、拒绝或记录摘要的策略。

### 请求哈希值

`hashCode` 同时用于 `_loadSampleDict` 的索引。相同哈希值的活动请求会被视为重复请求，导致新的 `BeginLoad` 被忽略。因此请求哈希值必须在活动生命周期内唯一。

### 帧数据时序

Profiler 图表计数器和元数据必须在相同的帧边界提交。当前生命周期在 `LateUpdate()` 中先刷新 Profiler，再清理结束样本；如果改变调用顺序，可能出现时间线数据与详情面板数据错帧。

`ProfilerWindow.selectedFrameIndex` 是 Profiler 捕获帧索引，不是 `Time.frameCount`。详情视图应始终通过 `ProfilerDriver.GetRawFrameDataView` 查询选中的捕获帧。

### 依赖查询和性能

依赖查询结果会被缓存，但首次查询仍可能产生较高开销。不要在每个 `Update` 中主动清空缓存，也不要在加载请求外重复查询同一资源的完整递归依赖。

Editor 详情视图读取历史帧时，如果依赖提供器已经销毁，依赖数组可能为空。此时应保证界面能够正常显示基本路径和状态信息。

### 对象池生命周期

`LoadSample` 归还对象池后会执行 `Reset()`。不要在 `ClearEndSample()` 之后继续保存或访问已结束样本的引用。

`LoadSampleCollector` 和 `LoadSamplePool` 释放后不可继续调用；重复释放或在释放后调用 `Get`、`Release` 可能导致空引用异常。

### 元数据兼容性

`LoadProfilerFrameData` 是 Runtime 写入和 Editor 读取之间的二进制契约。修改字段顺序、字段类型或字段数量后，应重新采集数据，不能假设旧的 Profiler 捕获数据仍然兼容。

## 6. 推荐使用场景

- **定位某一帧的资源加载峰值**：通过时间线计数器快速发现活动加载数量、Bundle 数量或依赖深度异常的帧。
- **分析 AssetBundle 依赖链**：在详情视图中按 Bundle 汇总样本，并展开依赖信息。
- **排查加载状态卡住**：查看样本是否长期停留在 `LoadingBundle` 或 `LoadingAsset`，辅助定位状态更新遗漏。
- **比较不同加载方案的 Profiler 数据**：使用统一的计数器和元数据结构对比不同帧的加载数量和依赖深度。
- **Editor 工具开发和资源导入调试**：借助 `EditorLoadDependencyProvider` 直接使用 `AssetDatabase` 查询资源依赖。

不推荐将当前实现直接用于正式 Player 运行时监控，因为生命周期和默认依赖查询器依赖 Unity Editor。若需要运行时或远程设备采集，应保留 Runtime 收集核心，替换为不依赖 `UnityEditor` 的依赖提供器和生命周期实现。
