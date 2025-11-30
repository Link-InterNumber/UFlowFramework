using System.IO;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class BasePlayerDataProcessor : IPlayerDataProcessor
    {
        
        /// <summary>
        /// 保存文件的根路径。
        /// The root path for saving files.
        /// </summary>
        /// <remarks>
        /// 此属性使用 Unity 的 <see cref="Application.persistentDataPath"/> 作为保存数据的根目录。
        /// This property uses Unity's <see cref="Application.persistentDataPath"/> as the root directory for saving data.
        /// </remarks>
        public static readonly string SavePathRoot = $"{Application.persistentDataPath}";

        public abstract string directoryName { get; }

        public abstract string extension { get; }

        /// <summary>
        /// 尝试获取保存文件的完整路径。
        /// Tries to get the full path of the save file.
        /// </summary>
        /// <param name="saveKey">保存数据的键。The key for the saved data.</param>
        /// <param name="savePath">输出参数，保存文件的完整路径。Output parameter for the full path of the save file.</param>
        /// <returns>如果成功获取路径，则返回 true；否则返回 false。Returns true if the path is successfully obtained; otherwise, false.</returns>
        public bool TryGetSaveFilePath(string saveKey, out string savePath)
        {
            if (string.IsNullOrEmpty(saveKey))
            {
                savePath = string.Empty;
                return false;
            }
            savePath = Path.Combine(SavePathRoot, directoryName, $"{saveKey}.{extension}");
            return true;
        }

        /// <summary>
        /// 检查并创建保存数据的目录。
        /// Checks and creates the directory for saving data.
        /// </summary>
        protected void CheckDirectory()
        {
            var directory = Path.Combine(SavePathRoot, directoryName);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }

        public virtual bool HasSave(string saveKey)
        {
            var directory = Path.Combine(SavePathRoot, directoryName);
            if (!Directory.Exists(directory)) return false;
            if (!TryGetSaveFilePath(saveKey, out var path)) return false;
            return File.Exists(path);
        }

        public virtual void Clear(string saveKey)
        {
            if (!TryGetSaveFilePath(saveKey, out var path)) return;
            if (!File.Exists(path)) return;
            File.Delete(path);
        }

        public virtual void ClearAll()
        {
            var path = Path.Combine(SavePathRoot, directoryName);
            if (!Directory.Exists(path)) return;
            DirectoryInfo di = new DirectoryInfo(path);
            di.Delete(true);
        }
    }
}