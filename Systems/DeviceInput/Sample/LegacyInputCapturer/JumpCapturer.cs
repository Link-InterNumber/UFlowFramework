using UnityEngine;

namespace UFlowFramework.Sample
{
    /// <summary>采集空格键跳跃输入。 Captures jump input from the space key.</summary>
    public class JumpCapturer : ILegacyInputCapturer<LegacyInputKey>
    {
        private bool _isPressed;

        /// <summary>采集当前跳跃状态。 Captures the current jump state.</summary>
        public bool TryGetEvent(out InputEvent<LegacyInputKey> inputEvent)
        {
            var pressed = false;

#if ENABLE_LEGACY_INPUT_MANAGER
            pressed = Input.GetKey(KeyCode.Space);
#endif

            var state = pressed
                ? (_isPressed ? InputEventState.Hold : InputEventState.PressThisFrame)
                : (_isPressed ? InputEventState.ReleaseThisFrame : InputEventState.Released);
            _isPressed = pressed;

            inputEvent = InputEvent<LegacyInputKey>.CreateValueEvent(
                LegacyInputKey.Jump, InputEventType.Button, state, pressed ? 1f : 0f);
            return true;
        }

        /// <summary>重置采集状态。 Resets the captured state.</summary>
        public void Dispose()
        {
            _isPressed = false;
        }
    }
}