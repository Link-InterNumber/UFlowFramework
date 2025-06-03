using System.Collections.Generic;
using UnityEngine;
    
namespace PowerCellStudio
{
    // BehaviourTree.cs

    [CreateAssetMenu(menuName = "AI/Behaviour Tree")]
    public class BehaviourTree : ScriptableObject
    {
        public List<BehaviourTreeNode> nodes = new List<BehaviourTreeNode>();
        public BehaviourTreeNode rootNode;

        public void Execute()
        {
            if (rootNode != null)
            {
                rootNode.Execute();
            }
        }
    }
}