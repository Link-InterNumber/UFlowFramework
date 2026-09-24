using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace UFlowFramework
{
	/// <summary>
	/// 监控玩家最近使用的输入设备类型，并在设备类型发生变化时发送事件。
	/// </summary>
	public sealed class DeviceDetector : MonoBehaviour
	{
		public enum DeviceType
		{
			Unknown,
			KeyboardMouse,
			Gamepad,
			Touch,
		}

		[SerializeField]
		private bool _detectTouch = true;

		[SerializeField]
		private float _axisThreshold = 0.2f;

#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
		[SerializeField]
		private bool _preferInputSystem = true;
#endif

		private DeviceType _currentDevice;

		/// <summary>当前检测到的玩家输入设备类型。</summary>
		public DeviceType currentDevice => _currentDevice;

		/// <summary>设备类型发生变化时触发，参数为新的设备类型。</summary>
		public event Action<DeviceType> deviceChanged;

		private void OnEnable()
		{
			_axisThreshold = Mathf.Clamp01(_axisThreshold);
			_currentDevice = DeviceType.Unknown;

#if ENABLE_INPUT_SYSTEM
			InputSystem.onEvent += OnInputSystemEvent;
#endif
		}

		private void OnDisable()
		{
#if ENABLE_INPUT_SYSTEM
			InputSystem.onEvent -= OnInputSystemEvent;
#endif
		}

		private void Update()
		{
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
			if (_preferInputSystem)
			{
				return;
			}

			DetectLegacyInput();
#elif ENABLE_INPUT_SYSTEM
			// Input System 通过 InputSystem.onEvent 检测，无需每帧轮询。
#elif ENABLE_LEGACY_INPUT_MANAGER
			DetectLegacyInput();
#endif
		}

		private void SetCurrentDevice(DeviceType deviceType)
		{
			if (deviceType == DeviceType.Unknown || deviceType == _currentDevice)
			{
				return;
			}

			var previousDevice = _currentDevice;
			_currentDevice = deviceType;

			if (previousDevice != DeviceType.Unknown)
			{
				deviceChanged?.Invoke(deviceType);
			}
		}

#if ENABLE_INPUT_SYSTEM
		private void OnInputSystemEvent(InputEventPtr inputEvent, InputDevice inputDevice)
		{
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
			if (!_preferInputSystem)
			{
				return;
			}
#endif

			if (inputDevice == null || !inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>())
			{
				return;
			}

			if (inputDevice is Gamepad)
			{
				SetCurrentDevice(DeviceType.Gamepad);
				return;
			}

			if (_detectTouch && inputDevice is Touchscreen)
			{
				SetCurrentDevice(DeviceType.Touch);
				return;
			}

			if (inputDevice is Keyboard || inputDevice is Mouse)
			{
				SetCurrentDevice(DeviceType.KeyboardMouse);
			}
		}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
		private void DetectLegacyInput()
		{
			if (_detectTouch && Input.touchCount > 0)
			{
				SetCurrentDevice(DeviceType.Touch);
				return;
			}

			if (HasLegacyGamepadInput())
			{
				SetCurrentDevice(DeviceType.Gamepad);
				return;
			}

			if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
				Input.GetMouseButtonDown(2) ||
				Mathf.Abs(Input.GetAxisRaw("Mouse X")) > _axisThreshold ||
				Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > _axisThreshold)
			{
				SetCurrentDevice(DeviceType.KeyboardMouse);
				return;
			}

			if (Input.anyKeyDown)
			{
				SetCurrentDevice(DeviceType.KeyboardMouse);
				return;
			}
		}

		private bool HasLegacyGamepadInput()
		{
			for (var index = 0; index < 20; index++)
			{
				if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + index)))
				{
					return true;
				}
			}

			return Mathf.Abs(Input.GetAxisRaw("Joy X")) > _axisThreshold ||
				   Mathf.Abs(Input.GetAxisRaw("Joy Y")) > _axisThreshold ||
				   Mathf.Abs(Input.GetAxisRaw("Joy Z")) > _axisThreshold ||
				   Mathf.Abs(Input.GetAxisRaw("Joy Rz")) > _axisThreshold;
		}
#endif
	}
}
