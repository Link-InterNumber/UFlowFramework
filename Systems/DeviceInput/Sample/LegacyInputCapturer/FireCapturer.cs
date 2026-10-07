using UnityEngine;

namespace UFlowFramework.Sample
{
    /// <summary>采集鼠标左键开火输入。 Captures fire input from the left mouse button.</summary>
    public class FireCapturer : ILegacyInputCapturer<LegacyInputKey>
    {
        private bool _isPressed;

        /// <summary>采集当前开火状态。 Captures the current fire state.</summary>
        public bool TryGetEvent(out InputEvent<LegacyInputKey> inputEvent)
        {
            var isDown = false;
            var isUp = false;
            var isHold = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            isDown = Input.GetKeyDown(KeyCode.Mouse0);
            isUp = Input.GetKeyUp(KeyCode.Mouse0);
            isHold = Input.GetKey(KeyCode.Mouse0);
#endif
            if (isDown)
            {
                inputEvent = InputEvent<LegacyInputKey>.CreateButtonDownEvent(LegacyInputKey.Fire);
            }
            else if (isUp)
            {
                inputEvent = InputEvent<LegacyInputKey>.CreateButtonUpEvent(LegacyInputKey.Fire);
            }
            else if (isHold)
            {
                inputEvent = InputEvent<LegacyInputKey>.CreateButtonHoldEvent(LegacyInputKey.Fire);
            }
            else
            {
                inputEvent = default;
                return false;
            }
            return true;
        }

        /// <summary>重置采集状态。 Resets the captured state.</summary>
        public void Dispose()
        {
            _isPressed = false;
        }
    }
}