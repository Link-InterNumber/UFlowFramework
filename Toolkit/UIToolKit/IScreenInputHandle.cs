using UnityEngine;
using System;

namespace PowerCellStudio
{
   public enum ScreenInputEventState
   {
      Start,
      Execute,
      End,
   }

   public struct ScreenInputEvent
   {
      public Vector2 position;
      public Vector2 delta;
      public float pressTime;
      public float pinchDelta;
      public int tapCount;
      public ScreenInputEventState state;
   }

   public delegate void ScreenInputEventHandler(ScreenInputEvent inputEvent);

   public interface IScreenInputHandle : IDisposable
   {
      void RegisterInput(ScreenInputEventHandler action);
      void UnregisterInput(ScreenInputEventHandler action);
      void OnEnable();
      void OnDisable();
      void OnUpdate();
   }
}