using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [CreateAssetMenu(menuName = "Act/ActAsset")]
    public class ActAsset : ScriptableObject
    {
        public float duration => tracks.Count == 0 ? 0f : Mathf.Max(tracks.ConvertAll(t => t.duration).ToArray());
        public List<ActTrackData> tracks = new List<ActTrackData>();

        public bool IsReady()
        {
            if (tracks == null || tracks.Count == 0) return true;
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                for (int j = 0; j < track.clips.Count; j++)
                {
                    var clip = track.clips[j];
                    if (!clip.IsReady)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        public void Restart()
        {
            _time = 0f;
        }

        private float _time;
        public void Simulate(float dt, ActRuntimePlayer target, out bool isEnd)
        {
            if (tracks == null || tracks.Count == 0)
            {
                isEnd = true;
                return;
            }

            var maxTime = 0f;
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                for (int j = 0; j < track.clips.Count; j++)
                {
                    var clip = track.clips[j];
                    maxTime = Mathf.Max(maxTime, clip.start + clip.length);
                    if (_time < clip.start)
                    {
                        if (_time + dt >= clip.start)
                            clip.OnStart(target);
                        continue;
                    }
                    if (_time > clip.start + clip.length) continue;
                    clip.DoAction(target, _time);
                    if (_time + dt > clip.start + clip.length)
                    {
                        clip.OnEnd(target);
                    }
                }
            }
            _time += dt;
            isEnd = _time > maxTime;
        }
    }
}