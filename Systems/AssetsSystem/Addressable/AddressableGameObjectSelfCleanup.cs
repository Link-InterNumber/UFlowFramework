using UnityEngine;

namespace PowerCellStudio
{
    public class AddressableGameObjectSelfCleanup: MonoBehaviour
    {
        private void OnDestroy()
        {
            AddressableManager.ReleaseGameObject(gameObject);
        }
    }
}