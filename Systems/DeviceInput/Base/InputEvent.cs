using System;
using UnityEngine;

namespace UFlowFramework
{
    /// <summary>统一输入数据，不暴露 Input System 或旧 Input API。</summary>
    public struct InputEvent<TKey>
    {
        private TKey _actionKey;
        public TKey actionKey => _actionKey;
        
        private InputEventType _type;
        public InputEventType type => _type;
        
        private InputEventState _state;
        public InputEventState state => _state;
        
        private float _value;
        public float value => _value;
        
        private float _x;
        public float x => _x;
        
        private float _y;
        public float y => _y;
        
        private float _holdTime;
        public float holdTime => _holdTime;
        
        public Vector2 v2 => new Vector2(x, y);

        private InputEvent(TKey actionKey, InputEventType type, InputEventState state = InputEventState.Released, float value = 0, float x = 0, float y = 0)
        {
            _actionKey = actionKey;
            _type = type;
            _state = state;
            _value = value;
            _x = x;
            _y = y;
            _holdTime = 0;
        }

        internal void SetHoldTime(float time)
        {
            _holdTime = time;
        }
        
        public bool isClickPerformance => state == InputEventState.ReleaseThisFrame && (type & InputEventType.Button) != 0 && holdTime < 0.2f;
        
        public static InputEvent<TKey> CreateValueEvent(TKey actionKey, InputEventType eventType, InputEventState state, float v, Vector2 v2 = default)
        {
            return new InputEvent<TKey>(actionKey, eventType, state: state, v, v2.x, v2.y);
        }
        
        public static InputEvent<TKey> CreateButtonDownEvent(TKey actionKey, float pressValue = 1)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Button, InputEventState.PressThisFrame, pressValue);
        }
        
        public static InputEvent<TKey> CreateButtonUpEvent(TKey actionKey, float pressValue = 0)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Button, InputEventState.ReleaseThisFrame, pressValue);
        }
        
        public static InputEvent<TKey> CreateButtonHoldEvent(TKey actionKey, float pressValue = 1)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Button, InputEventState.Hold, pressValue);
        }
        
        public static InputEvent<TKey> CreatePointDownEvent(TKey actionKey, Vector2 pos)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Point, InputEventState.PressThisFrame, 1, pos.x, pos.y);
        }
        
        public static InputEvent<TKey> CreatePointUpEvent(TKey actionKey, Vector2 pos)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Point, InputEventState.ReleaseThisFrame, 0, pos.x, pos.y);
        }
        
        public static InputEvent<TKey> CreatePointHoldEvent(TKey actionKey, Vector2 pos)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Point, InputEventState.Hold, 1, pos.x, pos.y);
        }
        
        public static InputEvent<TKey> CreatePointMoveEvent(TKey actionKey, Vector2 pos)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Point, InputEventState.Released, 1, pos.x, pos.y);
        }
        
        public static InputEvent<TKey> CreateVector2Event(TKey actionKey, InputEventState state, Vector2 v, float pressValue = 0)
        {
            return new InputEvent<TKey>(actionKey, InputEventType.Axis, state, pressValue, v.x, v.y);
        }
    }
}