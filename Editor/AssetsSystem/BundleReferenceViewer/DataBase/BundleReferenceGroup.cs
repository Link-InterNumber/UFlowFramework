using System;
using System.Collections.Generic;

namespace PowerCellStudio.Editor
{
    public class BundleReferenceGroup : IDisposable
    {
        public string groupName;
        public HashSet<string> bundleNames;
        public DefectLevel defectLevel = DefectLevel.None;

        public void Dispose()
        {
            groupName = null;
            if (bundleNames != null) bundleNames.Clear();
            bundleNames = null;
        }
    }
}
