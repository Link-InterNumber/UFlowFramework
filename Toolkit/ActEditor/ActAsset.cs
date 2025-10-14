using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [CreateAssetMenu(menuName = "Act/ActAsset")]
    public class ActAsset : ScriptableObject
    {
        public float duration => tracks.Count == 0 ? 0f : Mathf.Max(tracks.ConvertAll(t => t.duration).ToArray());
        public List<ActTrackData> tracks = new();
    }
}