using System.Collections.Generic;
using UnityEngine;
using System;

namespace PowerCellStudio
{
   public class ScreenInputMinitor : MonoBehaviour
   {
      private Dictionary<Type, IScreenInputHandle> _inputHandles;

      private List<Type> _removeBuffer;

      private void Awake()
      {
         _inputHandles = new Dictionary<Type, IScreenInputHandle>();
         _removeBuffer = new List<Type>();
         #region Test
         var tapHandle = new ScreenTapHandle();
         var dragHandle = new ScreenDragHandle();
         var pinchHandle = new ScreenPinchHandle();
         var twoFingerDragHandle = new ScreenTwoFingerDragHandle();
         var longPressHandle = new ScreenLongPressHandle();
         // 注册输入处理器
         RegisterInputHandle(tapHandle);
         RegisterInputHandle(dragHandle);
         RegisterInputHandle(pinchHandle);
         RegisterInputHandle(twoFingerDragHandle);
         RegisterInputHandle(longPressHandle);

         tapHandle.RegisterInput(OnTapEvent);
         dragHandle.RegisterInput(OnDragEvent);
         pinchHandle.RegisterInput(OnPinchEvent);
         twoFingerDragHandle.RegisterInput(OnTwoFingerDragEvent);
         longPressHandle.RegisterInput(OnLongPress);

         #endregion
      }
      
      private void OnDestroy()
      {
         // 注销所有输入处理器
         foreach (var handle in _inputHandles.Values)
         {
            handle.Dispose();
         }
         _inputHandles.Clear();
      }

      public void RegisterInputHandle<T>() where T : IScreenInputHandle, new()
      {
         var handle = new T();
         RegisterInputHandle(handle);
      }

      public void RegisterInputHandle<T>(T handle) where T : IScreenInputHandle
      {
         if (handle == null) return;
         var type = typeof(T);
         if (_inputHandles.ContainsKey(type))
         {
            _inputHandles[type].Dispose();
            _removeBuffer.Add(type);
         }
         _inputHandles[type] = handle;
         handle.OnEnable();
      }

      public void UnregisterInputHandle<T>() where T : IScreenInputHandle
      {
         var type = typeof(T);
         if (_inputHandles.ContainsKey(type))
         {
            _inputHandles[type].Dispose();
            _removeBuffer.Add(type);
         }
      }

      public bool IsInputHandleRegistered<T>() where T : IScreenInputHandle
      {
         return _inputHandles.ContainsKey(typeof(T));
      }

      public bool TryGetInputHandle<T>(out T handle) where T : IScreenInputHandle
      {
         if (_inputHandles.TryGetValue(typeof(T), out var exitedHandle))
         {
            handle = (T)exitedHandle;
            return true;
         }
         handle = default;
         return false;
      }

      public void AddListener<T>(ScreenInputEventHandler action) where T : IScreenInputHandle
      {
         if (_inputHandles.TryGetValue(typeof(T), out var handle))
         {
            handle.RegisterInput(action);
         }
         else
         {
            Debug.LogWarning($"Input handle of type {typeof(T)} is not registered.");
         }
      }

      public void RemoveListener<T>(ScreenInputEventHandler action) where T : IScreenInputHandle
      {
         if (_inputHandles.TryGetValue(typeof(T), out var handle))
         {
            handle.UnregisterInput(action);
         }
         else
         {
            Debug.LogWarning($"Input handle of type {typeof(T)} is not registered.");
         }
      }

      public void EnableInput<T>()
      {
         if (TryGetInputHandle<T>(out var handler))
         {
            handler.OnEnable();
         }
      }

      public void DisableInput<T>()
      {
         if (TryGetInputHandle<T>(out var handler))
         {
            handler.OnDisable();
         }
      }

      private void OnEnable()
      {
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#endif
         // 启用所有输入处理器
         foreach (var handle in _inputHandles.Values)
         {
            handle.OnEnable();
         }
      }

      private void OnDisable()
      {
#if ENABLE_INPUT_SYSTEM
         UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
#endif
         // 禁用所有输入处理器
         foreach (var handle in _inputHandles.Values)
         {
            handle.OnDisable();
         }
      }

      private void Update()
      {
         // 更新所有输入处理器
         foreach (var handle in _inputHandles.Values)
         {
            handle.OnUpdate();
         }
         // 清理已标记为删除的处理器
         if (_removeBuffer.Count == 0) return;
         foreach (var type in _removeBuffer)
         {
            if (_inputHandles.TryGetValue(type, out var handle))
            {
               _inputHandles.Remove(type);
            }
         }
         _removeBuffer.Clear();
      }

      #region Test
      // 事件分发
      private void OnTapEvent(ScreenInputEvent inputEvent)
      {
         Debug.LogError($"Tap Detected: {inputEvent.screenPos}, Tap Count: {inputEvent.tapCount}");
      }
      private void OnDragEvent(ScreenInputEvent inputEvent)
      {
         Debug.LogError($"Drag Detected: {inputEvent.delta}, State: {inputEvent.state}");
      }
      private void OnPinchEvent(ScreenInputEvent inputEvent)
      {
         Debug.LogError($"Pinch Detected: {inputEvent.pinchDelta}, State: {inputEvent.state}");
      }
      private void OnTwoFingerDragEvent(ScreenInputEvent inputEvent)
      {
         Debug.LogError($"Two-Finger Drag Detected: {inputEvent.delta}, State: {inputEvent.state}");
      }
      private void OnLongPress(ScreenInputEvent inputEvent)
      {
         Debug.LogError($"Long Press Detected: {inputEvent.screenPos}, Time: {inputEvent.pressTime}, State: {inputEvent.state}");
      }

      #endregion
   }
}