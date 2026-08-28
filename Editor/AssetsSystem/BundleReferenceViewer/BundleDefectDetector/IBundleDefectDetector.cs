namespace PowerCellStudio.Editor
{
    public interface IBundleDefectDetector
    {
        public string title { get; }
        
        public string toolTips { get; }
        
        public string tag { get; }
        
        public DefectLevel defectLevel { get; }

        public bool Detect(BundleReferenceQueryer queryer, BundleReferenceData bundleData, out string defectDetail);

        public bool HasDefect(BundleReferenceQueryer queryer, BundleReferenceGroup group);
    }
}