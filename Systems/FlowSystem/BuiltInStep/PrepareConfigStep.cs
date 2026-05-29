using System;
using System.Linq;

namespace PowerCellStudio
{
    public class PrepareConfigStep : FlowStepBase
    {
        private readonly ConfigGroup _configGroup;
        private readonly bool _failFlowOnLoadFailed;

        private bool _completed;
        private AssetLoadStatus _finalStatus = AssetLoadStatus.Unload;

        public PrepareConfigStep(ConfigGroup configGroup, bool failFlowOnLoadFailed = true, string stepName = null)
            : base(stepName)
        {
            _configGroup = configGroup;
            _failFlowOnLoadFailed = failFlowOnLoadFailed;
        }

        public PrepareConfigStep(bool failFlowOnLoadFailed = true, string stepName = null,
            params IConfBaseCollections[] configs) : this(new ConfigGroup(configs), failFlowOnLoadFailed, stepName)
        {
        }

        protected override void OnReset()
        {
            _completed = false;
            _finalStatus = AssetLoadStatus.Unload;
            Unsubscribe();
        }

        protected override void OnStart(IFlowContext context)
        {
            if (_configGroup == null)
            {
                context?.FailFlow($"PrepareConfigStep configGroup is null: {stepName}");
                Fail(context);
                return;
            }

            try
            {
                Unsubscribe();
                _configGroup.onLoadCompleted += OnConfigLoadCompleted;
                _configGroup.LoadAll();
            }
            catch (Exception ex)
            {
                context?.FailFlow($"PrepareConfigStep exception: {stepName}, {ex.Message}");
                Fail(context);
            }
        }

        protected override void OnUpdate(IFlowContext context, float deltaTime)
        {
            if (!_completed) return;

            if (_finalStatus == AssetLoadStatus.Unload && _failFlowOnLoadFailed)
            {
                var failedConfigs = _configGroup?.failLoadConfigs;
                var failedConfigText = failedConfigs == null || failedConfigs.Length == 0
                    ? "unknown"
                    : string.Join(", ", failedConfigs.Where(c => !string.IsNullOrWhiteSpace(c)));
                context?.FailFlow($"PrepareConfigStep failed: {stepName}, configs: {failedConfigText}");
                Fail(context);
                return;
            }

            CompleteStep();
        }

        protected override void OnExit(IFlowContext context)
        {
            Unsubscribe();
        }

        protected override void OnFail(IFlowContext context)
        {
            base.OnFail(context);
            Unsubscribe();
        }

        public override void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        private void OnConfigLoadCompleted(AssetLoadStatus status)
        {
            _finalStatus = status;
            _completed = true;
        }

        private void Unsubscribe()
        {
            if (_configGroup == null) return;
            _configGroup.onLoadCompleted -= OnConfigLoadCompleted;
        }
    }
}