using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UFlowFramework
{
	/// <summary>
	/// 运行时按键重绑定组件。Input System 和 Legacy Input Manager 同时启用时优先使用 Input System。
	/// Runtime input rebinding component. Input System takes priority when both input backends are enabled.
	/// </summary>
	public sealed class InputRebinder : MonoBehaviour
	{
		[Serializable]
		public sealed class BindingEntry
		{
			[SerializeField]
			private string _id;

			[SerializeField]
			private string _displayName;

#if ENABLE_INPUT_SYSTEM
			[SerializeField]
			private InputActionReference _inputAction;

			[SerializeField]
			private int _bindingIndex;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			[SerializeField]
			private KeyCode _legacyKey = KeyCode.None;
#endif

			public string id => _id;
			public string displayName => string.IsNullOrEmpty(_displayName) ? _id : _displayName;

			public BindingEntry(string id, string displayName)
			{
				_id = id;
				_displayName = displayName;
			}

			public BindingEntry()
			{
			}

#if ENABLE_INPUT_SYSTEM
			public InputActionReference inputAction => _inputAction;
			public int bindingIndex => _bindingIndex;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			public KeyCode legacyKey => _legacyKey;
			public void SetLegacyKey(KeyCode key)
			{
				_legacyKey = key;
			}
#endif
		}

		[SerializeField]
		private string _playerPrefsKey = "UFlowFramework.InputRebinder";

		[SerializeField]
		private List<BindingEntry> _bindings = new List<BindingEntry>();

		private RebindOperation _activeRebind;

		/// <summary>当前是否有重绑定操作正在进行。</summary>
		public bool isRebinding => _activeRebind != null;

		/// <summary>重绑定完成或取消时触发。参数依次为是否成功、绑定 ID、显示文本。</summary>
		public event Action<bool, string, string> rebindCompleted;

		private void Awake()
		{
			LoadBindings();
		}

		private void OnDisable()
		{
			CancelRebind();
		}

		private void OnDestroy()
		{
			CancelRebind();
		}

		/// <summary>
		/// 开始指定绑定的交互式重绑定。
		/// Starts interactive rebinding for the specified binding.
		/// </summary>
		public bool StartRebind(string bindingId)
		{
			if (_activeRebind != null || !TryGetBinding(bindingId, out var binding))
			{
				return false;
			}

#if ENABLE_INPUT_SYSTEM
			if (binding.inputAction != null && binding.inputAction.action != null)
			{
				return StartInputSystemRebind(binding);
			}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			return StartLegacyRebind(binding);
#else
			return false;
#endif
		}

		/// <summary>取消当前重绑定操作。</summary>
		public void CancelRebind()
		{
			if (_activeRebind == null)
			{
				return;
			}

			_activeRebind.Cancel();
			_activeRebind = null;
		}

		/// <summary>将指定绑定恢复为 Inspector 中的默认值。</summary>
		public bool ResetBinding(string bindingId)
		{
			if (!TryGetBinding(bindingId, out var binding))
			{
				return false;
			}

#if ENABLE_INPUT_SYSTEM
			if (binding.inputAction != null && binding.inputAction.action != null)
			{
				var action = binding.inputAction.action;
				if (binding.bindingIndex < 0 || binding.bindingIndex >= action.bindings.Count)
				{
					return false;
				}

				action.RemoveBindingOverride(binding.bindingIndex);
				SaveBindings();
				return true;
			}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			binding.SetLegacyKey(KeyCode.None);
			SaveBindings();
			return true;
#else
			return false;
#endif
		}

		/// <summary>恢复所有绑定的默认值。</summary>
		public void ResetAllBindings()
		{
			for (var index = 0; index < _bindings.Count; index++)
			{
				ResetBinding(_bindings[index].id);
			}
		}

		/// <summary>获取绑定当前显示文本。</summary>
		public string GetBindingDisplayString(string bindingId)
		{
			if (!TryGetBinding(bindingId, out var binding))
			{
				return string.Empty;
			}

#if ENABLE_INPUT_SYSTEM
			if (binding.inputAction != null && binding.inputAction.action != null)
			{
				var action = binding.inputAction.action;
				if (binding.bindingIndex >= 0 && binding.bindingIndex < action.bindings.Count)
				{
					return action.GetBindingDisplayString(binding.bindingIndex);
				}
			}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			return binding.legacyKey == KeyCode.None ? "Unassigned" : binding.legacyKey.ToString();
#else
			return string.Empty;
#endif
		}

#if ENABLE_LEGACY_INPUT_MANAGER
		/// <summary>查询 Legacy Input Manager 绑定在本帧是否按下。</summary>
		public bool GetLegacyKeyDown(string bindingId)
		{
			return TryGetBinding(bindingId, out var binding) &&
				binding.legacyKey != KeyCode.None && Input.GetKeyDown(binding.legacyKey);
		}
#endif

		private bool TryGetBinding(string bindingId, out BindingEntry binding)
		{
			binding = null;
			if (string.IsNullOrEmpty(bindingId))
			{
				return false;
			}

			for (var index = 0; index < _bindings.Count; index++)
			{
				if (_bindings[index] != null && string.Equals(_bindings[index].id, bindingId, StringComparison.Ordinal))
				{
					binding = _bindings[index];
					return true;
				}
			}

			return false;
		}

#if ENABLE_INPUT_SYSTEM
		private bool StartInputSystemRebind(BindingEntry binding)
		{
			var action = binding.inputAction.action;
			if (binding.bindingIndex < 0 || binding.bindingIndex >= action.bindings.Count)
			{
				return false;
			}

			action.Disable();
			var operation = action.PerformInteractiveRebinding(binding.bindingIndex)
				.WithCancelingThrough("<Keyboard>/escape")
				.OnComplete(_ => CompleteInputSystemRebind(binding, action))
				.OnCancel(_ => CancelInputSystemRebind(action, binding.id));

			_activeRebind = new RebindOperation(operation, action, binding.id);
			operation.Start();
			return true;
		}

		private void CompleteInputSystemRebind(BindingEntry binding, InputAction action)
		{
			_activeRebind?.DisposeInputSystemOperation();
			_activeRebind = null;
			action.Enable();
			SaveBindings();
			rebindCompleted?.Invoke(true, binding.id, GetBindingDisplayString(binding.id));
		}

		private void CancelInputSystemRebind(InputAction action, string bindingId)
		{
			_activeRebind?.DisposeInputSystemOperation();
			_activeRebind = null;
			action.Enable();
			rebindCompleted?.Invoke(false, bindingId, GetBindingDisplayString(bindingId));
		}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
		private bool StartLegacyRebind(BindingEntry binding)
		{
			_activeRebind = new RebindOperation(binding.id);
			return true;
		}

		private void Update()
		{
			if (_activeRebind == null || _activeRebind.usesInputSystem)
			{
				return;
			}

			foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
			{
				if (key == KeyCode.None || !Input.GetKeyDown(key) || !TryGetBinding(_activeRebind.bindingId, out var binding))
				{
					continue;
				}

				binding.SetLegacyKey(key);
				var bindingId = binding.id;
				_activeRebind = null;
				SaveBindings();
				rebindCompleted?.Invoke(true, bindingId, GetBindingDisplayString(bindingId));
				return;
			}
		}
#endif

		private void LoadBindings()
		{
#if ENABLE_INPUT_SYSTEM
			if (!string.IsNullOrEmpty(_playerPrefsKey))
			{
				var json = PlayerPrefs.GetString(_playerPrefsKey, string.Empty);
				if (!string.IsNullOrEmpty(json))
				{
					for (var index = 0; index < _bindings.Count; index++)
					{
						var binding = _bindings[index];
						if (binding?.inputAction?.action?.actionMap?.asset != null)
						{
							binding.inputAction.action.actionMap.asset.LoadBindingOverridesFromJson(json);
							break;
						}
					}
				}
			}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			for (var index = 0; index < _bindings.Count; index++)
			{
				var binding = _bindings[index];
				if (binding == null || string.IsNullOrEmpty(binding.id))
				{
					continue;
				}

				var key = PlayerPrefs.GetInt(GetLegacyKeyName(binding.id), (int)binding.legacyKey);
				binding.SetLegacyKey((KeyCode)key);
			}
#endif
		}

		private void SaveBindings()
		{
#if ENABLE_INPUT_SYSTEM
			for (var index = 0; index < _bindings.Count; index++)
			{
				var asset = _bindings[index]?.inputAction?.action?.actionMap?.asset;
				if (asset != null && !string.IsNullOrEmpty(_playerPrefsKey))
				{
					PlayerPrefs.SetString(_playerPrefsKey, asset.SaveBindingOverridesAsJson());
					PlayerPrefs.Save();
					break;
				}
			}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			for (var index = 0; index < _bindings.Count; index++)
			{
				var binding = _bindings[index];
				if (binding != null && !string.IsNullOrEmpty(binding.id))
				{
					PlayerPrefs.SetInt(GetLegacyKeyName(binding.id), (int)binding.legacyKey);
				}
			}

			PlayerPrefs.Save();
#endif
		}

#if ENABLE_LEGACY_INPUT_MANAGER
		private string GetLegacyKeyName(string bindingId)
		{
			return string.Concat(_playerPrefsKey, ".", bindingId);
		}
#endif

		private sealed class RebindOperation
		{
#if ENABLE_INPUT_SYSTEM
			private readonly InputActionRebindingExtensions.RebindingOperation _inputSystemOperation;
			private readonly InputAction _action;
			public bool usesInputSystem => _inputSystemOperation != null;
#endif
			public readonly string bindingId;

#if ENABLE_INPUT_SYSTEM
			public RebindOperation(InputActionRebindingExtensions.RebindingOperation operation, InputAction action, string bindingId)
			{
				_inputSystemOperation = operation;
				_action = action;
				this.bindingId = bindingId;
			}
#endif

			public RebindOperation(string bindingId)
			{
				this.bindingId = bindingId;
			}

			public void Cancel()
			{
#if ENABLE_INPUT_SYSTEM
				if (_inputSystemOperation != null)
				{
					_inputSystemOperation.Cancel();
					_inputSystemOperation.Dispose();
					_action?.Enable();
				}
#endif
			}

			public void DisposeInputSystemOperation()
			{
#if ENABLE_INPUT_SYSTEM
				_inputSystemOperation?.Dispose();
#endif
			}
		}
	}
}
