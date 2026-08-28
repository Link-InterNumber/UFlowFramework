# Bundle 构建对比工具

## 概述

Bundle 构建对比工具用于比较已构建 AssetBundle、当前 Unity 项目中的 Bundle 配置，以及可选的历史基准文件。

工具可以帮助定位以下变化：

- 已构建 Bundle 在当前项目中新增或移除；
- Bundle 内资源发生新增、移除或变化；
- Bundle 依赖关系发生变化；
- Bundle 文件大小和包含依赖后的加载成本发生变化；
- 当前构建相对于历史基准的资源、依赖和大小变化。

该功能以 Unity EditorWindow 的形式提供，不参与运行时构建和运行时加载逻辑。

## 位置

- 主窗口：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/BundleReferenceCompareWindow.cs`
- 对比服务：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/BundleReferenceComparisonService.cs`
- 对比工具：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/BundleReferenceCompareUtility.cs`
- 展示格式化：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/BundleReferenceCompareFormatter.cs`
- 界面配置：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/BundleReferenceCompareSettings.cs`
- 基准数据：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/Serialization/BundleBuildBaselineInfo.cs`
- 基准读写：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/Serialization/BundleBuildBaselineUtility.cs`
- Manifest 适配：`Assets/UFlowFramework/Editor/AssetsSystem/BuildedBundleCompare/Database/IBundleReferenceManifest.cs`

## 核心概念

### 1. 已构建 Bundle

已构建 Bundle 通过构建目录中的 `AssetBundleManifest` 进行索引。`BundleReferenceManifest.PrepareManifest(...)` 加载指定 Manifest，并通过 `IBundleReferenceManifest` 提供以下信息：

- 所有 Bundle 名称；
- Bundle 的直接依赖和全部依赖；
- Bundle 文件路径。

实际 Bundle 文件由 `AssetBundle.LoadFromFile(...)` 打开，并通过 `GetAllAssetNames()` 和 `GetAllScenePaths()` 获取内容。

### 2. 当前 Bundle 配置

当前项目的 Bundle 名称通过 `AssetDatabase.GetAllAssetBundleNames()` 获取，Bundle 内资源通过 `AssetDatabase.GetAssetPathsFromAssetBundle(...)` 获取。

该数据来源于当前工程中的 AssetImporter 标记，不是已构建目录中的 Bundle 文件内容。因此，当前配置资源和构建产物资源可能存在路径格式或扩展名差异，比较时由 `BundleReferenceCompareUtility.IsAssetMatch(...)` 负责匹配。

### 3. 历史基准

历史基准文件扩展名为 `.bundlebaseline`，保存每个 Bundle 的：

- Bundle 名称；
- 文件大小；
- 已构建资源列表；
- 直接及间接依赖 Bundle 列表。

基准文件是可选的。没有加载基准文件时，窗口只比较已构建内容和当前 Editor 配置；加载基准后，详情面板会额外显示相对于基准的变化。

### 4. 对比状态

`BundleCompareStatus` 表示 Bundle 当前状态：

| 状态 | 含义 |
|---|---|
| `Unanalyzed` | 已确认 Bundle 同时存在于构建结果和当前配置，但详细资源信息尚未加载 |
| `Same` | 两侧资源集合一致 |
| `Added` | 当前配置存在，但构建结果中不存在 |
| `Removed` | 构建结果存在，但当前配置中不存在 |
| `Changed` | Bundle 两侧均存在，但资源集合不同 |

## 工作原理

### 1. 打开窗口

在 Unity 编辑器菜单中选择：

`Tools/UFlow/Bundle Reference Compare`

窗口会恢复上次保存的构建目录、Manifest 名称和基准文件路径。路径通过带项目哈希的 `EditorPrefs` 键保存，避免不同项目之间互相覆盖历史配置。

### 2. 准备 Manifest

点击“开始对比”后，窗口执行以下检查：

1. 检查构建目录和 Manifest 名称是否填写；
2. 调用 `BundleReferenceManifest.PrepareManifest(...)`；
3. 验证目录和 Manifest 文件是否存在；
4. 从 Manifest Bundle 中加载 `AssetBundleManifest`；
5. 创建 `UnityBundleReferenceManifest`，作为后续查询入口；
6. 尝试加载可选的 `.bundlebaseline` 文件。

如果基准文件读取失败，工具会记录警告并跳过基准比较，不会阻止普通的构建结果比较。

### 3. 创建初始 Bundle 列表

`BundleReferenceComparisonService.Compare(...)` 会合并三组名称：

- Manifest 中的已构建 Bundle；
- `AssetDatabase.GetAllAssetBundleNames()` 返回的当前 Bundle；
- 基准文件中的 Bundle。

每个名称创建一个 `BundleCompareItem`，列表默认按已构建文件大小从大到小排序。初次比较只读取文件存在性和大小，详细资源分析采用延迟执行。

### 4. 延迟分析选中的 Bundle

当用户选中 Bundle，或 Bundle 行进入可视区域时，窗口调用 `Analyze(...)`：

1. 使用 `ReadBuiltAssets(...)` 读取构建 Bundle 中的资源和类型；
2. 使用 `CollectDependencyData(...)` 递归收集所有依赖；
3. 将当前项目中同名 Bundle 的资源读入 `currentAssets`；
4. 计算 Bundle 状态；
5. 计算新增资源、移除资源和合并后的资源列表；
6. 计算加载成本：自身文件大小加全部依赖 Bundle 的文件大小；
7. 如果存在基准，则计算相对于基准的状态和差异。

构建 Bundle 数据保存在 `_builtData` 缓存中，避免同一个构建文件被重复读取。

### 5. 展示结果

左侧列表展示：

- Bundle 大小；
- 当前配置状态；
- 基准状态；
- Bundle 大小占所有已构建 Bundle 的比例。

右侧详情展示：

- 当前状态和基准状态；
- Bundle 文件大小；
- 包含全部依赖的加载成本；
- 依赖 Bundle 列表；
- 构建资源、当前配置资源、新增资源和移除资源数量；
- 构建资源类型统计；
- 相对基准的大小、资源和依赖变化；
- 构建资源与当前配置资源的对应列表。

依赖 Bundle 使用只读 `TextField` 展示，资源名称可以点击并通过 `BundleReferenceUtils.PingAsset(...)` 在 Project 窗口中定位。

## 使用示例

### 编辑器中使用

1. 先完成 AssetBundle 构建，并保留构建目录中的 Manifest 文件和各个 Bundle 文件。
2. 打开菜单 `Tools/UFlow/Bundle Reference Compare`。
3. 在“已构建目录”中选择构建输出目录。
4. 在“Manifest 名称”中填写对应的 Manifest 文件名。
5. 可选：加载已有 `.bundlebaseline` 文件。
6. 点击“开始对比”。
7. 点击左侧 Bundle，查看资源、依赖、加载成本和差异详情。

### 保存当前构建为基准

在填写构建目录和 Manifest 名称后，点击“保存为基准”。工具会读取 Manifest 中的全部 Bundle，解析资源和依赖，并将结果保存为 `.bundlebaseline` 文件。

基准文件可以在下一次构建后通过“添加基准”加载，用于比较跨构建变化。

### 代码调用示例

以下示例展示了该模块内部的典型调用顺序。示例应在 Unity Editor 环境中执行；相关类型为当前模块的 `internal` 类型，适合放在同一程序集的编辑器代码中。

```csharp
using System.Collections.Generic;
using UnityEditor;

namespace PowerCellStudio.Editor
{
    internal static class BundleCompareExample
    {
        internal static List<BundleCompareItem> CompareCurrentBuild(
            string buildDirectory,
            string manifestName,
            string baselinePath = null)
        {
            BundleReferenceManifest.PrepareManifest(buildDirectory, manifestName);
            if (BundleReferenceManifest.manifest == null)
                return new List<BundleCompareItem>();

            List<BundleBuildBaselineInfo> baseline = null;
            if (!string.IsNullOrEmpty(baselinePath))
                baseline = BundleBuildBaselineUtility.Load(baselinePath);

            var currentBundleNames = new HashSet<string>(
                AssetDatabase.GetAllAssetBundleNames(),
                System.StringComparer.OrdinalIgnoreCase);

            return BundleReferenceComparisonService.Compare(
                baseline,
                currentBundleNames);
        }
    }
}
```

使用完毕后，应调用 `BundleReferenceManifest.ClearManifest()` 释放 Manifest 和已加载的 AssetBundle 资源。窗口自身会在 `OnDisable()` 中执行清理。

## 常见使用场景

- 检查新构建是否意外移除了 Bundle 或资源；
- 定位当前 AssetBundle 标记与实际构建内容不一致的问题；
- 分析某个 Bundle 的全部依赖和实际加载成本；
- 比较两个构建之间的 Bundle 资源变化；
- 在资源或依赖发生变化时快速定位受影响的 Bundle；
- 保存稳定构建作为基准，持续监控后续构建的大小和内容变化。

## 边界情况与注意事项

- 构建目录必须包含指定的 Manifest 文件，且 Manifest 中必须能加载 `AssetBundleManifest`。
- Manifest 名称应填写实际文件名，不需要额外添加 `.bundle` 扩展名，具体取决于构建输出使用的命名方式。
- `GetCurrentAssets(...)` 查询的是当前项目的 AssetBundle 标记。如果资源使用 Addressables 或没有设置传统 AssetBundle 名称，该查询可能返回空集合。
- 构建 Bundle 中的资源名称和当前项目资源路径可能存在扩展名差异。比较工具会先比较完整规范化路径，在一侧缺少扩展名时再比较文件名主体。
- `AssetBundle.LoadFromFile(...)` 或资源读取失败时，工具仍会保留可读取的文件元数据，但资源列表和类型统计可能为空。
- 详细资源分析是延迟执行的。初始 Bundle 列表不会立即读取每个 Bundle 的全部内容，选中项目或进入可视区域时才会分析。
- 加载成本是自身 Bundle 大小与全部依赖 Bundle 大小之和，不等同于运行时精确内存占用。
- 当前版本基准文件格式为 `CurrentVersion = 1`。不支持的版本会抛出异常，并提示重新生成基准文件。
- 对比完成或窗口关闭后应释放 Manifest。`BundleReferenceManifest.ClearManifest()` 会卸载 Manifest，并卸载当前已加载的 AssetBundle。
- `BundleReferenceManifest` 使用静态状态保存当前 Manifest。同一时间应避免多个窗口或流程同时切换不同的构建目录。
- 当前窗口的资源列表显示资源文件名主体，而不是完整路径；点击资源后才通过 Unity 工具定位实际资源。
