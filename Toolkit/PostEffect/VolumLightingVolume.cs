using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    public class VolumLightingVolume : VolumeComponent, IPostProcessComponent
    {
        public MaterialParameter volumLightingMaterial = new MaterialParameter(null);
        public MaterialParameter compositeMaterial = new MaterialParameter(null);
        // public ClampedFloatParameter lightIntensity = new ClampedFloatParameter(1f, 0f, 3f);
        // public FloatParameter stepSize = new FloatParameter(0.1f);
        // public FloatParameter maxDistance = new FloatParameter(1000);
        // public IntParameter maxStep = new IntParameter(200);
        public ClampedIntParameter downSample = new ClampedIntParameter(2, 1, 10);
        // public FloatParameter shadowPower = new FloatParameter(1f);
        public bool IsActive() => volumLightingMaterial.value != null && compositeMaterial.value != null;
        public bool IsTileCompatible() => false;
    }
}