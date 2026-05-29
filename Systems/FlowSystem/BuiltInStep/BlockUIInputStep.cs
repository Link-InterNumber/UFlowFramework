using System;

namespace PowerCellStudio
{
	public class BlockUIInputStep : FlowStepBase
	{
		private readonly Func<IFlowContext, bool> _canClose;
		private readonly float _realTime;
		private readonly bool _showWaiting;


		public BlockUIInputStep(bool showWaiting = false, string stepName = null) : base(stepName)
		{
			_showWaiting = showWaiting;
			_realTime = -1f;
		}

		public BlockUIInputStep(float realTime, bool showWaiting = true, string stepName = null) : base(stepName)
		{
			_realTime = realTime;
			_showWaiting = showWaiting;
		}

		public BlockUIInputStep(Func<IFlowContext, bool> canClose, bool showWaiting = true, string stepName = null)
			: base(stepName)
		{
			_canClose = canClose;
			_showWaiting = showWaiting;
			_realTime = -1f;
		}

		protected override void OnStart(IFlowContext context)
		{
			if (_canClose != null)
			{
				MaskWindow.Open(() => _canClose(context), _showWaiting);
				CompleteStep();
				return;
			}

			if (_realTime >= 0f)
			{
				MaskWindow.Open(_realTime, _showWaiting);
				CompleteStep();
				return;
			}

			// No auto-close rule: hold a single mask reference and release it on Exit/Fail/Dispose.
			UIManager.instance.OpenWindow<MaskWindow>();
			CompleteStep();
		}

		protected override void OnFail(IFlowContext context)
		{
			base.OnFail(context);
			TryCloseMask();
		}

		public override void Dispose()
		{
			TryCloseMask();
			base.Dispose();
		}

		public override void OnSceneFlowed(IFlowContext context)
		{
			TryCloseMask();
		}

		private void TryCloseMask()
		{
			if (UIManager.instance)
			{
				UIManager.instance.CloseWindow<MaskWindow>();
			}
		}
	}
}
