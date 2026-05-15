using UnityEngine;

namespace PowerCellStudio
{
    public struct ShakeRequest
    {
        public ShakeUtils.ShakeType shakeType;

        public Transform target;

        public float duration;

        public float frequency;

        public Vector3 magnitude;

        public AnimationCurve curve;

        public bool isUnscaleTime;

        // public Vector3 origPos;
        //
        // public Quaternion origRota;

        public bool isCamera;

        public ShakeRequest(
        ShakeUtils.ShakeType shakeType,
        Transform target,
        float duration,
        float frequency,
        Vector3 magnitude,
        AnimationCurve curve,
        bool isUnscaleTime,
        bool isCamera)
        {
            this.shakeType = shakeType;
            this.target = target;
            this.duration = duration;
            this.frequency = frequency;
            this.magnitude = magnitude;
            this.curve = curve;
            this.isUnscaleTime = isUnscaleTime;
            this.isCamera = isCamera;

            // origPos = target.localPosition;
            // origRota = target.localRotation;
        }
    }
}