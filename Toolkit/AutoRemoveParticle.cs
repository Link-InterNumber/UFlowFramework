using UnityEngine;

namespace PowerCellStudio
{
    public class AutoRemoveParticle : MonoBehaviour
    {
        public bool destroyOnEnd = true;

        private float _playTime;
        private float _playTimeCountDown;

        private void Awake()
        {
            var particles = gameObject.GetComponents<ParticleSystem>();
            var playTime = 0f;
            for (var i = 0; i < particles.Length; i++)
            {
                var parMain = particles[i].main;
                var time = (parMain.startDelayMultiplier + parMain.duration) 
                           / Mathf.Max(0.1f, parMain.simulationSpeed);
                playTime = Mathf.Max(time, playTime);
            }
            _playTime = playTime;
            _playTimeCountDown = _playTime;
        }

        private void OnEnable()
        {
            _playTimeCountDown = _playTime;
        }

        private void Update()
        {
            if (_playTime <= 0f || _playTimeCountDown <= 0) return;
            _playTimeCountDown -= Time.deltaTime;
            if (_playTimeCountDown <= 0)
            {
                if (destroyOnEnd) GameObject.Destroy(gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}