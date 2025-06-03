using UnityEngine;

namespace PowerCellStudio
{
    [System.Serializable]
    public class ACTAction
    {
        public string actionName;
        public AnimationClip animationClip;
        public List<HitBoxFrame> hitFrames = new List<HitBoxFrame>();
        public List<TransitionCondition> transitions = new List<TransitionCondition>();
    }

    [System.Serializable]
    public class HitBoxFrame
    {
        public int frame;
        public Vector3 position;
        public Vector3 size;
        public float damage;
    }

    [System.Serializable]
    public class TransitionCondition
    {
        public InputType inputType;
        public string targetAction;
        public int priority;
    }

    public enum InputType
    {
        AttackButton,
        DirectionInput,
        ComboCount
    }
}