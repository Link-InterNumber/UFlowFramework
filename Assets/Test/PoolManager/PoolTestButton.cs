using System;
using UnityEngine;
using UnityEngine.UI;

namespace Test.PoolManager
{
    public class PoolTestButton : MonoBehaviour
    {
        public GameObject Prefab;
        public Transform ParentNode;
        
        private void OnEnable()
        {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(Onclick);
        }

        private void Onclick()
        {
            if (!LinkFrameWork.MonoInstance.PoolManager.Instance.IsRegister<TestPool>())
            {
                LinkFrameWork.MonoInstance.PoolManager.Instance.Register<TestPool>(Prefab);
            }
            var node = LinkFrameWork.MonoInstance.PoolManager.Instance.Spawn<TestPool>();
            node.transform.SetParent(ParentNode);
        }

        private void OnDestroy()
        {
            var btn = GetComponent<Button>();
            btn.onClick.RemoveListener(Onclick);
        }
    }
}