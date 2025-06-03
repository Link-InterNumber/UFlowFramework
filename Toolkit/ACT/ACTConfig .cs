using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    [CreateAssetMenu(menuName = "ACT/Config")]
    public class ACTConfig : ScriptableObject
    {
        public List<ACTAction> actions = new List<ACTAction>();
    }
}