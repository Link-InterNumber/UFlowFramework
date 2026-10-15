using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [DisallowMultipleComponent]
    public class DefaultSelected : MonoBehaviour
    {
        public bool activeOnEnable = false;

        private Selectable _target;
        public Selectable target => _target;

        protected virtual void Awake()
        {
            if (!_target) _target = GetComponent<Selectable>();
        }

        private void OnDestroy()
        {
            _target = null;
        }

        private void OnEnable()
        {
            if (!activeOnEnable) return;
            target?.Select();
        }

        protected void Start()
        {
            if (activeOnEnable) return;
            target?.Select();
        }
    }
}