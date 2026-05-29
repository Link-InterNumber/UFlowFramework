using System;

namespace PowerCellStudio
{
	public class PlayBGMStep : FlowStepBase
	{
		private readonly string _clipRef;
		private readonly Func<IFlowContext, string> _clipResolver;
		private readonly MusicGroup _group;
		private readonly float _fadeOutTime;
		private readonly float _intervalTime;
		private readonly float _fadeInTime;

		public PlayBGMStep(string clipRef, MusicGroup group = MusicGroup.MainScene,
			float fadeOutTime = 0.5f, float intervalTime = 0.3f, float fadeInTime = 0.5f,
			string stepName = null) : base(stepName)
		{
			_clipRef = clipRef;
			_group = group;
			_fadeOutTime = fadeOutTime;
			_intervalTime = intervalTime;
			_fadeInTime = fadeInTime;
		}

		public PlayBGMStep(Func<IFlowContext, string> clipResolver, MusicGroup group = MusicGroup.MainScene,
			float fadeOutTime = 0.5f, float intervalTime = 0.3f, float fadeInTime = 0.5f,
			string stepName = null) : base(stepName)
		{
			_clipResolver = clipResolver;
			_group = group;
			_fadeOutTime = fadeOutTime;
			_intervalTime = intervalTime;
			_fadeInTime = fadeInTime;
		}

		protected override void OnStart(IFlowContext context)
		{
			if (!AudioManager.instance)
			{
				context?.FailFlow($"AudioManager instance is null: {stepName}");
				Fail(context);
				return;
			}

			var clipRef = _clipResolver?.Invoke(context) ?? _clipRef;
			if (!string.IsNullOrWhiteSpace(clipRef))
			{
				AudioManager.instance.PlayMusic(clipRef, _group, _fadeOutTime, _intervalTime, _fadeInTime);
			}
			CompleteStep();
		}
	}
}
