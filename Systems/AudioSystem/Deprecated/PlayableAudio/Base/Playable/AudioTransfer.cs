using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public class AudioTransfer: LinkPlayableBehaviour
    {
        private Action<Playable> _onTransferCompleted;
        private AudioMixerPlayable _mixer;
        private Playable _curInput;
        private int _curInputPortIndex = -1;
        private Playable _targetInput;
        private int _targetIInputPortIndex = -1;
        private bool _inTransfer = false;
        private float _fadeoutTime;
        private float _fadeoutTimeout;
        private float _fadeinTime;
        private float _fadeinTimeout;
        private float _clip0Weight;
        private float _intervalTime;
        private float _intervalTimeout;
        private bool _isPlayInput1;

        public static ScriptPlayable<AudioTransfer> Create(PlayableGraph graph, Playable defaultInput,
            int inputPortIndex)
        {
            var playable = ScriptPlayable<AudioTransfer>.Create(graph);
            playable.GetBehaviour().Init(graph, playable, defaultInput, inputPortIndex);
            return playable;
        }

        private void Init(PlayableGraph graph, ScriptPlayable<AudioTransfer> selfPlayable, Playable defaultInput,
            int inputPortIndex)
        {
            _playableGraph = graph;
            _playable = selfPlayable;
            _playable.SetInputCount(1);
            _playable.SetOutputCount(1);
            _isPlayInput1 = false;
            // _mixer.ConnectInput(0, defaultInput, inputPortIndex);
            _curInput = defaultInput;
            _curInputPortIndex = inputPortIndex;
            // _playable.ConnectInput(0, _curInput, _curInputPortIndex, 1f);
            _mixer = AudioMixerPlayable.Create(_playableGraph, 2);
            _mixer.ConnectInput(0, _curInput, _curInputPortIndex, 1f);
            _playable.ConnectInput(0, _mixer, 0, 1f);
        }

        public void TransferTo(Playable source, int sourceOutputIndex, float fadeoutTime, float fadeinTime, float intervalTime = 0f, Action<Playable> onCompleted = null)
        {
            if (_curInputPortIndex == sourceOutputIndex && !_inTransfer && _curInput.Equals(source)) 
                return;

            _clip0Weight = 1;
            if (_inTransfer)
            {
                _curInput = _targetInput;
                _curInputPortIndex = _targetIInputPortIndex;
                if (_isPlayInput1)
                {
                    _clip0Weight = _mixer.GetInputWeight(0);
                    _mixer.DisconnectNTryPauseInput(1);
                }
                else
                {
                    _clip0Weight = _mixer.GetInputWeight(1);
                    _mixer.DisconnectNTryPauseInput(0);
                }
                _isPlayInput1 = !_isPlayInput1;
            }

            _onTransferCompleted = onCompleted;
            _inTransfer = true;
            _targetInput = source;
            _targetIInputPortIndex = sourceOutputIndex;
            if(!_targetInput.Equals(_curInput))
            {
                _targetInput.Pause();
            }
            _mixer.ConnectInput(_isPlayInput1? 0 : 1, _targetInput, _targetIInputPortIndex, 0f);
            var curPlayable = _mixer.GetInput(_isPlayInput1 ? 1 : 0);
            var isCurPlayableValid = !curPlayable.IsNull() && curPlayable.IsValid();
            
            _fadeoutTime = fadeoutTime > 0 ? fadeoutTime : Time.deltaTime;
            _fadeoutTimeout = isCurPlayableValid? 0f : _fadeoutTime;
            _fadeinTime = fadeinTime > 0 ? fadeinTime : Time.deltaTime;
            _fadeinTimeout = 0f;
            _intervalTime = intervalTime;
            _intervalTimeout = isCurPlayableValid? 0f : _intervalTime;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (!_inTransfer) return;
            TransferHandle(info.deltaTime);
        }

        private void TransferHandle(float dt)
        {
            var fadeoutProcess = Mathf.Clamp01(_fadeoutTimeout / _fadeoutTime);
            var fadeinProcess = Mathf.Clamp01(_fadeinTimeout / _fadeinTime);
            OnTransferring(fadeoutProcess, fadeinProcess);
            if(_fadeoutTimeout < _fadeoutTime)
                _fadeoutTimeout += dt;
            else if (_intervalTimeout < _intervalTime)
                _intervalTimeout += dt;
            else
                _fadeinTimeout += dt;
            if (_fadeoutTimeout + _fadeinTimeout + _intervalTimeout >= _fadeoutTime + _fadeinTime + _intervalTime)
            {
                OnTransferEnd();
            }
        }

        private void OnTransferring(float fadeoutProcess, float fadeinProcess)
        {
            if (_mixer.GetInputCount() < 2) return;
            var weight = (1f - fadeoutProcess) * _clip0Weight;
            _mixer.SetInputWeight(_isPlayInput1?1:0, weight);
            _mixer.SetInputWeight(_isPlayInput1?0:1, fadeinProcess);
            if(fadeinProcess > 0 && _targetInput.IsValid() && _targetInput.GetPlayState() == PlayState.Paused)
            {
                _targetInput.SetTime(0f);
                _targetInput.Play();
            }
        }

        private void OnTransferEnd()
        {
            _inTransfer = false;
            _curInput = _targetInput;
            _curInputPortIndex = _targetIInputPortIndex;
            _targetIInputPortIndex = -1;

            if (_isPlayInput1)
            {
                _isPlayInput1 = false;
                _mixer.DisconnectNTryPauseInput(1);
                _mixer.SetInputWeight(0, 1f);
            }
            else
            {
                _isPlayInput1 = true;
                _mixer.DisconnectNTryPauseInput(0);
                _mixer.SetInputWeight(1, 1f);
            }
            _onTransferCompleted?.Invoke(_playable);
            _onTransferCompleted = null;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            _mixer.Destroy();
            _onTransferCompleted = null;
        }
    }
}