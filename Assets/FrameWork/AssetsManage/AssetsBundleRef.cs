using System.Collections;
using LinkFrameWork.MonoInstance;
using UnityEngine;

namespace LinkFrameWork.AssetsManage
{
    public class AssetsBundleRef
    {
        public AssetBundle Bundle;
        public int RefCount = 0;
        public bool AutoDispose = false;
        public bool Alive = true;
        private Coroutine _unloadCoroutine;

        public void DeRef()
        {
            RefCount -= 1;
            if (RefCount <= AssetsBundleManager.DisposeRefLine)
            {
                WaitToUnloadBundle();
            }
        }

        public void AddRef()
        {
            Alive = true;
            if (_unloadCoroutine != null)
            {
                ApplicationManager.Instance.StopCoroutine(_unloadCoroutine);
                _unloadCoroutine = null;
            }

            if (RefCount <= AssetsBundleManager.DisposeRefLine)
            {
                RefCount = 1;
            }
            else
            {
                RefCount += 1;
            }
        }

        public void ForceUnload()
        {
            RefCount = AssetsBundleManager.DisposeRefLine - 1;
            AutoDispose = true;
            WaitToUnloadBundle();
        }

        public void WaitToUnloadBundle()
        {
            if (!Alive || RefCount > AssetsBundleManager.DisposeRefLine || !AutoDispose)
                return;
            //  启动计时器
            if (ApplicationManager.IsExit)
                _unloadCoroutine = ApplicationManager.Instance.StartCoroutine(WaitToUnloadHandle());
        }

        private IEnumerator WaitToUnloadHandle()
        {
            yield return new WaitForSecondsRealtime(180f);
            Alive = false;
            AssetsBundleManager.Instance.UnloadAssetsBundle(Bundle.name);
            yield return null;
        }
    }
}