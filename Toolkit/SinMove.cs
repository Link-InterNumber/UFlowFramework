using UnityEngine;

namespace PowerCellStudio
{
    public class SinMove : MonoBehaviour
    {
        public float max;
        public float speed = 1;
        public bool isHorizontal;
        public bool updateInFixedUpdate;
        
        private Vector3 _originPos;
        private float _time;

        private void Awake()
        {
            _originPos = transform.localPosition;
        }

        private void OnEnable()
        {
            transform.localPosition = _originPos;
            _time = 0;
        }

        private void Update()
        {
            if (updateInFixedUpdate) return;
            UpdatePos(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!updateInFixedUpdate) return;
            UpdatePos(Time.fixedDeltaTime);
        }

        private void UpdatePos(float dt)
        {
            var sinValue = Mathf.Sin(_time) * max;
            if (isHorizontal)
            {
                var pos = _originPos + Vector3.right * sinValue;
                transform.localPosition = pos;
            }
            else
            {
                var pos = _originPos + Vector3.up * sinValue;
                transform.localPosition = pos;
            }

            if (float.MaxValue - dt * speed < _time)
            {
                _time = 0;
            }
            _time += dt * speed;
        }
    }
}