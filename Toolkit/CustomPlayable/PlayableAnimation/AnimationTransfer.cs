using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public class AnimationTransfer : LinkPlayableBehaviour
    {
        private Action<Playable> _OnTransferCompleted;
        private AnimationMixerPlayable _mixer;
        private Playable _curInput;
        private int _curInputPortIndex = -1;
        private Playable _targetInput;
        private int _targetIInputPortIndex = -1;
        private bool _inTransfer = false;
        private float _transferTime;
        private float _transferTimeout;
        private float _clip0Weight;

        public static ScriptPlayable<AnimationTransfer> Create(PlayableGraph graph, Playable defaultInput,
            int inputPortIndex)
        {
            var playable = ScriptPlayable<AnimationTransfer>.Create(graph);
            playable.GetBehaviour().Init(graph, playable, defaultInput, inputPortIndex);
            return playable;
        }

        private void Init(PlayableGraph graph, ScriptPlayable<AnimationTransfer> selfPlayable, Playable defaultInput,
            int inputPortIndex)
        {
            _playableGraph = graph;
            _playable = selfPlayable;
            _playable.SetInputCount(1);
            _playable.SetOutputCount(1);
            // _mixer.ConnectInput(0, defaultInput, inputPortIndex);
            _curInput = defaultInput;
            _curInputPortIndex = inputPortIndex;
            _playable.ConnectInput(0, _curInput, _curInputPortIndex, 1f);
        }

        public void TransferTo(Playable source, int sourceOutputIndex, float transferTime, Action<Playable> onCompleted = null)
        {
            if (_curInputPortIndex == sourceOutputIndex && !_inTransfer && _curInput.Equals(source)) 
                return;
            if (transferTime == 0f)
            {
                if(_inTransfer) _mixer.Destroy();
                _inTransfer = false;
                _curInput = source;
                _curInput.SetTime(0f);
                _curInput.Play();
                _curInputPortIndex = sourceOutputIndex;
                _targetIInputPortIndex = -1;
                _playable.DisconnectInput(0);
                _playable.ConnectInput(0, _curInput, _curInputPortIndex, 1f);
                onCompleted?.Invoke(_playable);
                _OnTransferCompleted = null;
                return;
            }

            _clip0Weight = 1;
            if (!_inTransfer)
            {
                _mixer = AnimationMixerPlayable.Create(_playableGraph, 2);
                _playable.DisconnectInput(0);
                _mixer.ConnectInput(0, _curInput, _curInputPortIndex, 1f);
                _playable.ConnectInput(0, _mixer, 0, 1f);
            }
            else
            {
                _curInput = _targetInput;
                _curInputPortIndex = _targetIInputPortIndex;
                _clip0Weight = _mixer.GetInputWeight(1);
                _mixer.DisconnectNTryPauseInput(0);
                _mixer.DisconnectInput(1);
                _mixer.ConnectInput(0, _curInput, _curInputPortIndex, _clip0Weight);
            }

            _OnTransferCompleted = onCompleted;
            _inTransfer = true;
            _targetInput = source;
            _targetIInputPortIndex = sourceOutputIndex;
            _targetInput.SetTime(0f);
            _targetInput.Play();
            _mixer.ConnectInput(1, _targetInput, _targetIInputPortIndex, 0);
            _transferTime = transferTime > 0 ? transferTime : Time.deltaTime;
            _transferTimeout = 0;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (!_inTransfer) return;
            TransferHandle(info.deltaTime);
        }

        private void TransferHandle(float dt)
        {
            var process = Mathf.Clamp01(_transferTimeout / _transferTime);
            OnTransferring(process);
            _transferTimeout += dt;
            if (_transferTimeout >= _transferTime)
            {
                OnTransferEnd();
            }
        }

        private void OnTransferring(float process)
        {
            if (_mixer.GetInputCount() < 2) return;
            var weight = (1f - process) * _clip0Weight;
            _mixer.SetInputWeight(0, weight);
            _mixer.SetInputWeight(1, 1f - weight);
        }

        private void OnTransferEnd()
        {
            _inTransfer = false;
            _curInput = _targetInput;
            _curInputPortIndex = _targetIInputPortIndex;
            _targetIInputPortIndex = -1;
            _mixer.DisconnectNTryPauseInput(0);
            _mixer.DisconnectInput(1);
            _playable.DisconnectInput(0);
            _playable.ConnectInput(0, _curInput, _curInputPortIndex, 1f);
            _mixer.Destroy();
            _OnTransferCompleted?.Invoke(_playable);
            _OnTransferCompleted = null;
        }
        
        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            if(!_mixer.IsNull() && _mixer.IsValid()) _mixer.Destroy();
            _OnTransferCompleted = null;
        }
    }
}