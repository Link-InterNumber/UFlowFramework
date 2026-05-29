using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio
{
	public enum LoadAssetsMode
	{
		Auto,
		Chain,
		BatchSync,
	}

	public class LoadAssetsStep : FlowStepBase
	{
		private readonly IAssetLoader _externalAssetLoader;
		private List<string> _assetAddresses;
        private List<string> _failedAssets;
		private readonly bool _failOnAnyError;
		private readonly LoadAssetsMode _loadMode;

		private int _finishedCount;
		private bool _hasError;
		private bool _started;
		private AssetBatchLoader _batchLoader;
		private ChainedLoadingLoader _chainedLoadingLoader;
		private bool _singleLoadCompleted;

		public LoadAssetsStep(IAssetLoader assetLoader, params string[] assetAddresses)
			: this(assetLoader, false, LoadAssetsMode.Auto, null, assetAddresses)
		{
		}

		public LoadAssetsStep(IAssetLoader assetLoader, bool failOnAnyError,
			LoadAssetsMode loadMode = LoadAssetsMode.Auto, string stepName = null,
			params string[] assetAddresses) : base(stepName)
		{
			_externalAssetLoader = assetLoader;
			_failOnAnyError = failOnAnyError;
			_loadMode = loadMode;
            _assetAddresses = ListPool<string>.Get();
            if (assetAddresses != null)
            {
                foreach (var address in assetAddresses)
                {
                    if (!string.IsNullOrWhiteSpace(address) && !_assetAddresses.Contains(address))
                    {
                        _assetAddresses.Add(address);
                    }
                }
            }
            _failedAssets = ListPool<string>.Get();
		}

		protected override void OnReset()
		{
			_finishedCount = 0;
			_hasError = false;
			_started = false;
			_singleLoadCompleted = false;
            _failedAssets.Clear();
		}

		protected override void OnStart(IFlowContext context)
		{
			if (_externalAssetLoader == null)
			{
				context?.FailFlow($"LoadAssetsStep loader is null: {stepName}");
				Fail(context);
				return;
			}

			if (_assetAddresses.Count == 0)
			{
				CompleteStep();
				return;
			}

			_started = true;
			if (_assetAddresses.Count == 1)
			{
				LoadSingle(_assetAddresses[0]);
				return;
			}

			switch (ResolveMode())
			{
				case LoadAssetsMode.Chain:
					LoadByChain();
					break;
				case LoadAssetsMode.BatchSync:
				default:
					LoadByBatchSync();
					break;
			}
		}

		protected override void OnUpdate(IFlowContext context, float deltaTime)
		{
			if (!_started) return;

			if (_assetAddresses.Count == 1)
			{
				if (_singleLoadCompleted)
				{
					FinalizeStep(context);
				}
				return;
			}

			if (_batchLoader != null)
			{
				if (_finishedCount >= _assetAddresses.Count)
				{
					FinalizeStep(context);
				}
				return;
			}

			if (_finishedCount < _assetAddresses.Count) return;
			FinalizeStep(context);
		}

		private void FinalizeStep(IFlowContext context)
		{
			if (_hasError && _failOnAnyError)
			{
				context?.FailFlow($"LoadAssetsStep failed: {stepName}");
				Fail(context);
				return;
			}

			CompleteStep();
		}

		private LoadAssetsMode ResolveMode()
		{
			if (_loadMode != LoadAssetsMode.Auto) return _loadMode;
			return LoadAssetsMode.BatchSync;
		}

		private void LoadSingle(string address)
		{
			_externalAssetLoader.LoadAsync<UnityEngine.Object>(address, _ =>
			{
				_singleLoadCompleted = true;
			}, () =>
			{
				_hasError = true;
				_singleLoadCompleted = true;
			});
		}

		private void LoadByChain()
		{
			_chainedLoadingLoader = new ChainedLoadingLoader(_externalAssetLoader);
			for (var i = 0; i < _assetAddresses.Count; i++)
			{
				var address = _assetAddresses[i];
				_chainedLoadingLoader.PushLoadTask<UnityEngine.Object>(address, _ =>
				{
					_finishedCount++;
				}, () =>
				{
					_hasError = true;
					_finishedCount++;
				});
			}
		}

		private void LoadByBatchSync()
		{
			_batchLoader = AssetUtils.SpawnBatchLoader(_externalAssetLoader, _assetAddresses, () =>
			{
				_finishedCount = _assetAddresses.Count;
			}, false);
		}

		protected override void OnExit(IFlowContext context)
		{
			DisposeHelpers();
		}

		protected override void OnFail(IFlowContext context)
		{
			base.OnFail(context);
			DisposeHelpers();
		}

		public override void Dispose()
		{
			DisposeHelpers();
            ListPool<string>.Release(_assetAddresses);
            _assetAddresses = null;
            ListPool<string>.Release(_failedAssets);
            _failedAssets = null;
			base.Dispose();
		}

		private void DisposeHelpers()
		{
			if (_batchLoader != null)
			{
				_batchLoader.Unprepare();
				_batchLoader = null;
			}

			if (_chainedLoadingLoader != null)
			{
				_chainedLoadingLoader.Cancel();
				_chainedLoadingLoader.Dispose();
				_chainedLoadingLoader = null;
			}
		}
	}
}
