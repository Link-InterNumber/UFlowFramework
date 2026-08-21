using System;
using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    public class BundleDefectDetectorBox : IDisposable
    {
        private List<IBundleDefectDetector> detectors;

        public BundleDefectDetectorBox()
        {
            detectors = new List<IBundleDefectDetector>();
            // 按缺陷等级从低到高注册，以符合当前检测流程的等级短路规则。
            detectors.Add(new SingleReferenceSingleAssetDefectDetector());
            detectors.Add(new ReferencesScatteredDefectDetector());
            // detectors.Add(new CircularBundleReferenceDefectDetector());
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
        
        public void DetectGroup(IEnumerable<BundleReferenceGroup> groups, BundleReferenceQueryer queryer)
        {
            if (groups == null || queryer == null || detectors == null)
                return;

            foreach (var group in groups)
            {
                if (group == null)
                    continue;
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
        }

        public void DetectBundle(BundleReferenceData data, BundleReferenceQueryer queryer)
        {
            if (data == null || queryer == null || detectors == null)
                return;

            data.tags ??= new List<string>();
            for (var i = 0; i < detectors.Count; i++)
            {
                var detector = detectors[i];
                if (!detector.Detect(queryer, data)) continue;
                data.defectLevel |= detector.defectLevel;
                data.tags.Add(detector.tag);
            }
        }
        
    }
}