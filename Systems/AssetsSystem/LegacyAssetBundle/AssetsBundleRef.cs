using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    public class AssetsBundleRef
    {
        public AssetBundle Bundle => _bundle;
        public int RefCount => _refCount;
        
        private AssetBundle _bundle;
        private int _refCount = 0;
        // public bool AutoDispose = false;
        public bool Alive = true;
        private Coroutine _unloadCoroutine;
        private AssetsBundleManager _assetsBundleManager;
        
        public AssetsBundleRef(AssetBundle bundle, AssetsBundleManager assetsBundleManager)
        {
            _bundle = bundle;
            _refCount = 0;
            _assetsBundleManager = assetsBundleManager;
        }

        public void DeRef()
        {
            _refCount -= 1;
            if (RefCount <= AssetsBundleManager.disposeRefLine)
            {
                WaitToUnloadBundle();
            }
        }

        public void Restore()
        {
            Alive = true;
            if (_unloadCoroutine != null)
            {
                ApplicationManager.instance.StopCoroutine(_unloadCoroutine);
                _unloadCoroutine = null;
            }
            if (RefCount <= AssetsBundleManager.disposeRefLine)
            {
                _refCount = 0;
            }
        }

        public void AddRef()
        {
            Alive = true;
            if (_unloadCoroutine != null)
            {
                ApplicationManager.instance.StopCoroutine(_unloadCoroutine);
                _unloadCoroutine = null;
            }

            if (RefCount <= AssetsBundleManager.disposeRefLine)
            {
                _refCount = 1;
            }
            else
            {
                _refCount += 1;
            }
        }

        public void ForceUnload()
        {
            _refCount = AssetsBundleManager.disposeRefLine - 1;
            // AutoDispose = true;
            WaitToUnloadBundle();
        }

        public void WaitToUnloadBundle()
        {
            if (!Alive || _refCount > AssetsBundleManager.disposeRefLine || _unloadCoroutine != null)
                return;
            //  启动计时器
            if (ApplicationManager.isExist && AssetsBundleManager.delayUnloadDuration > 0)
                _unloadCoroutine = ApplicationManager.instance.StartCoroutine(WaitToUnloadHandle());
            else
            {
                Alive = false;
                _assetsBundleManager?.UnloadAssetsBundle(this);
            }
        }

        private IEnumerator WaitToUnloadHandle()
        {
            yield return new WaitForSecondsRealtime(AssetsBundleManager.delayUnloadDuration);
            Alive = false;
            _assetsBundleManager?.UnloadAssetsBundle(this);
        }
    }
}