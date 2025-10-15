using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class ActTrackData
    {
        public string name = "Track";
        public Color color = new Color(0.3f, 0.6f, 0.9f);
        [SerializeReference]
        public List<ActClipData> clips = new List<ActClipData>();
        public float duration => clips.Count == 0 ? 0f : Mathf.Max(clips.ConvertAll(c => c.duration).ToArray());
    }
}