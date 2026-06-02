using System.Collections;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private string _remotePath = "http://localhost:8000/StreamingAssets/";
        private RemoteAssetIndexer _remoteBundleIndexer;

        public static bool simulateRemoteBundleInEditor
        {
            get => RemoteAssetIndexer.simulateRemoteBundleInEditor;
            set => RemoteAssetIndexer.simulateRemoteBundleInEditor = value;
        }

        private IEnumerator InitializeRemoteBundleManifest()
        {
            _remoteBundleIndexer = new RemoteAssetIndexer(_remotePath);
            yield return _remoteBundleIndexer.Initialize(OnRemoteBundleDownloadStarted, OnRemoteBundleDownloadProgress);
        }

        private void OnRemoteBundleDownloadStarted()
        {
            initState = AssetInitState.DownloadTheUpdateFile;
            initProcess = 0f;
        }

        private void OnRemoteBundleDownloadProgress(float progress)
        {
            initProcess = progress;
        }
    }
}