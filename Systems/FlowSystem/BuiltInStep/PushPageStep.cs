using System;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
	public class PushPageStep<T> : FlowStepBase where T : UIBehaviour, IUIParent
	{
		private readonly object _data;
		private readonly PagePushMode _pushMode;
		private readonly bool _waitUntilTop;
		private readonly bool _closeOnExit;

		public PushPageStep(object data = null, PagePushMode pushMode = PagePushMode.CloseOther,
			bool waitUntilTop = false, bool closeOnExit = false, string stepName = null) : base(stepName)
		{
			_data = data;
			_pushMode = pushMode;
			_waitUntilTop = waitUntilTop;
			_closeOnExit = closeOnExit;
		}

		protected override void OnStart(IFlowContext context)
		{
			if (!ValidatePageType(context)) return;
			if (!UIManager.instance)
			{
				context?.FailFlow($"UIManager instance is null: {stepName}");
				Fail(context);
				return;
			}

			try
			{
				UIManager.instance.PushPage<T>(_data, _pushMode);
				if (!_waitUntilTop)
				{
					CompleteStep();
					return;
				}

				if (IsCurrentTopPage())
				{
					CompleteStep();
				}
			}
			catch (Exception ex)
			{
				context?.FailFlow($"PushPageStep failed: {stepName}, {ex.Message}");
				Fail(context);
			}
		}

		protected override void OnUpdate(IFlowContext context, float deltaTime)
		{
			if (!_waitUntilTop) return;
			if (IsCurrentTopPage())
			{
				CompleteStep();
			}
		}

		protected override void OnExit(IFlowContext context)
		{
			TryClosePage();
		}

		protected override void OnFail(IFlowContext context)
		{
			base.OnFail(context);
			TryClosePage();
		}

		private bool ValidatePageType(IFlowContext context)
		{
			if (typeof(UIBehaviour).IsAssignableFrom(typeof(T)) && typeof(IUIParent).IsAssignableFrom(typeof(T)))
			{
				return true;
			}

			context?.FailFlow($"PushPageStep requires a page type implementing UIBehaviour and IUIParent: {stepName}");
			Fail(context);
			return false;
		}

		private bool IsCurrentTopPage()
		{
			if (!UIManager.instance) return false;
			var current = UIManager.instance.currentPage;
			return current != null && typeof(T).IsInstanceOfType(current);
		}

		private void TryClosePage()
		{
			if (!_closeOnExit || !UIManager.instance) return;
			UIManager.instance.ClosePage<T>(true, null);
		}
	}
}
