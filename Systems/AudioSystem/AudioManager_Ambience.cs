namespace PowerCellStudio
{
    public partial class AudioManager
    {
        private IBgmPlayer _ambiencePlayer;

        public void PlayAmbience(string clipRef, bool isLoop = true,
            float fadeoutTime = 1f, float intervalTime = 1f, float fadeinTime = 1f)
        {
            if(string.IsNullOrEmpty(clipRef)) return;
            // var pipelineId = (int)AudioSourceType.Ambience * 1000 + (int)group;
            // var ambiencePipeline = FindPipeline(pipelineId, _masterPipeline);
            // if (ambiencePipeline == null)
            // {
            //     ambiencePipeline = new AudioPipeline();
            //     ambiencePipeline.Init(pipelineId, new SampleAudioPipelineBehavior(ambiencePipeline));
            //     ambiencePipeline.parent = GetPipeline(AudioSourceType.Ambience);
            // }
            var request = new AudioRequest(clipRef, (int)AudioSourceType.Ambience, isLoop)
            {
                fadeIn = fadeinTime,
                fadeOut = fadeoutTime,
                delay = intervalTime
            };
            PushRequest(request);
        }
    }
}