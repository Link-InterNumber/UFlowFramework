using UnityEngine;

namespace PowerCellStudio
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isExist;

        public static bool isExist => _isExist;

        public static T instance => _instance;

        protected virtual void Awake()
        {
            if (!_instance)
            {
                _instance = this as T;
                ModuleLogger.Log<T>($"{typeof(T).Name} Spwaned, GameObject Name: {gameObject.name}.");
                _isExist = true;
            }
            else if (_instance.GetInstanceID() != GetInstanceID())
            {
                if(isExist)
                {
                    ModuleLogger.LogError<T>(
                        $"Delete redundant Singleton: {typeof(T).Name} \nGameObject Name: {gameObject.name}.");
                    Destroy(this);
                }
                else
                {
                    Destroy(_instance);
                    _instance = this as T;
                    ModuleLogger.Log<T>($"{typeof(T).Name} Spwaned, GameObject Name: {gameObject.name}.");
                    _isExist = true;
                }
            }
        }

        protected virtual void Deinit()
        {
            
        }

        protected virtual void OnDestroy()
        {
            Deinit();
            StopAllCoroutines();
            if (_instance.GetInstanceID() == GetInstanceID())
            {
                // ModuleLog<T>.Log($"{typeof(T).Name} Deinited, GameObject Name: {gameObject.name}.");
                // _instance = null;
                _isExist = false;
            }
        }
    }
}