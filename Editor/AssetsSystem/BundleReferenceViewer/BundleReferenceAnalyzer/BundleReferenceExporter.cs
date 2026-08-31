using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceExporter : IDisposable
    {
        public static void BundleReferenceExporterHandler()
        {
            using var analyzer = new BundleReferenceExporter();
            analyzer.AnalyzeSync(true, true);
        }

        private static async Task RunAnalysisAsync()
        {
            using var analyzer = new BundleReferenceExporter();
            try
            {
                await analyzer.InitAsync();
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

        public BundleReferenceExporter()
        {
            _defectDetectorBox = new BundleDefectDetectorBox();
        }
        
        public void Dispose()
        {
            _defectDetectorBox?.Dispose();
            _queryer?.Dispose();
        }

        public void AnalyzeSync(bool writeFile, bool showWindow)
        {
            if (_queryer != null) _queryer.Dispose();
            Debug.Log("正在分析所有Bundle，请稍等...");
            _queryer = QueryerFactory.GenerateQueryerByCurrentProjectSync();
            BundleReferenceAnalyzer.DetectorGroupDefect(_queryer, _defectDetectorBox);
            if (writeFile)
            {
                var filePath = $"{QueryerFactory.serializedAssetDirectory}{DateTime.Now:yyyyMMddHHmmss}.bin";
                WriteReport(filePath);
                EditorUtility.DisplayDialog("分析完成", $"分析完成，数据已保存到 {filePath}", "OK");
                if (showWindow)
                    BundleReferenceTextViewerWindow.ShowWindow(filePath);
            }
        }
        
        private async Task InitAsync()
        {
            if (_queryer != null) _queryer.Dispose();
            Debug.Log("正在分析所有Bundle，请稍等...");
            _queryer = await QueryerFactory.GenerateQueryerByCurrentProject(512);
        }

        private async Task AnalyzeAsync()
        {
            if (_queryer == null)
                return;

            var allGroup = _queryer.GetAllGroups();
            var totalBundleCount = allGroup.Count;
            var processedBundleCount = 0;

            Debug.Log("分析完成，正在采集资源并检测缺陷...");
            var assetData = new List<AssetReferenceData>();
            foreach (var bundleReferenceGroup in allGroup.Values)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Bundle 分析",
                        $"采集资源",
                        totalBundleCount == 0 ? 1f : (float)processedBundleCount / totalBundleCount))
                {
                    throw new OperationCanceledException();
                }
                BundleReferenceAnalyzer.CollectGroupAssetData(_queryer, bundleReferenceGroup, ref assetData);
                BundleReferenceAnalyzer.DetectorBundlesDefect(_queryer, _defectDetectorBox, bundleReferenceGroup);
                processedBundleCount++;
                await Task.Yield();
            }
            
            var filePath = $"{QueryerFactory.serializedAssetDirectory}{DateTime.Now:yyyyMMddHHmmss}.bin";
            WriteReport(filePath);
            EditorUtility.DisplayDialog("分析完成", $"分析完成，数据已保存到 {filePath}", "OK");
        }

        private void WriteReport(string filePath)
        {
            var report = new BundleReferenceReport
            {
                dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                bundleCount = _queryer?.bundleCount ?? 0
            };

            if (_queryer != null)
            {
                foreach (var pair in _queryer.GetAllBundleData())
                {
                    var data = pair.Value;
                    if (data == null || data.defectLevel == DefectLevel.None)
                        continue;

                    report.bundleDefectReports.Add(new BundleDefectReport
                    {
                        bundleName = data.bundleName,
                        defectLevel = data.defectLevel,
                        tag = string.Join("、", data.tags ?? new List<string>()),
                        defectDetail = string.Join("\n", data.defectDetail ?? new List<string>())
                    });
                }
            }

            using var writer = new ReferenceWriter();
            writer.Write(report);
            writer.Flush(filePath);
        }
    }
}