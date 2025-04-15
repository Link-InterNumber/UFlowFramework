using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    public static partial class PlayableUtils
    {
        public static void CreatePlayableOutput(this PlayableGraph graph, Animator target, Playable source,
            int sourceOutputPortIndex = 0, string name = null)
        {
            if (!target) return;
            if (!source.IsValid()) return;
            if (string.IsNullOrEmpty(name)) name = target.gameObject.name;
            var playerOutput = AnimationPlayableOutput.Create(graph, name, target);
            playerOutput.SetSourcePlayable(source, sourceOutputPortIndex);
        }
        
        public static void CreatePlayableOutput(this PlayableGraph graph, AudioSource target, Playable source,
            int sourceOutputPortIndex = 0, string name = null)
        {
            if (!target) return;
            if (!source.IsValid()) return;
            if (string.IsNullOrEmpty(name)) name = target.gameObject.name;
            var playerOutput = AudioPlayableOutput.Create(graph, name, target);
            playerOutput.SetSourcePlayable(source, sourceOutputPortIndex);
        }

        public static void DisconnectNTryPauseInput<T>(this T self, int inputPortIndex) 
            where T : struct, IPlayable
        {
            if (self.GetInputCount() - 1 < inputPortIndex) return;
            var inputPlayable = self.GetInput(inputPortIndex);
            if (inputPlayable.IsNull() || !inputPlayable.IsValid()) return;
            if (inputPlayable.GetOutputCount() == 1) inputPlayable.Pause();
            self.DisconnectInput(inputPortIndex);
        }

        public static void SetWeightNTryPauseInput<T>(this T self, int inputPortIndex, float weight)
            where T : struct, IPlayable
        {
            if (self.GetInputCount() - 1 < inputPortIndex) return;
            weight = Mathf.Clamp01(weight);
            var inputPlayable = self.GetInput(inputPortIndex);
            if (inputPlayable.IsNull() || !inputPlayable.IsValid() || !self.CanSetWeights()) return;
            if (inputPlayable.GetOutputCount() == 1 && weight == 0f) 
                inputPlayable.Pause();
            else if(weight > 0 && inputPlayable.GetPlayState() == PlayState.Paused) 
                inputPlayable.Play();
            self.SetInputWeight(inputPortIndex, weight);
        }
        
        public static void AutoConnectInput<T>(this T self, int inputPortIndex, Playable source, int sourceOutputPort, float weight)
            where T : struct, IPlayable
        {
            if (!self.CanChangeInputs()) return;
            if (inputPortIndex >= 0)
            {
                if (self.GetInputCount() >= inputPortIndex + 1)
                {
                    var inputPlay = self.GetInput(inputPortIndex);
                    if(!inputPlay.IsNull() && inputPlay.IsValid())
                        self.DisconnectInput(inputPortIndex);
                }
                else
                {
                    self.SetInputCount(inputPortIndex + 1);
                }
                self.ConnectInput(inputPortIndex, source, sourceOutputPort, weight);
            }
            else
            {
                self.AddInput(source, sourceOutputPort, weight);
            }
        }

        public static void DisconnectAllInput<T>(this T self) where T : struct, IPlayable
        {
            var loopCount = self.GetInputCount();
            if(loopCount == 0) return;
            for (int i = 0; i < loopCount; i++)
            {
                var input = self.GetInput(i);
                if(input.IsNull()) continue;
                self.DisconnectNTryPauseInput(i);
            }
        }
    }
}
