using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public static class ButtonExtension
    {
        #region ButtonEnumeratorListenerHandler

        private class ButtonEnumeratorListenerHandler
        {
            public ButtonEnumeratorListenerHandler(Button button)
            {
                _button = button;
                _IEnumertors = new List<Func<IEnumerator>>();
                _YieldInstructions = new List<Func<YieldInstruction>>();
            }
            
            private Button _button;
            private List<Func<IEnumerator>> _IEnumertors;
            private List<Func<YieldInstruction>> _YieldInstructions;
            
            public int Count => _IEnumertors.Count + _YieldInstructions.Count;
            
            public void AddListener(Func<IEnumerator> ac)
            {
                if(_IEnumertors.Count == 0)
                    _button.onClick.AddListener(OnClick);
                _IEnumertors.Add(ac);
            }
            
            public void RemoveListener(Func<IEnumerator> ac)
            {
                _IEnumertors.Remove(ac);
                if(Count == 0)
                    _button.onClick.RemoveListener(OnClick);
            }
            
            public void AddListener(Func<YieldInstruction> ac)
            {
                if(_YieldInstructions.Count == 0)
                    _button.onClick.AddListener(OnClick);
                _YieldInstructions.Add(ac);
            }
            
            public void RemoveListener(Func<YieldInstruction> ac)
            {
                _YieldInstructions.Remove(ac);
                if(Count == 0)
                    _button.onClick.RemoveListener(OnClick);
            }
            
            private void OnClick()
            {
                ApplicationManager.instance.StartCoroutine(RunEnumeratorHandler());
            }
            
            private IEnumerator RunEnumeratorHandler()
            {
                _button.interactable = false;
                foreach (var action in _IEnumertors)
                {
                    yield return action?.Invoke();
                }
                foreach (var action in _YieldInstructions)
                {
                    yield return action?.Invoke();
                }
                _button.interactable = true;
            }
        }
        
        private static Dictionary<Button, ButtonEnumeratorListenerHandler> _buttonEnumeratorListenerHandlers = new Dictionary<Button, ButtonEnumeratorListenerHandler>();
        
        #endregion

        #region EnumeratorListener

        public static void AddIEnumeratorListener(this Button button, Func<IEnumerator> action)
        {
            if(button == null || action == null) return;
            if (_buttonEnumeratorListenerHandlers.ContainsKey(button))
            {
                _buttonEnumeratorListenerHandlers[button].AddListener(action);
                return;
            }
            var handler = new ButtonEnumeratorListenerHandler(button);
            handler.AddListener(action);
            _buttonEnumeratorListenerHandlers.Add(button, handler);
        }
        
        public static void RemoveIEnumeratorListener(this Button button, Func<IEnumerator> action)
        {
            if(button == null || action == null) return;
            if (!_buttonEnumeratorListenerHandlers.ContainsKey(button)) return;
            _buttonEnumeratorListenerHandlers[button].RemoveListener(action);
            if(_buttonEnumeratorListenerHandlers[button].Count == 0)
                _buttonEnumeratorListenerHandlers.Remove(button);
        }
        
        #endregion

        #region YieldInstructionListener

        public static void AddYieldInstructionListener(this Button button, Func<YieldInstruction> action)
        {
            if (button == null || action == null) return;
            if (_buttonEnumeratorListenerHandlers.ContainsKey(button))
            {
                _buttonEnumeratorListenerHandlers[button].AddListener(action);
                return;
            }
            var handler = new ButtonEnumeratorListenerHandler(button);
            handler.AddListener(action);
            _buttonEnumeratorListenerHandlers.Add(button, handler);
        }
        
        public static void RemoveYieldInstructionListener(this Button button, Func<YieldInstruction> action)
        {
            if (button == null || action == null) return;
            if (!_buttonEnumeratorListenerHandlers.ContainsKey(button)) return;
            _buttonEnumeratorListenerHandlers[button].RemoveListener(action);
            if (_buttonEnumeratorListenerHandlers[button].Count == 0)
                _buttonEnumeratorListenerHandlers.Remove(button);
        }
        
        #endregion
    }
}