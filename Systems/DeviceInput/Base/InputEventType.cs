namespace UFlowFramework
{
    public enum InputEventType
    {
        Button,
        Axis,
        // PointerMove,
        // PointerDown,
        // PointerUp,
        // Text
    }

    public enum InputEventState
    {
        NoInput,
        PressThisFrame,
        Hold,
        ReleaseThisFrame,
    }
}