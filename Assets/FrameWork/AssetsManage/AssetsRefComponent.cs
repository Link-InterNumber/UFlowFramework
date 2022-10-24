using System.Collections.Generic;
using LinkFrameWork.Define;
using UnityEngine;

namespace LinkFrameWork.AssetsManage
{
    public class AssetsRefComponent : MonoBehaviour
    {
        private Dictionary<AssetType, string> RefBundleName = null;

        public void ResetRef(string newName, AssetType type)
        {
            if (RefBundleName == null)
            {
                RefBundleName = new Dictionary<AssetType, string>();
                RefBundleName.Add(type, newName);
            }
            else
            {
                if (RefBundleName.TryGetValue(type, out var value))
                {
                    AssetsBundleManager.Instance.DeRefByBundleName(value);
                    RefBundleName[type] = newName;
                }
                else
                {
                    RefBundleName.Add(type, newName);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (RefBundleName == null) return;
            foreach (var (_, value) in RefBundleName)
            {
                AssetsBundleManager.Instance.DeRefByBundleName(value);
            }

            RefBundleName = null;
        }
    }
}