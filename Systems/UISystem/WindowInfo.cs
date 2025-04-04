using System;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WindowInfo: Attribute
    {
        private string _path;
        public string path => _path;
        private bool _ignoreRaycast = false;
        public bool ignoreRaycast => _ignoreRaycast;

        /// <summary>
        /// UI预制体路径和是否忽略射线检测
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="ignoreRaycast">是否忽略射线检测</param>
        public WindowInfo(string path, bool ignoreRaycast = false)
        {
            _path = path;
            _ignoreRaycast = ignoreRaycast;
        }
    }
}