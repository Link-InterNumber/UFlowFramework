using LinkFrameWork.PoolSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Test.PoolManager
{
    public class TestPool : PoolableGameObject<TestPool>
    {
        private void Awake()
        {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(Onclick);
        }
        
        private void OnDestroy()
        {
            var btn = GetComponent<Button>();
            btn.onClick.RemoveListener(Onclick);
        }

        private void Onclick()
        {
            DeSpawn();
        }

        public override void OnSpawn()
        {
            Debug.Log(gameObject.name+ " Spawned");
        }

        public override void OnDeSpawn()
        {
            Debug.Log(gameObject.name+ " DeSpawned");
        }
    }
}