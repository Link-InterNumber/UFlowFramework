using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace PowerCellStudio
{
    public class VolumeLightingFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class LightingEventSetting
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
        }
        public LightingEventSetting settings = new LightingEventSetting();
        class VolumeLightingPass : ScriptableRenderPass
        {
            RenderTargetIdentifier currentTarget;
            VolumLightingVolume volumeLighting;
            Material volumeLightingMaterail;
            Material compositeMaterial;

            static readonly int MainTexId = Shader.PropertyToID("_MainTex");
            static readonly int TempTargetId = Shader.PropertyToID("_TempTargetVolumLighting");
            static readonly int MaxStepId = Shader.PropertyToID("_MaxStep");
            static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
            static readonly int StepSizeId = Shader.PropertyToID("_StepSize");
            static readonly int LightIntensityId = Shader.PropertyToID("_LightIntensity");
            static readonly int ShadowPowerId = Shader.PropertyToID("_ShadowPower");

            static readonly string k_RenderTag = "Render Volume Lighting Effects";

                
            public VolumeLightingPass(RenderPassEvent evt)
            {
                renderPassEvent = evt;
                var shader = Shader.Find("PostEffect/VolumeLighting");
                if (shader == null)
                {
                    Debug.LogError("PostEffect/VolumeLighting路径下无法找到着色器");
                    return;
                }
            }
                
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                currentTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!renderingData.cameraData.postProcessEnabled) return;
                var stac = VolumeManager.instance.stack;
                volumeLighting = stac.GetComponent<VolumLightingVolume>();
                if (volumeLighting == null)
                {
                    Debug.LogError("VolumLighting为空");
                    return;
                }
                if (!volumeLighting.IsActive())
                {
                    return;
                }
                volumeLightingMaterail = volumeLighting.volumLightingMaterial.value;
                if (volumeLightingMaterail == null)
                {
                    Debug.LogError("体积光材质为空");
                    return;
                }
                compositeMaterial = volumeLighting.compositeMaterial.value;
                if (compositeMaterial == null)
                {
                    Debug.LogError("体积光合成材质为空");
                    return;
                }

                var cmd = CommandBufferPool.Get(k_RenderTag);
                Render(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }

            private string _BaseTex = "_BaseTex";
            private string _BlendTex = "_BlendTex";
            void Render(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ref var cameraData = ref renderingData.cameraData;

                // 复制当前屏幕内容
                var colourText = RenderTexture.GetTemporary(cameraData.camera.scaledPixelWidth, cameraData.camera.scaledPixelHeight, 0, RenderTextureFormat.Default);
                cmd.Blit(currentTarget, colourText);

                // 降低计算分辨率
                var downSample = volumeLighting.downSample.value > 0 ? volumeLighting.downSample.value : 1;
                var w = cameraData.camera.scaledPixelWidth / downSample;
                var h = cameraData.camera.scaledPixelHeight / downSample;
                var downSampleTexture = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default);
                // 设置体积光材质参数
                volumeLightingMaterail.SetTexture(MainTexId, downSampleTexture);
                // volumeLightingMaterail.SetInt(MaxStepId, volumeLighting.maxStep.value);
                // volumeLightingMaterail.SetFloat(MaxDistanceId, volumeLighting.maxDistance.value);
                // volumeLightingMaterail.SetFloat(StepSizeId, volumeLighting.stepSize.value);
                // volumeLightingMaterail.SetFloat(LightIntensityId, volumeLighting.lightIntensity.value);
                // volumeLightingMaterail.SetFloat(ShadowPowerId, volumeLighting.shadowPower.value);
                // 渲染体积光
                int shaderPass = 0;
                var destinationTex = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default);
                cmd.Blit(downSampleTexture, destinationTex, volumeLightingMaterail, shaderPass);
                // 合成
                compositeMaterial.SetTexture(_BlendTex, destinationTex);
                compositeMaterial.SetTexture(_BaseTex, colourText);
                cmd.Blit(colourText, currentTarget, compositeMaterial);
                // 释放临时RT
                RenderTexture.ReleaseTemporary(colourText);
                RenderTexture.ReleaseTemporary(destinationTex);
                RenderTexture.ReleaseTemporary(downSampleTexture);
            }

        }

        private VolumeLightingPass volumeLightingPass;
        public override void Create()
        {
            volumeLightingPass = new VolumeLightingPass(settings.Event);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(volumeLightingPass);
        }


    }
}