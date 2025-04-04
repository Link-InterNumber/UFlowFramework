// using DG.Tweening;

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PowerCellStudio
{
    public class UICamera: MonoSingleton<UICamera>
    {
        private Camera _cameraCom;
        private Volume _volume;
        
        public Camera cameraCom
        {
            get
            {
                if (!_cameraCom)
                    _cameraCom = transform.GetComponent<Camera>();
                return _cameraCom;
            }
        }

        private Vector2Int _currentScreen;
        protected override void Awake()
        {
            base.Awake();
            _cameraCom = transform.GetComponent<Camera>();
            _volume = transform.GetComponent<Volume>();
            var screenHeight = Screen.height;
            _cameraCom.orthographicSize = screenHeight / 200f;
            _currentScreen = new Vector2Int(Screen.width, Screen.height);
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_currentScreen.x == Screen.width && _currentScreen.y == Screen.height) return;
            _currentScreen = new Vector2Int(Screen.width, Screen.height);
            var screenHeight = Screen.height;
            _cameraCom.orthographicSize = screenHeight / 200f;
            EventManager.instance.onChangeScreen?.Invoke(_currentScreen);
        }
    }
}