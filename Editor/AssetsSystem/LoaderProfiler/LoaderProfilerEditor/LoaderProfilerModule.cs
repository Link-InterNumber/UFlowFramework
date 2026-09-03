using System;
using PowerCellStudio;
using Unity.Profiling.Editor;

namespace PowerCellStudio.Editor
{
    [Serializable]
    [ProfilerModuleMetadata("Loader Profiler")]
        // "Shows AssetBundle and asset loading information for the selected frame.")]
    internal sealed class LoaderProfilerModule : ProfilerModule
    {
        private static readonly ProfilerCounterDescriptor[] ChartCounters =
        {
            // This counter is intentionally a descriptor only. Detailed loader data is
            // supplied by LoadSampleCollector and rendered in the details view.
            new ProfilerCounterDescriptor(LoadSampleCollector.ActiveLoadsCounterName, Unity.Profiling.ProfilerCategory.Loading),
            new ProfilerCounterDescriptor(LoadSampleCollector.BeginLoadsCounterName, Unity.Profiling.ProfilerCategory.Loading),
            new ProfilerCounterDescriptor(LoadSampleCollector.CompletedLoadsCounterName, Unity.Profiling.ProfilerCategory.Loading),
            new ProfilerCounterDescriptor(LoadSampleCollector.BundleCountCounterName, Unity.Profiling.ProfilerCategory.Loading),
            new ProfilerCounterDescriptor(LoadSampleCollector.DependencyDepthCounterName, Unity.Profiling.ProfilerCategory.Loading)
        };

        public LoaderProfilerModule()
            : base(ChartCounters, ProfilerModuleChartType.Line)
        {
        }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new LoaderProfilerModuleViewController(ProfilerWindow);
        }
    }
}