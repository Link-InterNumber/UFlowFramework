using UnityEngine;

namespace PowerCellStudio
{
    public class ABGameObjectSelfCleanup : MonoBehaviour
    {
        private string _address;
        private AssetsBundleManager _manager;
        
        public void Set(AssetsBundleManager manager, string address)
        {
            _address = address;
            _manager = manager;
        }
        
        private void OnDestroy()
        {
            if (_manager == null || string.IsNullOrEmpty(_address)) return;
            _manager.DelAssetRef(_address);
        }
    }
}