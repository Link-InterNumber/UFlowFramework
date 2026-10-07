using System.Collections.Generic;
using UnityEngine;

namespace UFlowFramework.Sample
{
    public enum LegacyInputKey
    {
        Move,
        Fire,
        Jump
    }
    
    public class LegacyInputAdapter : MonoBehaviour, IInputAdapter<LegacyInputKey>
    {
        private InputStack<LegacyInputKey> _inputStack;
        public InputStack<LegacyInputKey> inputStack => _inputStack;

        private List<ILegacyInputCapturer<LegacyInputKey>> _capturers;

        private void Awake()
        {
            _inputStack = new InputStack<LegacyInputKey>(() => new InputPage<LegacyInputKey>());
            _capturers = new List<ILegacyInputCapturer<LegacyInputKey>>();
            _capturers.Add(new MoveCapturer());
            _capturers.Add(new FireCapturer());
            _capturers.Add(new JumpCapturer());
        }

        private void OnDestroy()
        {
            _inputStack.Dispose();
            _inputStack = null;
            foreach (var iLegacyInputCapturer in _capturers)
            {
                iLegacyInputCapturer.Dispose();
            }
            _capturers.Clear();
            _capturers = null;
        }
        
        private void Update()
        {
            if (_inputStack.GetCurrentPage()?.isEmpty ?? true)
            {
                return;
            }
            for (int i = 0; i < _capturers.Count; i++)
            {
                var capturer = _capturers[i];
                if (!capturer.TryGetEvent(out var inputEvent)) continue;
                _inputStack.Dispatch(in inputEvent);
            }
        }
    }
}