using UnityEngine;

namespace PowerCellStudio
{
    public abstract class TempMonoSingletonBase : MonoBehaviour
    {
        public abstract void Init(object data);
        protected abstract void OnDestroy();
    }
    
    public abstract class TempMonoSingleton<T> : TempMonoSingletonBase where T : TempMonoSingletonBase
    {
        private static T _instance;
        private static bool _isExist;

        public static bool isExist => _isExist;

        public static T instance => _instance;

        public override void Init(object data)
        {
            if(_instance)
            {
                Destroy(gameObject);
            }
            _instance = this as T;
            ModuleLog<T>.Log($"{typeof(T).Name} Spwaned, GameObject Name: {gameObject.name}.");
            _isExist = true;
        }

        public void Deinit()
        {
            if(gameObject)
                GameObject.Destroy(gameObject);
        }

        protected override void OnDestroy()
        {
            StopAllCoroutines();
            if (_instance && _instance.GetInstanceID() == GetInstanceID())
            {
                ModuleLog<T>.Log($"{typeof(T).Name} Deinited, GameObject Name: {gameObject.name}.");
                _instance = null;
                _isExist = false;
            }
        }

        public static void Create(object initData)
        {
            var go = new GameObject(typeof(T).Name);
            var tempSingleton = go.AddComponent<T>();
            tempSingleton.Init(initData);
        }
    }
}