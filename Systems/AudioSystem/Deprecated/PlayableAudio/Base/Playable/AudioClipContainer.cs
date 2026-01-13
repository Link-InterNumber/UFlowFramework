using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using Random = UnityEngine.Random;

namespace PowerCellStudio
{
    public class AudioClipContainer: IDisposable
    {
        public Dictionary<string, AudioClipPlayable> clips;
        private string[] _clipNames; 
        private PlayableGraph _playableGraph;
        public static int MinSize = 64;

        public static AudioClipContainer Create(PlayableGraph graph, AudioClip defaultClip, bool loop)
        {
            var container = new AudioClipContainer();
            container.Init(graph, defaultClip, loop);
            return container;
        }

        private void Init(PlayableGraph graph, AudioClip defaultClip, bool loop)
        {
            // clips = new List<AudioClipPlayable>();
            _playableGraph = graph;
            Add(defaultClip, loop);
        }

        private bool VerifyClips()
        {
            if (clips == null || clips.Count == 0)
            {
                Debug.LogError("Please Add Clip First");
                return false;
            }

            return true;
        }
        
        public void Add(AudioClip[] clip, bool[] loop)
        {
            if(clip == null || clip.Length == 0) return;
            for (var index = 0; index < clip.Length; index++)
            {
                var audioClip = clip[index];
                if (!audioClip) continue;
                var isLoop = loop != null && loop.Length - 1 >= index && loop[index];
                Add(audioClip, isLoop);
            }
        }

        public void Add(AudioClip clip, bool loop)
        {
            if (clips == null)
            {
                clips = new Dictionary<string, AudioClipPlayable>();
                _clipNames = Enumerable.Repeat(string.Empty, MinSize).ToArray();
            }
            CreateClipPlayable(clip, loop);
        }

        public void Pause()
        {
            if (!VerifyClips()) return;
            foreach (var audioClipPlayable in clips)
            {
                audioClipPlayable.Value.Pause();
            }
        }

        public void Resume()
        {
            if (!VerifyClips()) return;
            foreach (var audioClipPlayable in clips)
            {
                var output = audioClipPlayable.Value.GetOutput(0);
                if(output.IsNull() || !output.IsValid()) continue;
                audioClipPlayable.Value.Play();
            }
        }

        public void SetTime(int index, float time)
        {
            GetClip(index).SetTime(time);
        }

        public void SetTime(string name, float time)
        {
            if (clips.TryGetValue(name, out var match))
            {
                match.SetTime(time);
            }
        }

        private void CreateClipPlayable(AudioClip clip, bool loop)
        {
            if (clip == null || clips.ContainsKey(clip.name)) return;
            var clipPlayable = AudioClipPlayable.Create(_playableGraph, clip, loop);
            clips.Add(clip.name, clipPlayable);
            if (_clipNames.Length < clips.Count)
            {
                var lengthBefore = _clipNames.Length;
                Array.Resize(ref _clipNames, _clipNames.Length + MinSize);
                for (int i = _clipNames.Length; i >= lengthBefore; i--)
                {
                    _clipNames[i] = string.Empty;
                }
            }
            _clipNames[clips.Count - 1] = clip.name;
            clipPlayable.Pause();
        }

        public void RemoveByIndex(int index)
        {
            if (index > _clipNames.Length - 1 || index < 0) return;
            var clipName = _clipNames[index];
            if (clips.TryGetValue(clipName, out var clip))
            {
                clip.Destroy();
            }
            clips.Remove(clipName);
            for (int i = index; i < _clipNames.Length; i++)
            {
                var next = i + 1 >= _clipNames.Length ? string.Empty : _clipNames[i + 1];
                _clipNames[i] = next;
                if(next.Equals(string.Empty)) break;

            }
        }

        public void RemoveByName(string animationName)
        {
            if (!VerifyClips()) return;
            for (int i = 0; i < _clipNames.Length; i++)
            {
                if (!_clipNames[i].Equals(animationName)) continue;
                RemoveByIndex(i);
                break;
            }
        }

        public void RemoveByClip(AudioClip clip)
        {
            if (!VerifyClips()) return;
            RemoveByName(clip.name);
        }

        public void RemoveAll()
        {
            if (!VerifyClips()) return;
            foreach (var audioClipPlayable in clips)
            {
                _playableGraph.DestroyPlayable(audioClipPlayable.Value);

            }
            clips.Clear();
            _clipNames = Enumerable.Repeat(string.Empty, MinSize).ToArray();
        }

        private AudioClipPlayable GetClip(int index)
        {
            if (index > clips.Count - 1 || index < 0)
            {
#if UNITY_EDITOR
                throw new IndexOutOfRangeException();
#endif
                return default;
            }
            var clipName = _clipNames[index];
            return clips.TryGetValue(clipName, out var clip) ? clip : default;
        }

        // private AudioClipPlayable PlayClip(int index, bool restart)
        // {
        //     var clip = GetClip(index);
        //     if (restart) clip.SetTime(0);
        //     clip.Play();
        //     return clip;
        // }

        // private void StopClip(int index)
        // {
        //     var clip = GetClip(index);
        //     clip.SetTime(0);
        //     clip.Pause();
        // }

        public AudioClipPlayable GetByIndex(int index)
        {
            if (!VerifyClips()) return default;
            if (index > clips.Count - 1 || index < 0) return default;
            return GetClip(index);
        }

        public AudioClipPlayable GetByName(string clipName)
        {
            if (!VerifyClips()) return default;
            for (int i = 0; i < _clipNames.Length; i++)
            {
                if (!_clipNames[i].Equals(clipName)) continue;
                return GetClip(i);
            }
            return default;
        }

        public AudioClipPlayable PlayByIndex(int index, bool restart = false)
        {
            if (!VerifyClips()) return default;
            if (index > clips.Count - 1 || index < 0) return default;
            var clipName = _clipNames[index];
            return PlayByName(clipName, restart);
        }

        public AudioClipPlayable PlayByName(string clipName, bool restart = false)
        {
            if (!VerifyClips()) return default;
            if (clips.TryGetValue(clipName, out var clip))
            {
                if (restart) clip.SetTime(0);
                clip.Play();
                return clip;
            }
            return default;
        }

        private int _curRandIndex = -1;

        public AudioClipPlayable PlayRandOne()
        {
            if (!VerifyClips()) return default;
            if (clips.Count == 1) return clips.First().Value;
            var rollList = new List<int>();
            for (int i = 0; i < clips.Count; i++)
            {
                if (_curRandIndex == i) continue;
                rollList.Add(i);
            }
            var rolledIndex = Random.Range(0, rollList.Count - 1);
            _curRandIndex = rollList[rolledIndex];
            var name = _clipNames[_curRandIndex];
            return PlayByName(name, true);
        }

        public void PlayAll(bool restart)
        {
            if (!VerifyClips()) return;
            foreach (var audioClipPlayable in clips)
            {
                if (restart) audioClipPlayable.Value.SetTime(0);
                audioClipPlayable.Value.Play();
            }
        }

        public void StopByIndex(int index)
        {
            if (!VerifyClips()) return;
            if (index > clips.Count - 1 || index < 0) return;
            var clipName = _clipNames[index];
            if (clips.TryGetValue(clipName, out var clip))
            {
                clip.SetTime(0);
                clip.Pause();
            }
        }

        public void StopByName(string clipName)
        {
            if (!VerifyClips()) return;
            if (clips.TryGetValue(clipName, out var clip))
            {
                clip.SetTime(0);
                clip.Pause();
            }
        }

        public void StopAll()
        {
            if (!VerifyClips()) return;
            foreach (var audioClipPlayable in clips)
            {
                audioClipPlayable.Value.SetTime(0);
                audioClipPlayable.Value.Pause();
            }
        }

        public void Dispose()
        {
            RemoveAll();
        }

        public void Destroy()
        {
            Dispose();
        }
    }
}