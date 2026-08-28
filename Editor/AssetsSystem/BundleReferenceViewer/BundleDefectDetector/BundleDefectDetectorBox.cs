using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio.Editor
{
    public class BundleDefectDetectorBox : IDisposable
    {
        private List<IBundleDefectDetector> detectors;

        public BundleDefectDetectorBox()
        {
            detectors = new List<IBundleDefectDetector>
            {
                new SingleReferenceSingleAssetDefectDetector(), // Low
                // new OrphanBundleDefectDetector(),               // Low
                new ReferencesScatteredDefectDetector(),        // Medium
                new DeepDependencyDefectDetector(),             // Medium
                // new HighReferenceCountDefectDetector(),         // Medium
                new CircularBundleReferenceDefectDetector()     // High
            };
        }

        public void Dispose()
        {
            if (detectors == null)
                return;

            for (var i = 0; i < detectors.Count; i++)
            {
                if (detectors[i] is IDisposable disposable)
                    disposable.Dispose();
            }
            detectors.Clear();
            detectors = null;
        }
        
        public void DetectGroups(IEnumerable<BundleReferenceGroup> groups, BundleReferenceQueryer queryer)
        {
            if (groups == null || queryer == null || detectors == null)
                return;

            foreach (var group in groups)
            {
                DetectGroups(group, queryer);
            }
        }

        public void DetectGroups(BundleReferenceGroup group, BundleReferenceQueryer queryer)
        {
            if (group == null)
                return;
            for (var i = 0; i < detectors.Count; i++)
            {
                var detector = detectors[i];
                if (group.defectLevel >= detector.defectLevel) continue;
                if (!detector.HasDefect(queryer, group)) continue;
                group.defectLevel |= detector.defectLevel;
                if (group.defectLevel == DefectLevel.High)
                    break;
            }
        }

        public void DetectBundleAndMarkGroup(BundleReferenceData data, BundleReferenceQueryer queryer)
        {
            if (data == null || queryer == null || detectors == null)
                return;

            data.defectLevel = DefectLevel.None;
            data.tags.Clear();
            data.defectDetail.Clear();
            var group = queryer.GetGroupByBundle(data.bundleName);
            var defectInfos = group?.defectInfos;
            for (var i = 0; i < detectors.Count; i++)
            {
                var detector = detectors[i];
                if (!detector.Detect(queryer, data, out var defectDetail)) continue;
                data.defectLevel |= detector.defectLevel;
                data.tags.Add(detector.tag);
                data.defectDetail.Add(defectDetail);
                if (defectInfos == null) continue;
                group.defectLevel |= detector.defectLevel;
                if (defectInfos.TryGetValue(detector.tag, out var info))
                {
                    info.count++;
                    info.bundleNames.Add(data.bundleName);
                }
                else
                {
                    defectInfos[detector.tag] = new GroupDefectInfo
                    {
                        level = detector.defectLevel,
                        count = 1,
                        bundleNames = new List<string> { data.bundleName },
                        tag = detector.tag,
                        toolTips = detector.toolTips,
                    };
                }
            }
        }

        public void DetectBundleOnly(BundleReferenceData data, BundleReferenceQueryer queryer)
        {
            if (data == null || queryer == null || detectors == null)
                return;
        
            data.defectLevel = DefectLevel.None;
            data.tags.Clear();
            data.defectDetail.Clear();
            for (var i = 0; i < detectors.Count; i++)
            {
                var detector = detectors[i];
                if (!detector.Detect(queryer, data, out var defectDetail)) continue;
                data.defectLevel |= detector.defectLevel;
                data.tags.Add(detector.tag);
                data.defectDetail.Add(defectDetail);
            }
        }
        
        // public List<GroupDefectInfo> EvaluateBundle(BundleReferenceData data, BundleReferenceQueryer queryer)
        // {
        //     var results = new List<GroupDefectInfo>();
        //     if (data == null || queryer == null || detectors == null)
        //         return results;
        //
        //     for (var i = 0; i < detectors.Count; i++)
        //     {
        //         var detector = detectors[i];
        //         if (!detector.Detect(queryer, data, out var defectDetail))
        //             continue;
        //
        //         results.Add(new GroupDefectInfo
        //         {
        //             level = detector.defectLevel,
        //             count = 1,
        //             bundleNames = new List<string> { data.bundleName },
        //             tag = detector.tag,
        //             toolTips = detector.toolTips
        //         });
        //     }
        //
        //     return results;
        // }
        
    }
}