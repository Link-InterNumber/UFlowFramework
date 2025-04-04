using UnityEngine.Playables;

namespace PowerCellStudio
{
    public abstract class LinkPlayableBehaviour: PlayableBehaviour
    {
        public bool enable { get => _enable; set => SetEnable(value); }

        protected bool _enable;

        protected Playable _playable;
        
        protected PlayableGraph _playableGraph;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            if (_playable.Equals(default)) _playable = playable;
            if (_enable)
            {
                OnEnable(playable);
            }
            else
            {
                OnDisable(playable);
            }
        }
        
        protected virtual void SetEnable(bool val)
        {
            if(_enable == val) return;
            _enable = val;
            if(_playable.Equals(default)) return;
            if (_enable)
            {
                OnEnable(_playable);
            }
            else
            {
                OnDisable(_playable);
            }
        }

        protected virtual void OnEnable(Playable playable)
        {
            playable.Play();
            // for (int i = 0; i < _playable.GetInputCount(); i++)
            // {
            //     var playable = _playable.GetInput(i);
            //     playable.Play();
            // }
        }

        protected virtual void OnDisable(Playable playable)
        {
            playable.Pause();
            // for (int i = 0; i < _playable.GetInputCount(); i++)
            // {
            //     var playable = _playable.GetInput(i);
            //     playable.Pause();
            // }
        }

        // public override void PrepareFrame(Playable playable, FrameData info)
        // {
        //     if(!enable) return;
        // }
        
    }
}