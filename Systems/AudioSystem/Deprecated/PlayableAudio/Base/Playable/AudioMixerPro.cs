using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public class AudioMixerPro: LinkPlayableBehaviour
    {
        private AudioMixerPlayable _mixer;
        public AudioMixerPlayable mixer => _mixer;

        private bool _fading;
        private float _fadeTime;
        private float _fadeTimeout;

        private List<float> _targetWeights;
        private List<float> _originWeights;
        
        private Action<Playable> _OnMixCompleted;

        public void Init(PlayableGraph graph, ScriptPlayable<AudioMixerPro> playable)
        {
            _playableGraph = graph;
            _playable = playable;
            _mixer = AudioMixerPlayable.Create(_playableGraph);
            _mixer.SetOutputCount(1);
            _targetWeights = new List<float>();
            _originWeights = new List<float>();
        }

        public static ScriptPlayable<AudioMixerPro> Create(PlayableGraph graph)
        {
            var playable = ScriptPlayable<AudioMixerPro>.Create(graph);
            playable.GetBehaviour().Init(graph, playable);
            playable.SetInputCount(1);
            playable.SetOutputCount(1);
            playable.ConnectInput(0, playable.GetBehaviour().mixer, 0, 1f);
            return playable;
        }

        public void Reset()
        {
            _playableGraph.DestroyPlayable(_mixer);
            _mixer = AudioMixerPlayable.Create(_playableGraph);
            _mixer.SetOutputCount(1);
            _targetWeights.Clear();
            _originWeights.Clear();
            _playable.ConnectInput(0, _mixer, 0, 1f);
        }

        private void StartFade(float[] targetWeights, float fadeTime, Action<Playable> onCompleted)
        {
            _originWeights.Clear();
            _targetWeights.Clear();
            var inputCount = _mixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                var originWeight = _mixer.GetInputWeight(i);
                _originWeights.Add(originWeight);
                if (targetWeights.Length - 1 < i)
                {
                    _targetWeights.Add(0f);
                    continue;
                }
                var targetWeight = targetWeights[i];
                _targetWeights.Add(targetWeights[i]);
                if(_mixer.GetInput(i).IsNull() || !_mixer.GetInput(i).IsValid()) continue;
                if(originWeight + targetWeight == 0)
                {
                    _mixer.GetInput(i).Pause();
                }
                else
                {
                    _mixer.GetInput(i).Play();
                }
            }
            _fadeTime = fadeTime;
            _fadeTimeout = 0f;
            _fading = true;
            _OnMixCompleted = onCompleted;
        }

        public int AddInputDirectly(Playable input, int inputOutputPort, float weight)
        {
            weight = Mathf.Clamp01(weight);
            _mixer.AddInput(input, inputOutputPort, weight);
            return _mixer.GetInputCount();
        }

        public void ConnectOrAddInput(int inputPortIndex, Playable input, int inputOutputPort, float weight)
        {
            if (inputPortIndex >= 0)
            {
                if (_mixer.GetInputCount() >= inputPortIndex + 1)
                {
                    var inputPlay = _mixer.GetInput(inputPortIndex);
                    if(!inputPlay.IsNull() && inputPlay.IsValid())
                        _mixer.DisconnectInput(inputPortIndex);
                }
                else
                {
                    _mixer.SetInputCount(inputPortIndex + 1);
                }
                _mixer.ConnectInput(inputPortIndex, input, inputOutputPort, weight);
            }
            else
            {
                _mixer.AddInput(input, inputOutputPort, weight);
            }
        }
        
        public int AddInput(Playable input, int inputOutputPort, float weight, float fadeTime = 0f, Action<Playable> onCompleted = null, int inputPortIndex = -1)
        {
            if (weight <= 0f)
            {
                weight = 0f;
                ConnectOrAddInput(inputPortIndex, input, inputOutputPort, weight);
                return _mixer.GetInputCount();
            }
            weight = Mathf.Clamp01(weight);
            List<float> weights = new List<float>();
            var inputCount = _mixer.GetInputCount();
            for (var i = 0; i < inputCount; i++)
            {
                weights.Add(_mixer.GetInputWeight(i));
            }

            var sum = weights.Sum();
            var less = weight + sum - 1f;
            if (fadeTime > 0f)
            {
                var targets = new float[inputCount + 1];
                for (var i = 0; i < inputCount; i++)
                {
                    var curWeight = weights[i];
                    targets[i] = sum > 0 ? curWeight - curWeight / sum * less : 0f;
                }
                targets[inputCount] = weight;
                ConnectOrAddInput(inputPortIndex, input, inputOutputPort, 0f);
                // _mixer.AddInput(input, inputOutputPort, 0f);
                StartFade(targets, fadeTime, onCompleted);
                return _mixer.GetInputCount();
            }
            for (var i = 0; i < inputCount; i++)
            {
                _mixer.SetInputWeight(i, sum> 0f ? weights[i] - weights[i] / sum * less : 0f);
            }
            // _mixer.AddInput(input, inputOutputPort, weight);
            ConnectOrAddInput(inputPortIndex, input, inputOutputPort, weight);
            return _mixer.GetInputCount();
        }

        public void SetWeightDirectly(int inputPortIndex, float weight)
        {
            var inputCount = _mixer.GetInputCount();
            if(inputCount-1 < inputPortIndex) return;
            weight = Mathf.Clamp01(weight);
            _mixer.SetInputWeight(inputPortIndex, weight);
        }

        public void TweenWeight(int inputPortIndex, float weight, float fadeTime = 0f, Action<Playable> onCompleted = null)
        {
            weight = Mathf.Clamp01(weight);
            List<float> weights = new List<float>();
            var inputCount = _mixer.GetInputCount();
            if(inputCount-1 < inputPortIndex) return;
            for (var i = 0; i < inputCount; i++)
            {
                if(inputPortIndex == i) continue;
                weights.Add(_mixer.GetInputWeight(i));
            }

            var sum = weights.Sum();
            var less = weight + sum - 1f;
            if (fadeTime > 0f)
            {
                var targets = new float[inputCount];
                for (var i = 0; i < inputCount; i++)
                {
                    if (i == inputPortIndex)
                    {
                        targets[i] = weight;
                        continue;
                    }
                    var curWeight = _mixer.GetInputWeight(i);
                    targets[i] = sum > 0 ? curWeight - curWeight / sum * less : 0f;
                }
                StartFade(targets, fadeTime, onCompleted);
                return;
            }
            
            for (var i = 0; i < inputCount; i++)
            {
                if(inputPortIndex == i) continue;
                var curWeight = _mixer.GetInputWeight(i);
                _mixer.SetInputWeight(i, sum > 0 ? curWeight - curWeight / sum * less : 0f);
            }
            _mixer.SetInputWeight(inputPortIndex, weight);
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            base.PrepareFrame(playable, info);
            if(!_fading) return;
            var process = Mathf.Clamp01(_fadeTimeout / _fadeTime);
            OnFading(process);
            _fadeTimeout += info.deltaTime;
            if (_fadeTimeout >= _fadeTime)
            {
                OnFadeEnd();
            }
        }

        private void OnFading(float process)
        {
            var inputCount = _mixer.GetInputCount();
            if(_originWeights.Count < inputCount || _targetWeights.Count < inputCount) return;
            for (int i = 0; i < inputCount; i++)
            {
                var weightValue = Mathf.Lerp(_originWeights[i], _targetWeights[i], process);
                _mixer.SetInputWeight(i, weightValue);
            }
        }
        
        private void OnFadeEnd()
        {
            _fading = false;
            var inputCount = _mixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                if(i > _targetWeights.Count -1) break;
                _mixer.SetWeightNTryPauseInput(i, _targetWeights[i]);
            }
            _OnMixCompleted?.Invoke(_playable);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            _mixer.Destroy();
            _targetWeights = null;
            _originWeights = null;
        }
    }
}