using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

namespace PowerCellStudio
{
    /// <summary>
    /// Utility for triggering vibrations on mobile devices and controller rumble.
    /// Supports Unity's new Input System when ENABLE_INPUT_SYSTEM is defined, otherwise falls back to Handheld.Vibrate or no-op for controllers.
    /// </summary>
    public static class VibrationUtils
    {
        private const float DEFAULT_DURATION = 0.1f;

        /// <summary>
        /// Vibrate shortly (default duration).
        /// </summary>
        public static void VibrateShort()
        {
            Vibrate(DEFAULT_DURATION);
        }

        /// <summary>
        /// Vibrate for the specified duration in seconds.
        /// On mobile this uses Handheld.Vibrate (duration is platform dependent). For controllers the duration is used for motor rumble where available.
        /// </summary>
        public static void Vibrate(float duration)
        {
#if ENABLE_INPUT_SYSTEM
            // New Input System: try to vibrate any active gamepad(s) and also fallback to mobile vibrate
            TryRumbleGamepads(0.5f, 0.5f, duration);
            TryMobileVibrate();
#else
            // Legacy / no Input System: use Handheld.Vibrate on mobile, controllers generally unsupported
            TryMobileVibrate();
#endif
        }

        /// <summary>
        /// Rumble attached gamepads with low-frequency and high-frequency motors.
        /// If no gamepad is connected this is a no-op.
        /// </summary>
        /// <param name="lowFrequency">Low frequency motor intensity [0..1]</param>
        /// <param name="highFrequency">High frequency motor intensity [0..1]</param>
        /// <param name="duration">Duration in seconds</param>
        public static void Rumble(float lowFrequency, float highFrequency, float duration)
        {
#if ENABLE_INPUT_SYSTEM
            TryRumbleGamepads(lowFrequency, highFrequency, duration);
#else
            // No new input system: nothing to do for controllers
            TryMobileVibrate();
#endif
        }

        private static void TryMobileVibrate()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_WSA
            Handheld.Vibrate();
#else
            // Editor or unsupported platform - no-op
#endif
        }

#if ENABLE_INPUT_SYSTEM

        private static Coroutine _RumbleCoroutine;
        // We reference types conditionally to avoid compile errors when the package isn't present.
        private static void TryRumbleGamepads(float lowFrequency, float highFrequency, float duration)
        {
            // Defer creating the coroutine runner until needed
            if (_RumbleCoroutine != null)
            {
                StopRumble();
            }
            _RumbleCoroutine = CoroutineRunner.instance.StartCoroutine(RumbleRoutine(lowFrequency, highFrequency, duration));
        }

        private static void StopRumble()
        {
            if (_RumbleCoroutine != null)
            {
                CoroutineRunner.instance.StopCoroutine(_RumbleCoroutine);
                _RumbleCoroutine = null;
            }
            var gamepads = UnityEngine.InputSystem.Gamepad.all;
            if (gamepads.IsUnityNull() || gamepads.Count == 0)
            {
                // No gamepad connected
                return;
            }
            // Set motor speeds for all gamepads
            foreach (var gp in gamepads)
            {
                if (gp == null) continue;
                gp.SetMotorSpeeds(0f, 0f);
            }
        }

        private static IEnumerator RumbleRoutine(float low, float high, float duration)
        {
            // Acquire gamepads from the new Input System at runtime
            var gamepads = UnityEngine.InputSystem.Gamepad.all;
            if (gamepads.IsUnityNull() || gamepads.Count == 0)
            {
                // No gamepad connected
                yield break;
            }
            // Set motor speeds for all gamepads
            foreach (var gp in gamepads)
            {
                if (gp == null) continue;
                gp.SetMotorSpeeds(low, high);
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            StopRumble();
        }
#endif
    }
}
