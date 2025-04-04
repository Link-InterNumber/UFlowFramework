using UnityEngine;

namespace PowerCellStudio
{
    public class ABGameObjectSelfCleanup : MonoBehaviour
    {
        private string _address;
        private IAssetLoader _loader;
        
        public void Set(IAssetLoader loader, string address)
        {
            _address = address;
            _loader = loader;
        }
        
        private void OnDestroy()
        {
            if (_loader == null) return;
            _loader.Release(_address);
        }
    }
}