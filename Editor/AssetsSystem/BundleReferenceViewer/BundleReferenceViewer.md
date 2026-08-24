# Bundle 引用可视化分析器

## 概述

Bundle 引用可视化分析器用于收集、检测和展示 Unity AssetBundle 之间以及 Bundle 内资源之间的引用关系。

该功能包含两个主要部分：

- `BundleReferenceAnalyzer`：扫描当前 Unity 工程，采集 Bundle 资源依赖并执行缺陷检测。
- `BundleReferenceViewerWindow`：在 Unity Editor 中以 GraphView 和列表形式查看分析结果。

分析器支持 Bundle 分组、资源节点、依赖边、缺陷提示、简化模式以及局部引用链层数限制。

## 核心概念

### Bundle 关系

`BundleReferenceData` 保存单个 Bundle 的引用关系：

- `bundleDependent`：当前 Bundle 依赖的 Bundle。
- `bundleReferenced`：引用当前 Bundle 的 Bundle。
- `assets`：当前 Bundle 中采集到的资源数据。
- `defectLevel`：当前 Bundle 的缺陷等级。
- `tags`：当前 Bundle 命中的缺陷标签。

### 资源关系

`AssetReferenceData` 保存单个资源的关系：

- `assetPath`：资源路径。
- `bundleName`：资源所属 Bundle。
- `assetDependent`：当前资源直接依赖的资源路径集合。
- `bundleReferenced`：直接引用当前资源的资源路径集合。

### Group

`BundleReferenceGroup` 是 Bundle 关系图中的连通分组。分组通过 Bundle 的依赖和被引用关系建立。

Group 还保存聚合后的缺陷信息：

```csharp
public struct GroupDefectInfo
{
    public DefectLevel level;
    public int count;
    public string toolTips;
    public string tag;
    public List<string> bundleNames;
}
```

其中 `bundleNames` 用于记录命中指定缺陷的 Bundle 名称。

### 缺陷检测

`BundleDefectDetectorBox` 管理多个 `IBundleDefectDetector` 实现。当前检测器包括：

- `SingleReferenceSingleAssetDefectDetector`
- `ReferencesScatteredDefectDetector`
- `DeepDependencyDefectDetector`
- `HighReferenceCountDefectDetector`
- `CircularBundleReferenceDefectDetector`

检测结果会写入 Bundle 的 `defectLevel`、`tags`，并聚合到 Group 的 `defectInfos`。

## API / 字段 / 方法

### `BundleReferenceAnalyzer`

| 名称 | 类型 | 说明 |
|---|---|---|
| `ReferenceAnalyzerEditorHandler` | `static void` | 通过 Unity 菜单启动异步分析。 |
| `InitAsync` | `Task` | 创建或重新创建 `BundleReferenceQueryer`。 |
| `AnalyzeAsync` | `Task` | 采集资源、检测缺陷并保存分析文件。 |
| `AnalyzeSync` | `void` | 同步执行完整分析流程。 |
| `Dispose` | `void` | 释放检测器和查询器持有的数据。 |

### `BundleReferenceViewerWindow`

| 名称 | 类型 | 说明 |
|---|---|---|
| `GenerateGraph` | `private void` | 重新生成查询数据、检测 Group 并刷新列表。 |
| `ClearGraph` | `private void` | 清空查询数据、Group 列表和当前图谱。 |
| `Relayout` | `private void` | 重新布局当前图谱。 |
| `SelectBundle` | `private void` | 显示选中 Bundle 的局部引用图。 |
| `isSimplifyMode` | `bool` | 是否仅显示 Bundle 关系而不显示资源节点关系。 |
| `_referenceDepthField` | `IntegerField` | 控制 Bundle 局部关系图的最大层数。 |

### `BundleReferenceGraphView`

| 名称 | 类型 | 说明 |
|---|---|---|
| `ShowBundle` | `public void` | 显示指定 Bundle 两侧限定层数内的关系。 |
| `ShowGroup` | `public void` | 显示指定 Group 内的全部 Bundle。 |
| `ClearGraph` | `public void` | 删除当前图中的节点、边和缺陷提示按钮。 |
| `Relayout` | `public void` | 重新布局当前 Bundle 图。 |
| `HighlightDownstream` | `public void` | 从资源输出端口开始递归高亮下游资源节点。 |

## 工作原理

### 1. 创建查询数据

执行 `GenerateGraph` 或 `BundleReferenceAnalyzer.InitAsync` 时，会创建 `BundleReferenceQueryer`。

`BundleReferenceQueryer` 负责保存：

- Bundle 到 `BundleReferenceData` 的映射；
- 资源到 `AssetReferenceData` 的映射；
- Bundle Group 映射；
- Bundle 和资源之间的关系。

### 2. 采集资源引用

分析器通过 Unity Editor 的 `AssetDatabase` 获取 Bundle 中的资源：

```csharp
var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleInfo.bundleName);
```

随后通过 `AssetReferenceCollector.FindDirectReferences` 获取每个资源的直接依赖，并交给 Queryer：

```csharp
_queryer.SetAssets(bundleInfo.bundleName, assetData);
```

`AssetDatabase` 相关调用必须在 Unity 主线程执行。

### 3. 执行缺陷检测

资源采集完成后，`BundleDefectDetectorBox` 逐个执行已注册的检测器。

检测到缺陷时：

1. 将检测器的 `DefectLevel` 合并到 `BundleReferenceData.defectLevel`；
2. 将检测器的 `tag` 添加到 `BundleReferenceData.tags`；
3. 更新对应 Group 的 `defectInfos`；
4. 在 GraphView 中显示缺陷颜色和提示信息。

### 4. 构建可视化列表

`BundleReferenceViewerWindow` 使用外层 `ListView` 显示 Group，Group 内部再使用一个 `ListView` 显示 Bundle。

支持：

- Bundle 名称模糊搜索；
- Group 展开状态保存；
- 显示整个 Group；
- 点击 Bundle 显示局部关系图。

### 5. 构建局部 Bundle 图

点击 Bundle 后，`BundleReferenceGraphView.ShowBundle` 会从当前 Bundle 开始，分别沿以下两个方向遍历：

- `bundleDependent`：依赖方向；
- `bundleReferenced`：被引用方向。

工具栏中的“关系层数”分别限制两个方向的最大深度：

- `0`：只显示当前 Bundle；
- `1`：显示当前 Bundle 及直接邻居；
- `2`：显示当前 Bundle 及两层关系。

点击 Group 的“显示所有 Bundle”按钮时，不使用该层数限制，而是显示整个 Group。

### 6. 创建 GraphView 节点和边

非简化模式下：

- 创建 Bundle 节点；
- 创建 Bundle 内的资源节点；
- 根据 `AssetReferenceData.assetDependent` 创建资源依赖边。

简化模式下：

- 只创建 Bundle 节点；
- 根据 `bundleDependent` 创建 Bundle 关系边；
- 不创建资源关系边。

资源节点被点击时，会 Ping 对应资源，并从当前输出端口递归高亮下游资源节点。

## 使用示例

### 在 Unity Editor 中启动分析

在 Unity 菜单执行：

```text
Tools/UFlow/Analyze All Bundles
```

分析完成后，结果会保存到：

```text
Analysis/yyyyMMddHHmmss.bin
```

在 Unity 菜单执行：

```text
Tools/UFlow/Bundle Reference Text Viewer
```
选择之前生成的bin文件则能获得缺陷清单

### 打开可视化窗口

在 Unity 菜单执行：

```text
Tools/UFlow/Bundle Reference
```

然后按以下步骤操作：

1. 点击“生成分析”；
2. 在左侧 Group 列表中展开分组；
3. 设置“关系层数”；
4. 点击 Bundle 名称查看局部关系图；
5. 或点击“显示所有 Bundle”查看完整 Group；
6. 点击“简化模式”切换 Bundle 级关系显示。

### 通过代码显示 Bundle 图

```csharp
var graphView = new BundleReferenceGraphView();
var detectorBox = new BundleDefectDetectorBox();

var queryer = QueryerFactory.GenerateQueryerByCurrentProjectSync();
var bundleName = "example.bundle";

graphView.ShowBundle(
    queryer,
    bundleName,
    detectorBox,
    isSimplifyMode: false,
    referenceDepth: 2);
```

使用完成后应释放分析数据和检测器：

```csharp
try
{
    // 使用 queryer 和 graphView
}
finally
{
    detectorBox.Dispose();
    queryer.Dispose();
}
```

## 常见使用场景

- 检查 Bundle 是否存在过深的依赖链；
- 定位循环 Bundle 引用；
- 查找 Bundle 拆分过细导致的单引用单资源问题；
- 检查外部 Bundle 对内部资源的分散引用；
- 从某个资源向下游追踪资源引用；
- 在大型 Group 中只查看指定 Bundle 附近的局部关系；
- 使用简化模式快速查看 Bundle 级依赖结构。

## 边界情况与注意事项

- `AssetDatabase` 和 Unity Editor API 必须在 Unity 主线程调用。
- `referenceDepth` 会被限制为不小于 `0`；负数会按 `0` 处理。
- 点击 Group 时会显示整个 Group，不受“关系层数”限制。
- Bundle 数据、资源数据和关系集合在可视化完成前不能释放，否则 GraphView 无法继续查询或展示引用关系。
- `BundleReferenceQueryer.Dispose()` 会释放对象池中的关系集合，释放后不能继续使用该 Queryer。
- 资源数据同时可能被 Bundle 列表和 Queryer 的资源字典引用，释放时应由统一所有者负责，避免重复归还对象池。
- 大型 Group 可能创建大量 GraphView 节点和边，建议优先使用 Bundle 局部查看或简化模式。
- 关闭窗口或清空图谱时，会清理当前 GraphView 元素和左侧列表数据源。
- 图布局需要处理循环或异常关系；对于循环关系，布局逻辑会选择稳定的起始 Bundle 继续分配层级。
- 当前分析结果文件的文本查看器使用虚拟化 `ListView`，适合显示较长文本，但读取文件时仍会先构建文本行列表。

## 相关文件

- `BundleReferenceAnalyzer/BundleReferenceAnalyzer.cs`：分析入口、资源采集和缺陷检测流程。
- `BundleReferenceAnalyzer/AssetReferenceCollector.cs`：采集资源直接依赖。
- `DataBase/BundleReferenceQueryer.cs`：维护 Bundle、资源和 Group 关系。
- `DataBase/BundleReferenceData.cs`：保存单个 Bundle 的关系和缺陷信息。
- `DataBase/AssetReferenceData.cs`：保存单个资源的关系。
- `DataBase/BundleReferenceGroup.cs`：保存 Bundle 分组和 Group 缺陷聚合信息。
- `BundleDefectDetector/BundleDefectDetectorBox.cs`：管理并执行 Bundle 缺陷检测器。
- `BundleReferenceGraphView.cs`：创建 GraphView 节点、边、布局和高亮效果。
- `BundleReferenceBundleNode.cs`：实现 Bundle 节点和资源节点。
- `BundleReferenceViewerWindow.cs`：实现可视化窗口、工具栏和左侧 Bundle 列表。
- `BundleReferenceTextViewerWindow.cs`：以文本形式读取并显示分析文件。
