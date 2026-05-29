using System;

namespace PowerCellStudio
{
	public class LoadSceneStep : FlowStepBase
	{
		private readonly string _sceneName;
		private readonly bool _unloadOtherScene;
		private readonly bool _waitUntilLoaded;
		private readonly bool _failFlowOnLoadFailed;

		private bool _loadCompleted;
		private bool _loadFailed;

		public LoadSceneStep(string sceneName, bool unloadOtherScene = false,
			bool waitUntilLoaded = true, bool failFlowOnLoadFailed = true,
			string stepName = null) : base(stepName)
		{
			_sceneName = sceneName;
			_unloadOtherScene = unloadOtherScene;
			_waitUntilLoaded = waitUntilLoaded;
			_failFlowOnLoadFailed = failFlowOnLoadFailed;
		}

		protected override void OnReset()
		{
			_loadCompleted = false;
			_loadFailed = false;
		}

		protected override void OnStart(IFlowContext context)
		{
			if (string.IsNullOrWhiteSpace(_sceneName))
			{
				context?.FailFlow($"LoadSceneStep scene name is empty: {stepName}");
				Fail(context);
				return;
			}

			try
			{
				AssetUtils.LoadScene(_sceneName, () =>
				{
					_loadCompleted = true;
				}, () =>
				{
					_loadFailed = true;
					_loadCompleted = true;
				}, _unloadOtherScene);

				if (!_waitUntilLoaded)
				{
					CompleteStep();
				}
			}
			catch (Exception ex)
			{
				context?.FailFlow($"LoadSceneStep exception: {stepName}, {ex.Message}");
				Fail(context);
			}
		}

		protected override void OnUpdate(IFlowContext context, float deltaTime)
		{
			if (!_waitUntilLoaded) return;
			if (!_loadCompleted) return;

			if (_loadFailed && _failFlowOnLoadFailed)
			{
				context?.FailFlow($"Load scene failed: {_sceneName}");
				Fail(context);
				return;
			}

			CompleteStep();
		}
	}
}