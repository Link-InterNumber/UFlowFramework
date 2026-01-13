using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public class AudioParallelMixer: LinkPlayableBehaviour
    {
        private AudioMixerPlayable _mixer;
        public AudioMixerPlayable mixer => _mixer;

        public bool controlGraph = false;

        // private float _fadeinTime;
        // private float _curAverageWeight;
        private List<AudioClipPlayable> _inputPortRecord;
        public bool destroyOnDisconnect = true;
        private int _newestInput;
        public float backgroundScale = 1f;

        public void Init(PlayableGraph graph, ScriptPlayable<AudioParallelMixer> playable)
        {
            _playableGraph = graph;
            _playable = playable;
            _mixer = AudioMixerPlayable.Create(_playableGraph);
            _inputPortRecord = new List<AudioClipPlayable>();
            _mixer.SetOutputCount(1);
            _mixer.SetInputCount(1);
        }

        public static ScriptPlayable<AudioParallelMixer> Create(PlayableGraph graph)
        {
            var playable = ScriptPlayable<AudioParallelMixer>.Create(graph);
            playable.GetBehaviour().Init(graph, playable);
            playable.SetInputCount(1);
            playable.SetOutputCount(1);
            playable.ConnectInput(0, playable.GetBehaviour().mixer, 0, 1f);
            return playable;
        }

        public int AddClip(AudioClipPlayable source, int sourceOutputIndex)
        {
            if (source.IsNull() || !source.IsValid()) return -1;
            var inputIndex = _inputPortRecord.Count == 0 ? 0 : _mixer.GetInputCount();
            for (var index = 0; index < _inputPortRecord.Count; index++)
            {
                var audio = _inputPortRecord[index];
                var has = !audio.IsNull() && audio.IsValid();
                if (has) continue;
                inputIndex = index;
                break;
            }

            if (inputIndex >= _mixer.GetInputCount())
            {
                _mixer.SetInputCount(inputIndex + 1);
            }
            while (inputIndex >= _inputPortRecord.Count)
            {
                _inputPortRecord.Add(default);
            }
            _inputPortRecord[inputIndex] = source;
            _mixer.ConnectInput(inputIndex, source, sourceOutputIndex, 1f);
            SetInputsWeight();
            if(_playable.GetPlayState() == PlayState.Paused) _playable.Play();
            if(controlGraph && !_playableGraph.IsPlaying()) _playableGraph.Play();
            return inputIndex;
        }

        public void RemoveInput(int inputPortIndex)
        {
            if (_mixer.GetInputCount() <= inputPortIndex || inputPortIndex <= 0) return;
            var playable = _mixer.GetInput(inputPortIndex);
            if (playable.IsNull() || !playable.IsValid()) return;
            _mixer.DisconnectNTryPauseInput(inputPortIndex);
            _inputPortRecord[inputPortIndex] = default;
            if (destroyOnDisconnect) playable.Destroy();
            // SetInputsWeight();
            StopWhenNoInput();
        }
        
        public void RemoveInput(string clipName)
        {
            var removeIndex = -1;
            for (var i = 0; i < _inputPortRecord.Count; i++)
            {
                var clipPlayable = _inputPortRecord[i];
                if(!clipPlayable.IsValid() || clipPlayable.GetClip().name != clipName) continue;
                removeIndex = i;
                break;
            }
            RemoveInput(removeIndex);
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            base.PrepareFrame(playable, info);
            for (var index = 0; index < _inputPortRecord.Count; index++)
            {
                var audio = _inputPortRecord[index];
                var empty = audio.IsNull() && !audio.IsValid();
                if (empty) continue;
                if ((!audio.IsDone() || audio.GetLooped()) && audio.GetPlayState() != PlayState.Paused) continue;
                _mixer.DisconnectNTryPauseInput(index);
                _inputPortRecord[index] = default;
                if (destroyOnDisconnect) audio.Destroy();
                StopWhenNoInput();
            }
        }

        public void RemoveAll()
        {
            var count = _mixer.GetInputCount();
            for (int i = 0; i < count; i++)
            {
                RemoveInput(i);
            }
        }
 
        public bool IsPlayEnd(int inputPortIndex)
        {
            if (_mixer.GetInputCount() <= inputPortIndex) return false;
            var playable = _mixer.GetInput(inputPortIndex);
            if (playable.IsNull() || !playable.IsValid()) return true;
            return playable.IsDone() || playable.GetPlayState() == PlayState.Paused;
        }

        private void StopWhenNoInput()
        {
            var has = _inputPortRecord.Any(o => !o.IsNull() && o.IsValid());
            if(has) return;
            _playable.Pause();
            _playable.SetTime(0f);
            if(controlGraph) _playableGraph.Stop();
        }

        private void SetInputsWeight()
        {
            var count = _inputPortRecord.Count(o => !o.IsNull() && o.IsValid());
            if (count <= 0) return;
            var scaledWeight = backgroundScale;
            for (var i = 0; i < _inputPortRecord.Count; i++)
            {
                var keyValuePair = _inputPortRecord[i];
                var has = !keyValuePair.IsNull() && keyValuePair.IsValid();
                if (!has || i == _newestInput) continue;
                if (_mixer.GetInputCount() <= i) continue;
                _mixer.SetInputWeight(i, scaledWeight);
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            _mixer.Destroy();
            _inputPortRecord = null;
        }
    }
}