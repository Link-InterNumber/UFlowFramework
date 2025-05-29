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
        private bool _standaloneCanvas = false;
        public bool standaloneCanvas => _standaloneCanvas;

        /// <summary>
        /// UI预制体路径和是否忽略射线检测
        /// </summary>
        /// <param name="path">预制体路径</param>
        /// <param name="ignoreRaycast">是否忽略射线检测</param>
        /// <param name="standaloneCanvas">是否使用独立Canvas</param>
        public WindowInfo(string path, bool ignoreRaycast = false, bool standaloneCanvas = false)
        {
            _path = path;
            _ignoreRaycast = ignoreRaycast;
            _standaloneCanvas = standaloneCanvas;
        }
    }
}