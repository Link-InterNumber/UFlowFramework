using UnityEngine.Audio;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public class AudioSwitcher: LinkPlayableBehaviour
    {
        public bool DestroyOnDisconnect = true;
        public bool ControlGraph = false;
        private AudioClipPlayable _curClip;
        
        public static ScriptPlayable<AudioSwitcher> Create(PlayableGraph graph)
        {
            var playable = ScriptPlayable<AudioSwitcher>.Create(graph);
            playable.GetBehaviour().Init(graph, playable);
            playable.SetInputCount(1);
            playable.SetOutputCount(1);
            return playable;
        }

        private void Init(PlayableGraph graph, ScriptPlayable<AudioSwitcher> playable)
        {
            _playableGraph = graph;
            _playable = playable;
        }

        public void PlayClip(AudioClipPlayable clip)
        {
            var cur  = _playable.GetInput(0);
            if(!cur.IsNull() && cur.IsValid())
            {
                _playable.DisconnectInput(0);
                if(DestroyOnDisconnect && !cur.Equals(clip)) cur.Destroy();
            }
            _curClip = clip;
            _playable.ConnectInput(0, clip, 0);
            _playable.SetInputWeight(0, 1f);
            clip.SetTime(0);
            clip.Play();
            if(_playable.GetPlayState() == PlayState.Paused) _playable.Play();
            if(ControlGraph && !_playableGraph.IsPlaying()) _playableGraph.Play();
        }

        private void RemoveCurClip()
        {
            _playable.DisconnectInput(0);
            if(DestroyOnDisconnect) _curClip.Destroy();
            _curClip = default;
            _playable.Pause();
            _playable.SetTime(0f);
            if(ControlGraph) _playableGraph.Stop();
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            base.PrepareFrame(playable, info);
            if (!_curClip.IsValid() || !_curClip.IsDone()) return;
            RemoveCurClip();
        }
        
        public void RemoveAll()
        {
            if (!_curClip.IsValid() || !_curClip.IsDone()) return;
            RemoveCurClip();
        }
    }
}