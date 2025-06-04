using System.Collections.Generic;
using UnityEngine;
    
namespace PowerCellStudio
{
    // BehaviourTree.cs

    [CreateAssetMenu(menuName = "AI/Behaviour Tree")]
    public class BehaviourTree : ScriptableObject
    {
        public BehaviourTreeNode rootNode;

        public BehaviourTree(BehaviourTreeNode node)
        {
            rootNode = node;
            return this;
        }

        public void Tick()
        {
            if (rootNode != null)
            {
                rootNode.Tick();
            }
        }
    }
}