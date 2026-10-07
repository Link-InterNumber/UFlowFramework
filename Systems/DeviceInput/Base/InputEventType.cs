using System;

namespace UFlowFramework
{
    [Flags]
    public enum InputEventType
    {
        Button = 1 << 0,
        Axis = 1 << 1,
        Scroll = 1 << 2,
        Point = Button | Axis,
        // PointerMove,
        // PointerDown,
        // PointerUp,
        // Text
    }

    public enum InputEventState
    {
        Released,
        PerformThisFrame,
        Hold,
        ReleaseThisFrame,
        // Cancel
    }
}