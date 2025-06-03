// BehaviourTreeNode.cs
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class BehaviourTreeNode : ScriptableObject
    {
        public enum NodeState
        {
            Running,
            Success,
            Failure
        }

        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;
        [TextArea] public string description;
        
        public abstract NodeState Execute();
    }

    // CompositeNode.cs
    public abstract class CompositeNode : BehaviourTreeNode
    {
        public BehaviourTreeNode[] children;
    }

    // SelectorNode.cs
    public class SelectorNode : CompositeNode
    {
        public override NodeState Execute()
        {
            foreach (var child in children)
            {
                switch (child.Execute())
                {
                    case NodeState.Success:
                        return NodeState.Success;
                    case NodeState.Running:
                        return NodeState.Running;
                }
            }
            return NodeState.Failure;
        }
    }

    // SequenceNode.cs
    public class SequenceNode : CompositeNode
    {
        public override NodeState Execute()
        {
            foreach (var child in children)
            {
                switch (child.Execute())
                {
                    case NodeState.Failure:
                        return NodeState.Failure;
                    case NodeState.Running:
                        return NodeState.Running;
                }
            }
            return NodeState.Success;
        }
    }
}