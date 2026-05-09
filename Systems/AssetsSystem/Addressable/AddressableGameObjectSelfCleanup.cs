using UnityEngine;

namespace PowerCellStudio
{
    public class AddressableGameObjectSelfCleanup: MonoBehaviour
    {
        private AddressableManager _manager;

        public void Init(AddressableManager manager)
        {
            _manager = manager;
        }

        private void OnDestroy()
        {
            _manager?.ReleaseGameObject(gameObject);
        }
    }
}