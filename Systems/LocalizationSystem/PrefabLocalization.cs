using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PowerCellStudio
{
    public class PrefabLocalization :AssetLocalizationSwitch
    {
        protected override void BeforeLoaded()
        {
        }
        
        protected override void OnLoaded(AsyncOperationHandle<Object> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded) return;
            var prefab = handle.Result as GameObject;
            if (!prefab) return;
            var oldGo = transform.GetChild(0);
            oldGo.SetParent(null);
            GameObject.Destroy(oldGo.gameObject);
            
            var go = GameObject.Instantiate(prefab, transform);
            var goTr = go.transform;
            
            goTr.localScale = Vector3.one;
            goTr.rotation = Quaternion.identity;
            goTr.localPosition = Vector3.zero;
            goTr.SetSiblingIndex(0);
        }


    }
}