using System;

namespace PowerCellStudio
{
	public class OpenWindowStep<T> : FlowStepBase where T : class, IUIChild
	{
		private readonly object _data;
		private readonly bool _waitUntilOpened;
		private readonly bool _closeOnExit;

		public OpenWindowStep(object data = null, bool waitUntilOpened = false,
			bool closeOnExit = false, string stepName = null) : base(stepName)
		{
			_data = data;
			_waitUntilOpened = waitUntilOpened;
			_closeOnExit = closeOnExit;
		}

		protected override void OnStart(IFlowContext context)
		{
			if (!ValidateWindowType(context)) return;
			if (!UIManager.instance)
			{
				context?.FailFlow($"UIManager instance is null: {stepName}");
				Fail(context);
				return;
			}

			try
			{
				UIManager.instance.OpenWindow<T>(_data);
				if (!_waitUntilOpened)
				{
					CompleteStep();
					return;
				}

				if (IsWindowOpened())
				{
					CompleteStep();
				}
			}
			catch (Exception ex)
			{
				context?.FailFlow($"OpenWindowStep failed: {stepName}, {ex.Message}");
				Fail(context);
			}
		}

		protected override void OnUpdate(IFlowContext context, float deltaTime)
		{
			if (!_waitUntilOpened) return;
			if (IsWindowOpened())
			{
				CompleteStep();
			}
		}

		protected override void OnExit(IFlowContext context)
		{
			TryCloseWindow();
		}

		protected override void OnFail(IFlowContext context)
		{
			base.OnFail(context);
			TryCloseWindow();
		}

		private bool ValidateWindowType(IFlowContext context)
		{
			if (typeof(IUIChild).IsAssignableFrom(typeof(T))) return true;
			context?.FailFlow($"OpenWindowStep requires a type implementing IUIChild: {stepName}");
			Fail(context);
			return false;
		}

		private bool IsWindowOpened()
		{
			if (!UIManager.instance) return false;
			var current = UIManager.instance.currentPage;
            if (current != null ) return false;
            return current.GetOpenedUI<T>() != null;
		}

		private void TryCloseWindow()
		{
			if (!_closeOnExit || !UIManager.instance) return;
			UIManager.instance.CloseWindow<T>(null, false);
		}
	}
}
