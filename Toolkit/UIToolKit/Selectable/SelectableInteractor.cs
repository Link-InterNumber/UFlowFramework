using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [DisallowMultipleComponent]
    public abstract class SelectableInteractor : MonoBehaviour
    {
        private Selectable _target;
        public Selectable target => _target;
        protected virtual void Awake()
        {
            if (!_target) _target = GetComponent<Selectable>();
        }
    }
}