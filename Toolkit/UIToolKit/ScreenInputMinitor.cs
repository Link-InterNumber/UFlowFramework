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
         var tapHandle = new ScreenTapHandle();
         var dragHandle = new ScreenDragHandle();
         var pinchHandle = new ScreenPinchHandle();
         var twoFingerDragHandle = new ScreenTwoFingerDragHandle();
         // 注册输入处理器
         RegisterInputHandle(tapHandle);
         RegisterInputHandle(dragHandle);
         RegisterInputHandle(pinchHandle);
         RegisterInputHandle(twoFingerDragHandle);

         #region Test
         tapHandle.RegisterInput(OnTapEvent);
         dragHandle.RegisterInput(OnDragEvent);
         pinchHandle.RegisterInput(OnPinchEvent);
         twoFingerDragHandle.RegisterInput(OnTwoFingerDragEvent);
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
         _inputHandles[typeof(T)] = handle;
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

      public void RegisterInput<T>(ScreenInputEventHandler action) where T : IScreenInputHandle
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

      public void UnregisterInput<T>(ScreenInputEventHandler action) where T : IScreenInputHandle
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
               handle.Dispose();
               _inputHandles.Remove(type);
            }
         }
         _removeBuffer.Clear();
      }

      #region Test
      // 事件分发
      private void OnTapEvent(ScreenInputEvent e)
      {
         Debug.LogError($"Tap Detected: {e.position}, Tap Count: {e.tapCount}");
      }
      private void OnDragEvent(ScreenInputEvent e)
      {
         Debug.LogError($"Drag Detected: {e.delta}, State: {e.state}");
      }
      private void OnPinchEvent(ScreenInputEvent e)
      {
         Debug.LogError($"Pinch Detected: {e.pinchDelta}, State: {e.state}");
      }
      private void OnTwoFingerDragEvent(ScreenInputEvent e)
      {
         Debug.LogError($"Two-Finger Drag Detected: {e.delta}, State: {e.state}");
      }
      #endregion
   }
}