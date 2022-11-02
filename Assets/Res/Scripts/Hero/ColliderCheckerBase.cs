using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Res.Scripts.Hero
{
    public class ColliderCheckerBase : MonoBehaviour
    {
        public Collider2D colliderCom;
        public LayerMask maskLayer;
        public CheckerType updeteType;
        public UnityEvent<bool> onValueChange = new UnityEvent<bool>();

        private ContactFilter2D _filter;
        private bool _inited = false;
        private bool _isOnHitting = false;

        public bool IsOnHitting => _isOnHitting;

        protected virtual void SetFilter()
        {
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(maskLayer);
            _inited = true;
        }
        
        public virtual bool OnHitting()
        {
            if(!_inited) SetFilter();
            var list = ListPool<Collider2D>.New();
            var result = colliderCom.OverlapCollider(_filter, list) > 0;
            list.Free();
            if (_isOnHitting == result) return result;
            _isOnHitting = result;
            onValueChange?.Invoke(result);
            return result;
        }

        private void Update()
        {
            if(updeteType != CheckerType.Update) return;
            OnHitting();
        }

        private void FixedUpdate()
        {
            if(updeteType != CheckerType.FixUpdate) return;
            OnHitting();
        }
    }

    public enum CheckerType
    {
        Update,
        FixUpdate,
        Manual
    }
}