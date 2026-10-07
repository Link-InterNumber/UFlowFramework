# BundleReferenceViewer

## 1. 系统概述

`BundleReferenceViewer` 是一个 Unity Editor 工具，用于分析当前工程中的 AssetBundle 引用关系、资源依赖关系和常见 Bundle 结构缺陷，并以图形或文本形式查看分析结果。它面向编辑器分析和构建前检查，不参与运行时资源加载，也不会自动修改 AssetBundle 配置。

主要特点：

- 从 `AssetDatabase` 收集 Bundle 及其直接依赖，并同时建立“依赖”和“被引用”两个方向的索引。
- 按资源建立跨 Bundle 的直接依赖关系，可在图中切换 Bundle 简化模式和资源级关系模式。
- 通过缺陷检测器组合识别单引用单资源、引用分散或冗余、依赖链路过长和循环依赖。
- 支持同步分析、分批异步分析，以及将 Bundle 关系和缺陷结果保存为二进制文件后再查看。
- 支持按连通关系划分 Bundle 组，并按 Bundle、组和缺陷等级筛选分析结果。

### 主要组件 / 接口

- `BundleReferenceViewerWindow`：Bundle 引用关系图形窗口，负责生成分析、筛选 Bundle、选择显示范围和控制图形视图。
- `BundleReferenceGraphView`：基于 Unity GraphView 创建 Bundle 节点、资源节点及其连接，并执行布局和关系高亮。
- `BundleReferenceQueryer`：保存 Bundle、资源、分组和缺陷索引，提供查询与释放能力。
- `QueryerFactory`：从当前工程、已构建数据或序列化数据创建 `BundleReferenceQueryer`。
- `BundleReferenceAnalyzer` / `AssetReferenceCollector`：收集 Bundle 和资源依赖，并驱动分组与资源分析。
- `BundleDefectDetectorBox`：持有缺陷检测器集合，将检测结果写回 Bundle 和分组数据。
- `IBundleDefectDetector`：缺陷检测扩展契约，定义单 Bundle 检测和分组检测行为。
- `BundleReferenceExporter`：执行完整分析并生成 `BundleReferenceReport` 二进制报告。
- `ReferenceWriter` / `ReferenceReader`：使用 `IBundleReferenceBinary` 完成分析数据的二进制写入和读取。

### 组件依赖关系

```text
BundleReferenceViewerWindow
|
|──> BundleReferenceGraphView
|       |
|       └──> BundleReferenceBundleNode
|               |
|               └──> AssetReferenceNode
|
|──> QueryerFactory
|       |
|       └──> BundleReferenceQueryer
|               |
|               |──> BundleReferenceData
|               |──> AssetReferenceData
|               └──> BundleReferenceGroup
|
└──> BundleDefectDetectorBox
        |
        └──> IBundleDefectDetector
                |
                |──> SingleReferenceSingleAssetDefectDetector
                |──> ReferencesScatteredDefectDetector
                |──> DeepDependencyDefectDetector
                └──> CircularBundleReferenceDefectDetector

BundleReferenceExporter
|
|──> BundleReferenceAnalyzer
|       |
|       └──> AssetReferenceCollector
|
|──> BundleDefectDetectorBox（同上）
|
└──> ReferenceWriter
        |
        └──> BundleReferenceReport
```

依赖树表示类型之间的持有、参数传递、实现或实际调用关系：窗口持有查询器、检测器盒和图视图；`QueryerFactory` 创建查询器；`BundleDefectDetectorBox` 创建并调用检测器；导出器通过分析器、检测器盒和 `ReferenceWriter` 生成报告。初始化、分析、显示和释放的先后顺序见下一节，不在此树中用时序关系代替组件层级。

## 2. 工作原理与优化

### 当前工程分析流程

1. `BundleReferenceViewerWindow.GenerateGraph` 创建或重建 `BundleReferenceQueryer`，调用 `QueryerFactory.GenerateQueryerByCurrentProjectSync`。
2. `QueryerFactory` 使用 `AssetDatabase.GetAllAssetBundleNames` 枚举 Bundle，再用 `AssetDatabase.GetAssetBundleDependencies` 写入每个 Bundle 的依赖关系。
3. `BundleReferenceQueryer.AddBundleData` 同时建立 `bundleDependent` 和 `bundleReferenced`，因此可以从依赖方或被引用方遍历关系。
4. `BundleReferenceAnalyzer.DetectorGroupDefect` 调用 `EnsureGroups`，通过双向邻接关系将 Bundle 划分为连通组，然后收集组内资源并执行 Bundle 缺陷检测。
5. `CollectGroupAssetData` 使用 `AssetDatabase.GetAssetPathsFromAssetBundle` 获取资源，`AssetReferenceCollector.FindDirectReferences` 再用 `AssetDatabase.GetDependencies(path, false)` 收集直接资源依赖。
6. `BundleReferenceQueryer.AddAsset` 合并重复资源的依赖信息，并为依赖资源补充反向引用索引。
7. 用户选择组或 Bundle 后，`BundleReferenceGraphView` 创建 Bundle 节点；普通模式连接资源节点，简化模式则直接连接 Bundle 节点。
8. 选择单个 Bundle 时，图视图按关系层数从当前 Bundle 向 `bundleDependent` 和 `bundleReferenced` 两个方向收集可见节点；选择整个组时显示组内全部 Bundle。

### 缺陷检测

`BundleDefectDetectorBox` 当前默认启用四个检测器：

- `SingleReferenceSingleAssetDefectDetector`：低等级，检测只包含一个资源且仅被一个 Bundle 引用的 Bundle。
- `ReferencesScatteredDefectDetector`：中等级，检测同一 Bundle 的资源被多个外部 Bundle 以不同资源集合分散引用的情况。
- `DeepDependencyDefectDetector`：中等级，检测从当前 Bundle 出发超过 5 层的依赖链路。
- `CircularBundleReferenceDefectDetector`：高等级，使用 DFS 检测依赖图中的循环引用，递归检测深度上限为 6。

检测结果会写入 `BundleReferenceData.defectLevel`、`tags` 和 `defectDetail`。分组检测结果则聚合到 `BundleReferenceGroup.defectLevel` 和 `defectInfos`，用于列表颜色、图形提示按钮和工具提示展示。

### 缓存、异步和内存策略

- `BundleReferenceQueryer` 使用字典按 Bundle 名称、组名称和资源路径建立索引，使图视图和检测器可以直接查询，而不必重复遍历全部数据。
- 资源依赖使用 `HashSet<string>` 去重，避免同一依赖路径被重复记录。
- `AssetReferenceData` 的部分集合使用 `HashSetPool<string>`，缺陷检测器也使用 Unity 的集合池减少临时集合分配。
- 异步入口 `GenerateQueryerByCurrentProject` 按 `analysisBatchSize` 调用 `Task.Yield`，使编辑器能够分批让出执行权；资源收集阶段还会显示可取消进度条。
- 窗口使用 `_analysisVersion` 防止旧分析结果在新一轮分析后继续写入界面。
- `BundleReferenceExporter` 可只在内存中分析，也可写出 `BundleReferenceReport`；写出后的数据能够通过 `QueryerFactory.GenerateQueryerBySerializedData` 恢复 Bundle 关系和缺陷标签。

这些优化带来相应约束：字典和集合中的数据由 `BundleReferenceQueryer` 统一拥有，分析结束必须调用 `Dispose`；异步流程仍然调用 Unity Editor API，不能因此推断为可在后台线程执行。

## 3. 使用方法

### 3.1 在编辑器中生成关系图

使用统一菜单入口打开 `BundleReferenceViewerWindow`，然后按以下步骤操作：

1. 点击“生成分析”，从当前工程的 AssetBundle 配置创建查询数据。
2. 在左侧组列表中展开目标组，选择“显示所有 Bundle”或选择单个 Bundle。
3. 使用“简化模式”只查看 Bundle 之间的关系；关闭后查看 Bundle 内资源节点之间的关系。
4. 需要限制单 Bundle 图的范围时，在“关系层数”中设置两侧遍历层数。
5. 点击资源节点可在 Project 窗口中定位资源，并高亮其下游依赖。
6. 使用“重新布局”重新排列当前显示的 Bundle 节点，使用“清空”释放当前分析数据并清除图形。

图形窗口依赖 Unity Editor 的 `AssetDatabase` 和 GraphView API，应在 Unity 主线程的编辑器环境中使用。

### 3.2 导出并查看二进制报告

下面是项目中真实的同步导出入口调用方式，适合从编辑器菜单或其他编辑器工具触发一次完整分析：

```csharp
using PowerCellStudio.Editor;

public static class BundleReferenceAnalysisEntry
{
    public static void Export()
    {
        using var exporter = new BundleReferenceExporter();
        exporter.AnalyzeSync(writeFile: true, showWindow: true);
    }
}
```

`AnalyzeSync` 会重新创建查询器、分析当前工程、生成时间戳命名的二进制报告，并可打开 `BundleReferenceTextViewerWindow`。文本查看器通过 `ReferenceReader.ReadSingle<BundleReferenceReport>` 读取报告并显示缺陷信息。

### 3.3 加载已保存的查询数据

如果只需要恢复已保存的 Bundle 关系和缺陷标签，可以使用 `QueryerFactory.GenerateQueryerBySerializedData`：

```csharp
using PowerCellStudio.Editor;

public static BundleReferenceQueryer LoadAnalysis(string assetPath)
{
    return QueryerFactory.GenerateQueryerBySerializedData(assetPath);
}
```

调用者使用完成后必须释放返回的查询器：

```csharp
using var queryer = QueryerFactory.GenerateQueryerBySerializedData(assetPath);
```

该序列化查询器包含 Bundle 依赖和缺陷标签，但不会恢复完整的资源依赖对象；若需要资源级关系，应重新执行当前工程分析并收集资源数据。

## 4. 扩展方法

### 4.1 添加 Bundle 缺陷检测器

`IBundleDefectDetector` 是当前明确的检测扩展点。实现需要提供标题、提示文本、标签、缺陷等级，并实现 `Detect` 和 `HasDefect`。检测器由 `BundleDefectDetectorBox` 持有，在 `Dispose` 时释放实现了 `IDisposable` 的检测器。

以下示例使用项目中真实的接口和数据类型，检测 Bundle 是否没有任何直接依赖：

```csharp
using PowerCellStudio.Editor;

public sealed class NoDependencyDefectDetector : IBundleDefectDetector
{
    public string title => "无直接依赖";
    public string toolTips => "Bundle 没有直接依赖，需结合项目规则判断是否符合预期。";
    public string tag => "无直接依赖";
    public DefectLevel defectLevel => DefectLevel.Low;

    public bool Detect(BundleReferenceQueryer queryer,
        BundleReferenceData bundleData, out string defectDetail)
    {
        defectDetail = null;
        if (queryer == null || bundleData == null ||
            string.IsNullOrEmpty(bundleData.bundleName))
            return false;

        if (bundleData.bundleDependent == null ||
            bundleData.bundleDependent.Count != 0)
            return false;

        defectDetail = $"Bundle '{bundleData.bundleName}' 没有直接依赖。";
        return true;
    }

    public bool HasDefect(BundleReferenceQueryer queryer,
        BundleReferenceGroup group)
    {
        if (queryer == null || group?.bundleNames == null)
            return false;

        foreach (var bundleName in group.bundleNames)
        {
            if (Detect(queryer, queryer.GetBundleData(bundleName), out _))
                return true;
        }
        return false;
    }
}
```

当前 `BundleDefectDetectorBox` 没有公开注册方法，默认检测器在其构造函数中直接创建。因此接入自定义检测器还需要修改 `BundleDefectDetectorBox` 的初始化列表，将 `new NoDependencyDefectDetector()` 加入其中；仅实现接口不会被自动发现。检测器应保持无状态或自行管理可释放资源，且不能修改 Bundle 依赖集合后再继续依赖原索引。

### 4.2 替换数据来源

`QueryerFactory` 已提供当前工程、已构建 Bundle 和序列化数据三种创建路径，但没有通用的数据源注册接口。若要接入新的数据源，应在项目代码中新增等价的工厂方法，将有效的 Bundle 名称和依赖传递给 `BundleReferenceQueryer.AddBundleData`，并在需要资源级图形时调用 `SetAssets`。这一接入属于项目定制，不存在可直接配置的插件注册机制。

## 5. 注意事项

- `GenerateQueryerByCurrentProject` 和 `GenerateQueryerByCurrentProjectSync` 依赖 `AssetDatabase`，必须在 Unity Editor 中运行；异步方法的 `Task.Yield` 只用于分批让出执行权，不代表 Unity API 可以在后台线程调用。
- `BundleReferenceQueryer`、`BundleReferenceData`、`AssetReferenceData` 和 `BundleReferenceGroup` 均包含可释放的集合或索引。分析窗口关闭、重新生成分析或导出器结束时必须调用 `Dispose`。
- `EnsureGroups` 只在组字典为空时构建分组；如果在同一个查询器生命周期内改变 Bundle 关系，必须重新创建查询器，不能依赖旧分组结果。
- `AddBundleData` 需要有效的依赖数组。当前 `QueryerFactory` 的调用会传入 Unity 返回的数组，但自定义数据源应将空依赖转换为空数组，避免对空引用执行 `UnionWith` 或遍历。
- 序列化报告只保存 `BundleReferenceReport` 中的时间、Bundle 数量和缺陷报告；`BundleReferenceInfo` 路径保存的是 Bundle 依赖和标签，不等同于完整资源关系图。
- `ReferenceReader.Read` 按文件头的数量读取记录，文件必须由兼容的 `ReferenceWriter` 写出；损坏文件或版本不兼容文件可能在读取阶段抛出异常。
- 当前循环依赖检测器的递归深度上限为 6，深度超过该限制的路径不会继续用于判定循环；文档或工具显示的结果应按此实现边界理解。
- 单 Bundle 图的关系层数会通过 `Mathf.Max(0, referenceDepth)` 限制为非负值；设置为 0 时只显示选中的 Bundle。
- 简化模式只连接 Bundle 节点，普通模式只连接当前可见资源节点；被过滤或不在可见范围内的节点不会生成对应连接。
- `BundleReferenceExporter` 的异步分析可通过进度条取消，取消或异常时会清理进度条；外部异步调用仍应等待任务结束并处理异常。
- 缺陷等级是带 `[Flags]` 的组合值，系统展示时优先显示高、中、低中的最高等级，但一个 Bundle 可能同时包含多个缺陷标签。

## 6. 推荐使用场景

- **AssetBundle 配置提交前检查**：生成当前工程分析并查看缺陷等级，尽早发现循环依赖、过深依赖和过度拆包。
- **资源依赖定位**：关闭简化模式查看资源节点，点击资源即可定位 Project 资源，并观察其下游依赖。
- **公共资源拆包评估**：查看某个资源被哪些 Bundle 使用，判断是否存在引用分散、冗余加载或 Bundle 划分不合理。
- **构建前后关系对比**：导出二进制分析报告并在后续流程中恢复查询数据，保存特定版本的缺陷结果。
- **编辑器工具集成**：通过 `BundleReferenceExporter.AnalyzeSync` 将完整分析接入自定义构建检查或编辑器菜单流程。

不推荐将该工具用于运行时依赖查询、运行时资源加载或持续每帧监控；这些需求应使用运行时资源系统或专用性能分析工具。对于超大规模工程，优先使用异步分析或导出报告，避免在编辑器交互期间一次性执行过长的同步分析。
