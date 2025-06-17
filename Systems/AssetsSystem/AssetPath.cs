using UnityEngine
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [Serializable]
    public class AssetPath<T> where T : Object
    {
        public string path;
    }
}
