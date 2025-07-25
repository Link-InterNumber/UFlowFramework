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
      public int inputId;
      public Vector2 screenPos;
      /// <summary>
      /// 基于屏幕坐标
      /// besed on screenPos
      /// </summary>
      public Vector2 delta;
      public float pressTime;
      /// <summary>
      /// 基于屏幕坐标
      /// besed on screenPos
      /// </summary>
      public float pinchDelta;
      public int tapCount;
      public ScreenInputEventState state;
   }

   public delegate void ScreenInputEventHandler(ScreenInputEvent inputEvent);

   public interface IScreenInputHandle : IDisposable
   {
      bool enable {get; set;}
      void RegisterInput(ScreenInputEventHandler action);
      void UnregisterInput(ScreenInputEventHandler action);
      void OnEnable();
      void OnDisable();
      void OnUpdate();
   }
}