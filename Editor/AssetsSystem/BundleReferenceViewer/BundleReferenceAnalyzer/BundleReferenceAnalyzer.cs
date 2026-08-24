using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceAnalyzer : IDisposable
    {
        [MenuItem("Test/Analyze All Bundles", priority = 100)]
        public static void ReferenceAnalyzerEditorHandler()
        {
            _ = RunAnalysisAsync();
        }

        private static async Task RunAnalysisAsync()
        {
            using var analyzer = new BundleReferenceAnalyzer();
            try
            {
                await analyzer.Init();
                await analyzer.AnalyzeAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Bundle 分析已取消。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Bundle 分析失败", exception.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        private BundleDefectDetectorBox _defectDetectorBox;
        
        private BundleReferenceQueryer _queryer;

        public BundleReferenceAnalyzer()
        {
            _defectDetectorBox = new BundleDefectDetectorBox();
        }
        
        public void Dispose()
        {
            _defectDetectorBox?.Dispose();
            _queryer?.Dispose();
        }
        
        public async Task Init()
        {
            if (_queryer != null) _queryer.Dispose();
            Debug.Log("正在分析所有Bundle，请稍等...");
            _queryer = await QueryerFactory.GenerateQueryerByCurrentProject();
        }

        public async Task AnalyzeAsync()
        {
            if (_queryer == null)
                return;

            var allGroup = _queryer.GetAllGroups();
            var totalBundleCount = _queryer.bundleCount;
            var processedBundleCount = 0;

            Debug.Log("分析完成，正在采集资源并检测缺陷...");
            foreach (var bundleReferenceGroup in allGroup.Values)
            {
                foreach (var bundleName in bundleReferenceGroup.bundleNames)
                {
                    var bundleInfo = _queryer.GetBundleData(bundleName);
                    var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleInfo.bundleName);
                    var assetData = new List<AssetReferenceData>();
                    for (var i = 0; i < assets.Length; i++)
                    {
                        var d = AssetReferenceCollector.FindDirectReferences(bundleInfo.bundleName, assets[i]);
                        if (d == null) continue;
                        assetData.Add(d);
                    }
                    _queryer.SetAssets(bundleInfo.bundleName, assetData);

                    processedBundleCount++;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Bundle 分析",
                            $"采集资源: {bundleInfo.bundleName}",
                            totalBundleCount == 0 ? 1f : (float)processedBundleCount / totalBundleCount))
                    {
                        throw new OperationCanceledException();
                    }

                    // AssetDatabase、Graph/Editor API 必须留在 Unity 主线程；
                    // 分帧而不是 Task.Run，避免阻塞 Editor，同时保持线程安全。
                    await Task.Yield();
                }

                var bundleNames = bundleReferenceGroup.bundleNames.ToArray();
                var detectionTasks = new Task<List<GroupDefectInfo>>[bundleNames.Length];
                for (var i = 0; i < bundleNames.Length; i++)
                {
                    var bundleInfo = _queryer.GetBundleData(bundleNames[i]);
                    detectionTasks[i] = Task.Run(() =>
                    {
                        using var detectorBox = new BundleDefectDetectorBox();
                        return detectorBox.EvaluateBundle(bundleInfo, _queryer);
                    });
                }

                var detectionResults = await Task.WhenAll(detectionTasks);
                for (var i = 0; i < bundleNames.Length; i++)
                {
                    var bundleInfo = _queryer.GetBundleData(bundleNames[i]);
                    ApplyDetectionResults(bundleInfo, detectionResults[i], _queryer);
                    processedBundleCount++;

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Bundle 分析",
                            $"检测缺陷: {bundleInfo.bundleName}",
                            totalBundleCount == 0 ? 1f : (float)processedBundleCount / totalBundleCount))
                    {
                        throw new OperationCanceledException();
                    }

                    await Task.Yield();
                }

                // foreach (var bundleName in bundleReferenceGroup.bundleNames)
                // {
                //     var bundleInfo = _queryer.GetBundleData(bundleName);
                //     foreach (var assetReferenceData in bundleInfo.assets)
                //     {
                //         assetReferenceData.Inactivate();
                //     }
                // }
            }
            
            var filePath = $"{QueryerFactory.serializedAssetDirectory}/{DateTime.Now:yyyyMMddHHmmss}.bin";
            QueryerFactory.SaveSerializedDataFromQueryer(_queryer, filePath);
            EditorUtility.DisplayDialog("分析完成", $"分析完成，数据已保存到 {filePath}", "OK");
        }

        private static void ApplyDetectionResults(
            BundleReferenceData bundleInfo,
            List<GroupDefectInfo> results,
            BundleReferenceQueryer queryer)
        {
            if (bundleInfo == null)
                return;

            bundleInfo.defectLevel = DefectLevel.None;
            if (bundleInfo.tags == null)
                bundleInfo.tags = new List<string>();
            else
                bundleInfo.tags.Clear();

            var group = queryer?.GetGroupByBundle(bundleInfo.bundleName);
            if (results == null)
                return;

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                bundleInfo.defectLevel |= result.level;
                bundleInfo.tags.Add(result.tag);

                if (group == null || group.defectInfos == null)
                    continue;

                if (group.defectInfos.TryGetValue(result.tag, out var info))
                {
                    info.count += result.count;
                    if (info.bundleNames == null)
                        info.bundleNames = new List<string>();
                    info.bundleNames.AddRange(result.bundleNames);
                    group.defectInfos[result.tag] = info;
                }
                else
                {
                    group.defectInfos[result.tag] = result;
                }
            }
        }
    }
}