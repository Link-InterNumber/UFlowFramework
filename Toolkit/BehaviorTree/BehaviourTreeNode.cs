// BehaviourTreeNode.cs
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class BehaviourTreeNode
    {
        public enum NodeState
        {
            Running,
            Success,
            Failure
        }
        
        public abstract NodeState Tick();
    }

    public abstract class CompositeNode : BehaviourTreeNode
    {
        public BehaviourTreeNode[] children;
    }

    public class SelectorNode : CompositeNode
    {
        public SelectorNode(params BehaviourTreeNode[] nodes)
        {
            children = nodes;
        }

        public override NodeState Tick()
        {
            foreach (var child in children)
            {
                switch (child.Tick())
                {
                    case NodeState.Success:
                        return NodeState.Success;
                    case NodeState.Running:
                        return NodeState.Running;
                    default:
                        break;
                }
            }
            return NodeState.Failure;
        }
    }

    public class SequenceNode : CompositeNode
    {
        public SequenceNode(params BehaviourTreeNode[] nodes)
        {
            children = nodes;
        }

        public override NodeState Tick()
        {
            foreach (var child in children)
            {
                switch (child.Tick())
                {
                    case NodeState.Failure:
                        return NodeState.Failure;
                    case NodeState.Running:
                        return NodeState.Running;
                    default:
                        break;
                }
            }
            return NodeState.Success;
        }
    }

    public class ActionNode : BehaviourTreeNode
    {
        private System.Func<NodeState> action;

        public ActionNode(System.Func<NodeState> action)
        {
            this.action = action;
        }

        public override NodeState Tick()
        {
            return action();
        }
    }

    public class ConditionNode : BehaviourTreeNode
    {
        private System.Func<bool> condition;

        public ConditionNode(System.Func<bool> condition)
        {
            this.condition = condition;
        }

        public override NodeState Tick()
        {
            return condition() ? NodeState.Success : NodeState.Failure;
        }
    }

    public class ParallelNode : BehaviourTreeNode
    {
        private BehaviourTreeNode[] children;

        public ParallelNode(params BehaviourTreeNode[] nodes)
        {
            children = nodes;
        }

        public override NodeState Tick()
        {
            bool anyRunning = false;
            foreach (var child in children)
            {
                var result = child.Tick();
                if (result == NodeState.Failure)
                    return NodeState.Failure;
                if (result == NodeState.Running)
                    anyRunning = true;
            }
            return anyRunning ? NodeState.Running : NodeState.Success;
        }
    }
}