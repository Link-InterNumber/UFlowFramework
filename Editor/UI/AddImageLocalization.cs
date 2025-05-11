#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [CustomEditor(typeof(Image), true)]
    [CanEditMultipleObjects]
    public class AddImageLocalization : ImageEditor
    {
        private bool _componentAdded = false;
        private Image _target => target as Image;

        private ImageLocalization _com;

        protected override void OnEnable()
        {
            base.OnEnable();
            _com = _target.GetComponent<ImageLocalization>();
            _componentAdded = _com;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _com = null;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!_componentAdded && GUILayout.Button("Add Up ImageLocalization"))
            {
                _componentAdded = _target.GetComponent<ImageLocalization>();
                if (_componentAdded) return;
                _com = _target.gameObject.AddComponent<ImageLocalization>();
                _componentAdded = true;
            }
        }
    }
}

#endif