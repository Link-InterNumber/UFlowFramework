using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    [Serializable, VolumeComponentMenu("Custom-Post-processing/GaussBlur")]
    public class GaussBlur : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter onEnable = new BoolParameter(true);
        public ClampedIntParameter iterations = new ClampedIntParameter(2, 1, 5);
        public ClampedFloatParameter blurSpread = new ClampedFloatParameter(0.6f, 0f, 2f);
        public ClampedIntParameter downSample = new ClampedIntParameter(4, 1, 5);
        public MaterialParameter gaussBlurMaterial = new MaterialParameter(null);

        public bool IsActive()
        {
            return gaussBlurMaterial.value != null && onEnable.value;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }

    [Serializable]
    public sealed class GaussianFilerModeParameter : VolumeParameter<FilterMode>
    {
        public GaussianFilerModeParameter(FilterMode value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }
}