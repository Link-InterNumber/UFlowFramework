using System;
using UnityEngine;

namespace UFlowFramework
{
    /// <summary>统一输入数据，不暴露 Input System 或旧 Input API。</summary>
    public readonly struct InputEvent<TKey>
    {
        public readonly TKey actionKey;
        public readonly InputEventType type;
        public readonly InputEventState state;
        public readonly float value;
        public readonly float x;
        public readonly float y;
        public Vector2 v2 => new Vector2(x, y);

        public InputEvent(TKey actionKey, InputEventType type, InputEventState state = InputEventState.NoInput, float value = 0, float x = 0, float y = 0)
        {
            this.actionKey = actionKey;
            this.type = type;
            this.state = state;
            this.value = value;
            this.x = x;
            this.y = y;
        }
        
        public static InputEvent<TKey> CreateValueEvent(TKey actionKey, InputEventType eventType, InputEventState state, float v)
        {
            return new InputEvent<TKey>(actionKey, eventType, state: state, v, 0, 0);
        }
        
        public static InputEvent<TKey> CreateButtonEvent(TKey actionKey, bool isPressThisFrame, bool isReleaseThisFrame, bool pressed)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Button, isPressThisFrame ? InputEventState.PressThisFrame : isReleaseThisFrame ? InputEventState.ReleaseThisFrame : InputEventState.Hold, pressed ? 1 : 0,  0,  0);
        }
        
        public static InputEvent<TKey> CreateVector2Event(TKey actionKey, InputEventState state, Vector2 v)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Axis, state, value: 0, x: v.x, y: v.y);
        }
    }
}