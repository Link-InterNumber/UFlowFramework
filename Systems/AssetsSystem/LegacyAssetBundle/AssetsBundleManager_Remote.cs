using System.Collections;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private string _remotePath = "http://localhost:8000/StreamingAssets/";
        private RemoteBundleIndexer _remoteBundleIndexer;

        public static bool simulateRemoteBundleInEditor
        {
            get => RemoteBundleIndexer.simulateRemoteBundleInEditor;
            set => RemoteBundleIndexer.simulateRemoteBundleInEditor = value;
        }

        private IEnumerator InitializeRemoteBundleManifest()
        {
            _remoteBundleIndexer = new RemoteBundleIndexer(_remotePath, _bundleFoldName);
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