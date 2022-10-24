using UnityEngine;

namespace LinkFrameWork.DesignPatterns
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static bool IsExit => _instance != null;

        public static T Instance
        {
            get
            {
                if (!_instance && Application.isPlaying)
                {
                    _instance = FindObjectOfType<T>();
                    if (!_instance)
                    {
                        var go = new GameObject($"Singleton_{typeof(T).Name}");
                        _instance = go.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (!_instance)
            {
                _instance = this as T;
            }
            else if (_instance.GetInstanceID() != GetInstanceID())
            {
                Debug.LogError($"Delete redundant Singleton: {typeof(T).Name} \nGameObject Name: {gameObject.name}");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            StopAllCoroutines();
            if (_instance && _instance.GetInstanceID() == GetInstanceID())
                _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            if (_instance)
            {
                _instance = null;
            }
        }
    }
}