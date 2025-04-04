using System;
using UnityEngine;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SpaceAfterAttribute : PropertyAttribute
    {
        public float spaceHeight;

        public SpaceAfterAttribute(float spaceHeight)
        {
            this.spaceHeight = spaceHeight;
        }
    }
}