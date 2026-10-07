# BundleReferenceCompare

## 1. 系统概述

`BundleReferenceCompare` 是一个 Unity Editor 工具，用于比较已构建 AssetBundle 与当前工程中的 Bundle 配置，并可选地与历史构建基准进行比较。它不负责构建 Bundle，也不自动修改 Bundle 配置，主要用于构建结果检查、资源差异定位和体积回归分析。

主要特点：

- 同时检查已构建 Bundle、当前 Bundle 配置和历史基准。
- 递归收集依赖 Bundle，并计算包含全部依赖的加载成本。
- 初次比较只读取文件元数据，选中条目后再延迟读取资源详情。
- 使用 `.bundlebaseline` 文件保存跨构建比较所需的资源、大小和依赖数据。
- 通过 UI Toolkit 提供筛选、状态展示和资源详情查看。

### 主要组件 / 接口

- `BundleReferenceCompareWindow`：编辑器窗口入口，负责参数输入、比较操作和结果展示。
- `BundleReferenceManifest`：管理当前已加载的 Manifest 适配器及其清理生命周期。
- `IBundleReferenceManifest`：抽象 Bundle 名称、依赖关系、文件路径和释放操作。
- `UnityBundleReferenceManifest`：将 Unity `AssetBundleManifest` 适配为 `IBundleReferenceManifest`。
- `BundleReferenceComparisonService`：合并多种数据来源并计算 Bundle 比较状态。
- `BundleReferenceCompareUtility`：读取 Bundle 内容、查询当前配置、收集依赖并计算加载成本。
- `BundleBuildBaselineUtility` / `BundleBuildBaselineInfo`：创建、保存、读取和校验历史基准数据。

### 组件依赖关系

```text
BundleReferenceCompareWindow
| 
|──> BundleReferenceManifest
|       |
|       └──> IBundleReferenceManifest
|               |
|               └──> UnityBundleReferenceManifest
|                       |
|                       └──> Unity AssetBundleManifest
|
|──> BundleReferenceComparisonService
|       |
|       |──> BundleCompareItem / BundleCompareStatus
|       |
|       └──> BundleReferenceCompareUtility
|               |
|               |──> Unity AssetDatabase
|               |
|               └──> Unity AssetBundle
|
└──> BundleBuildBaselineUtility
    |
    |──> BundleBuildBaselineInfo
    |
    |──> ReferenceWriter / ReferenceReader
    |
    └──> .bundlebaseline 文件
```

其中，`BundleReferenceCompareWindow` 是用户操作入口；它通过 `BundleReferenceManifest` 提供的 `IBundleReferenceManifest` 访问构建结果，通过 `BundleReferenceComparisonService` 计算差异，并通过 `BundleBuildBaselineUtility` 管理历史基准。依赖树表示组件引用或调用关系，具体执行先后见下一节。

## 2. 工作原理与优化

### 比较流程

1. `BundleReferenceCompareWindow` 从 `EditorPrefs` 恢复上次使用的构建目录、Manifest 名称和基准文件路径。
2. 用户点击“开始对比”后，窗口调用 `BundleReferenceManifest.PrepareManifest`。
3. `PrepareManifest` 校验目录和 Manifest 文件，加载其中的 `AssetBundleManifest`，并创建 `UnityBundleReferenceManifest`。
4. `BundleReferenceComparisonService.Compare` 合并三类 Bundle 名称：Manifest 中的已构建 Bundle、`AssetDatabase` 中的当前 Bundle，以及基准文件中的 Bundle。
5. 系统先通过 `ReadBuiltMetadata` 检查文件存在性和大小，并创建 `BundleCompareItem`。
6. 用户选中某个条目后，窗口调用分析逻辑，读取 Bundle 资源名称、资源类型、当前配置资源和递归依赖。
7. 比较结果按状态、资源差异、依赖关系和基准变化显示在详情面板中。

### 核心数据和资源管理

- `BundleCompareItem` 保存单个 Bundle 的当前状态、已构建资源、当前配置资源、依赖、加载成本以及基准差异。
- `_builtData` 使用 Bundle 名称缓存 `BuiltBundleData`，避免同一次分析中反复打开公共依赖 Bundle。
- `GetCurrentAssets` 通过 `AssetDatabase.GetAssetPathsFromAssetBundle` 获取当前工程资源，并将路径分隔符统一为 `/`。
- `CollectDependencyData` 使用 `HashSet<string>` 递归去重依赖，再将自身和全部依赖的文件大小相加为 `loadCost`。
- `BundleBuildBaselineUtility` 将版本号、Bundle 名称、大小、资源名称和依赖名称写入 `.bundlebaseline` 文件；读取时会校验文件版本。
- 基准保存先写入临时文件，再替换目标文件，避免直接覆盖时因写入失败损坏已有基准。

### 优化策略及约束

初次比较只读取构建文件的存在性和大小，不立即加载所有 Bundle 内容；详细分析按条目触发，降低大量 Bundle 同时加载造成的编辑器开销。依赖集合去重和 `_builtData` 缓存则减少公共依赖的重复读取。

这些操作依赖 Unity Editor 主线程 API，并包含文件 IO 和 `AssetBundle.LoadFromFile`。因此它适合用户主动触发的编辑器分析，不适合放入高频刷新、每帧回调或后台线程任务中。

## 3. 使用方法

### 3.1 执行 Bundle 对比

使用统一菜单入口：

`Tools/UFlow/Bundle Analysis/Builted Bundle Reference Compare`

最小操作流程：

1. 在“已构建目录”中填写包含 Manifest 和 Bundle 文件的目录。
2. 在“Manifest 名称”中填写构建输出中的 Manifest 文件名。
3. 如需历史对比，在“基准文件”中选择 `.bundlebaseline` 文件。
4. 点击“开始对比”。
5. 在左侧选择 Bundle，查看右侧资源、依赖、类型、大小和加载成本详情。

例如，构建目录为 `Build/Windows`、Manifest 文件名为 `Windows` 时，应将目录和 `Windows` 分别填写到对应输入框中。Manifest 名称必须是构建输出文件名，而不是 Bundle 内部资源名称。

### 3.2 创建历史基准

填写构建目录和 Manifest 名称后点击“保存为基准”。工具会读取 Manifest 中的每个 Bundle，创建 `BundleBuildBaselineInfo`，然后保存为 `.bundlebaseline` 文件。后续比较时选择该文件，详情面板会显示相对基准的大小、资源和依赖变化。

### 3.3 生命周期结束

比较或保存基准完成后，工具会在清理路径中调用 `BundleReferenceManifest.ClearManifest`，释放 Manifest 和已加载的 AssetBundle。若外部代码扩展比较流程，必须保留相同的清理责任，不能在清理后继续使用 `BundleReferenceManifest.manifest`。

## 4. 扩展方法

系统提供的扩展点是 `IBundleReferenceManifest`。它允许比较逻辑依赖统一的清单接口，而不直接依赖某一种 Manifest 数据来源。实现需要提供 Bundle 列表、直接依赖、全部依赖、Bundle 文件路径以及资源释放逻辑。

当前没有独立的注册表或自动发现机制。`BundleReferenceManifest.PrepareManifest` 目前直接创建 `UnityBundleReferenceManifest`，所以自定义适配器必须接入该方法的构造分支，或由项目维护者增加等价的选择入口。仅新增一个接口实现不会自动被系统使用。

下面的示例展示自定义清单适配器的接入形态。`CustomManifestData` 是外部数据源，属于示意类型；实际项目需要用真实的构建清单替换它，并在 `PrepareManifest` 中增加选择逻辑。

```csharp
// 伪代码：CustomManifestData 代表项目自己的清单数据源。
internal sealed class CustomBundleReferenceManifest : IBundleReferenceManifest
{
    private readonly CustomManifestData _data;
    private readonly string _bundleDirectory;

    public CustomBundleReferenceManifest(CustomManifestData data, string bundleDirectory)
    {
        _data = data;
        _bundleDirectory = bundleDirectory;
    }

    public string[] GetAllAssetBundles() => _data.GetAllAssetBundles();

    public string[] GetDirectDependencies(string assetBundleName) =>
        _data.GetDirectDependencies(assetBundleName) ?? Array.Empty<string>();

    public string[] GetAllDependencies(string assetBundleName) =>
        _data.GetAllDependencies(assetBundleName) ?? Array.Empty<string>();

    public string GetBundlePath(string assetBundleName) =>
        Path.Combine(_bundleDirectory, assetBundleName);

    public void UnloadAsset()
    {
        _data.Dispose();
    }
}
```

接入时必须保持以下不变量：Bundle 名称应使用与现有实现一致的比较规则；依赖查询不能把缺失依赖静默伪造成有效 Bundle；`GetBundlePath` 必须指向可被 `BundleReferenceCompareUtility` 读取的实际构建文件；`UnloadAsset` 必须释放适配器持有的文件句柄、Unity 资源或外部清单对象。

## 5. 注意事项

- 构建目录必须存在，Manifest 文件必须能通过 `AssetBundle.LoadFromFile` 加载，并且其中必须包含名为 `AssetBundleManifest` 的资源。
- 当前工程 Bundle 名称来自 `AssetDatabase.GetAllAssetBundleNames`；执行比较前应确保 Bundle 配置已经刷新到 AssetDatabase。
- 列表中的“未分析”表示尚未读取该 Bundle 的详细内容，不表示构建结果本身错误。
- `AssetDatabase`、`AssetBundle` 和编辑器窗口操作必须在 Unity 主线程执行。
- `.bundlebaseline` 文件包含版本号。版本不匹配时，`Load` 会拒绝读取，需要重新生成基准文件。
- 基准文件保存目录必须可写；保存失败时应保留原文件，并根据异常信息处理临时文件或权限问题。
- Bundle 依赖通过递归方式收集。异常的循环依赖或无效依赖名称可能影响分析结果，清单适配器应保证依赖数据有效。
- 资源路径会统一使用 `/`；缺少扩展名的资源名称可能执行文件名匹配，而带不同扩展名的同名资源不会被视为相同资源。
- `BundleReferenceManifest.ClearManifest` 会卸载当前 Manifest 和已加载的 AssetBundle。释放后不能继续调用当前 Manifest 适配器。
- 自定义 `IBundleReferenceManifest` 当前不会自动注册，必须同时修改适配器选择逻辑，并验证资源读取和释放流程。

## 6. 推荐使用场景

- **发布前构建回归检查**：确认已构建 Bundle 与当前 Bundle 配置一致，发现新增、移除或资源变化。
- **版本体积审查**：通过 Bundle 大小和包含依赖的 `loadCost`，定位下载或加载成本明显增加的 Bundle。
- **跨构建差异追踪**：为关键版本保存 `.bundlebaseline`，持续比较资源、依赖和大小变化。
- **依赖链排查**：查看单个 Bundle 的递归依赖，辅助定位公共资源重复打包或依赖链过长问题。
- **资源归属检查**：通过资源列表和类型统计确认 Prefab、贴图、材质、场景等资源是否进入预期 Bundle。

不推荐将此工具用于运行时资源加载、实时性能监控或自动修复 Bundle 配置；这些需求应分别使用运行时资源系统、Unity Profiler 或专门的构建校验流程。
