using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    [Serializable]
    public class CustomFullScreenPostEffectData 
    {
        public Material material;
        public int passIndex;
    }
    
    [Serializable, VolumeComponentMenu("Custom-Post-processing/CustomFullScreenPostEffect")]
    public class CustomFullScreenPostEffect : VolumeComponent, IPostProcessComponent
    {
        public MaterialParameter effectMaterial_1 = new MaterialParameter(null);
        public IntParameter passIndex_1 = new IntParameter(0);
        public MaterialParameter effectMaterial_2 = new MaterialParameter(null);
        public IntParameter passIndex_2 = new IntParameter(0);
        public MaterialParameter effectMaterial_3 = new MaterialParameter(null);
        public IntParameter passIndex_3 = new IntParameter(0);
        // public BoolParameter requiresColor = new BoolParameter(true);
        // internal BoolParameter isBeforeTransparents = new BoolParameter(false);
        // public ProfilingSampler profilingSampler;
        // public RTHandle copiedColor;

        public bool IsActive()
        {
            return effectMaterial_1.value != null || effectMaterial_2.value != null || effectMaterial_3.value != null;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}