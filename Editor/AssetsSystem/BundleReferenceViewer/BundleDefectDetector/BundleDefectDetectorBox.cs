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
                new HighReferenceCountDefectDetector(),         // Medium
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

        public void DetectBundle(BundleReferenceData data, BundleReferenceQueryer queryer)
        {
            if (data == null || queryer == null || detectors == null)
                return;

            if (data.tags == null)
            {
                data.tags = ListPool<string>.Get();
            }
            else
            {
                data.tags.Clear();
            }
            var group = queryer.GetGroupByBundle(data.bundleName);
            var defectInfos = group?.defectInfos;
            for (var i = 0; i < detectors.Count; i++)
            {
                var detector = detectors[i];
                if (!detector.Detect(queryer, data)) continue;
                data.defectLevel |= detector.defectLevel;
                data.tags.Add(detector.tag);
                if (defectInfos == null) continue;
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
        
    }
}