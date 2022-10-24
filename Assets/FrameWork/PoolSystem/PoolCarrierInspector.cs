using UnityEngine;

namespace LinkFrameWork.PoolSystem
{
    public class PoolCarrierInspector : MonoBehaviour
    {
        private PoolCarrier _carrier;

        public void Init(PoolCarrier poolCarrier)
        {
            _carrier = poolCarrier;
        }

        private void OnDestroy()
        {
            _carrier?.ClearWithoutDestroy();
        }
    }
}