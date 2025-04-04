using UnityEngine;
using UnityEngine.Rendering;
#if Unity_6_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    public class CustomFullScreenFeature : ScriptableRendererFeature
    {
        private Material passMaterial;
        private class PassData
        {
            internal Material effectMaterial;
            internal int passIndex;
            // internal bool requiresColor;
            // internal bool isBeforeTransparents;
            // public ProfilingSampler profilingSampler;
            // public RTHandle copiedColor;
        }

        class CustomFullScreenPass : ScriptableRenderPass
        {
            private CustomFullScreenPostEffect _customFullScreenPostEffect;
            private ProfilingSampler m_ProfilingSampler;
            private static readonly int m_BlitTextureShaderID = Shader.PropertyToID("_CustomFullTexture");
            static readonly string k_RenderTag = "Custom Full Texture Effects";
            static readonly int k_RenderTagID = Shader.PropertyToID(k_RenderTag);
            static readonly int MainTexId = Shader.PropertyToID("_MainTex");
            static readonly int Buffer0 = Shader.PropertyToID("CustomFullScreenPass_Buffer");
            
            
            // private PassData m_PassData;

            public void Setup(Material mat, int index, bool requiresColor, bool isBeforeTransparents, string featureName, in RenderingData renderingData)
            {
                m_ProfilingSampler ??= new ProfilingSampler(featureName);
#if Unity_6_OR_NEWER
                requiresIntermediateTexture = true;
#endif
                var colorCopyDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                colorCopyDescriptor.depthBufferBits = (int) DepthBits.None;
                // RenderingUtils.ReAllocateIfNeeded(ref m_CopiedColor, colorCopyDescriptor, name: "_FullscreenPassColorCopy");
                // m_PassData ??= new PassData();
            }

            public void Dispose()
            {
                // m_CopiedColor?.Release();
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if(!renderingData.cameraData.postProcessEnabled) return;
                var stack = VolumeManager.instance.stack;
                _customFullScreenPostEffect = stack.GetComponent<CustomFullScreenPostEffect>();
                if (_customFullScreenPostEffect == null || !_customFullScreenPostEffect.IsActive()) return;
                var cmd = CommandBufferPool.Get(k_RenderTag);
                ExecutePass(_customFullScreenPostEffect, cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                cmd.Release();
            }
            
            private static void ExecutePass(CustomFullScreenPostEffect passData, CommandBuffer cmdbf, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.isPreviewCamera)
                {
                    return;
                }
                var cmd = cmdbf;
                ref var cameraData = ref renderingData.cameraData;
                var source = cameraData.renderer.cameraColorTargetHandle;
                int rtW = cameraData.camera.scaledPixelWidth;
                int rtH = cameraData.camera.scaledPixelHeight;
                cmd.GetTemporaryRT(Buffer0, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                if (passData.effectMaterial_1 != null)
                {
                    Material passMaterial = passData.effectMaterial_1.value;
                    int passIndex = passData.passIndex_1.value;
                    cmd.Blit(source, Buffer0, passMaterial, passIndex);
                    cmd.Blit(Buffer0, source);
                }
                if (passData.effectMaterial_2 != null)
                {
                    Material passMaterial = passData.effectMaterial_2.value;
                    int passIndex = passData.passIndex_2.value;
                    cmd.Blit(source, Buffer0, passMaterial, passIndex);
                    cmd.Blit(Buffer0, source);
                }
                if (passData.effectMaterial_3 != null)
                {
                    Material passMaterial = passData.effectMaterial_3.value;
                    int passIndex = passData.passIndex_3.value;
                    cmd.Blit(source, Buffer0, passMaterial, passIndex);
                    cmd.Blit(Buffer0, source);
                }
                cmd.ReleaseTemporaryRT(Buffer0);
            }
            
            private class MainPassData
            {
                internal Material material;
                internal int passIndex;
#if Unity_6_OR_NEWER
                internal TextureHandle inputTexture;
#endif
            }
#if Unity_6_OR_NEWER
            private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
            {
                Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
            }
            
            private static void ExecuteMainPass(RasterCommandBuffer cmd, Material material, int passIndex)
            {
                cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Triangles, 3, 1);
            }
            
            static void ExecuteMaterialPass( MainPassData data, RasterGraphContext context )
            {
                data.material.SetTexture( MainTexId, data.inputTexture );
                Blitter.BlitTexture( context.cmd, data.inputTexture, new Vector4( 1, 1, 0, 0 ), data.material, data.passIndex );
            }

            void AddBlitPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, Material mat, string pass,
                int passIndex)
            {
                using var builder = renderGraph.AddRasterRenderPass( pass, out MainPassData passData );

                builder.UseTexture( source );
                passData.inputTexture = source;
                passData.passIndex = passIndex;
                passData.material = mat;
                builder.SetRenderAttachment( destination, 0 );
                builder.SetRenderFunc< MainPassData >( ExecuteMaterialPass );
                
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (!cameraData.postProcessEnabled) return;
                var stack = VolumeManager.instance.stack;
                _customFullScreenPostEffect = stack.GetComponent<CustomFullScreenPostEffect>();
                if (_customFullScreenPostEffect == null || !_customFullScreenPostEffect.IsActive()) return;
                if (resourcesData.isActiveTargetBackBuffer)
                {
                    Debug.LogError("No Active Target Back Buffer");
                    return;
                }
                Debug.Assert(resourcesData.cameraColor.IsValid());
                
                var source = resourcesData.activeColorTexture;
                
                var destinationDesc =  renderGraph.GetTextureDesc(source);
                destinationDesc.name = "CustomFullScreenPass";
                destinationDesc.clearBuffer = false;
                var destination = renderGraph.CreateTexture(destinationDesc);
                if (!source.IsValid() || !destination.IsValid())
                {
                    Debug.LogError("No Texture For CustomFullScreenPass");
                    return;
                }

                // if (_customFullScreenPostEffect.effectMaterial_1.value != null)
                // {
                //     AddBlitPass(renderGraph, source, destination, _customFullScreenPostEffect.effectMaterial_1.value, "CustomFullScreenPass", 0);
                // }

                if (_customFullScreenPostEffect.effectMaterial_1.value != null)
                {
                    // Blit(source, destination, _customFullScreenPostEffect.effectMaterial_1.value, 0)
                    RenderGraphUtils.BlitMaterialParameters paraHorizontal = new(source, destination,
                        _customFullScreenPostEffect.effectMaterial_1.value,
                        _customFullScreenPostEffect.passIndex_1.value);
                    renderGraph.AddBlitPass(paraHorizontal, "CustomFullScreenPass_0");
                }
                
                resourcesData.cameraColor = destination;
            }
#endif
        }

        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRendering;
        public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.Color;
        public int passIndex = 0;

        private CustomFullScreenPass fullScreenPass;
        private bool requiresColor = true;
        private bool injectedBeforeTransparents;
        
        public override void Create()
        {
            fullScreenPass = new CustomFullScreenPass();
            fullScreenPass.renderPassEvent = injectionPoint;

            // This copy of requirements is used as a parameter to configure input in order to avoid copy color pass
            ScriptableRenderPassInput modifiedRequirements = requirements;

            requiresColor = (requirements & ScriptableRenderPassInput.Color) != 0;
            injectedBeforeTransparents = injectionPoint <= RenderPassEvent.BeforeRenderingTransparents;

            if (requiresColor && !injectedBeforeTransparents)
            {
                // Removing Color flag in order to avoid unnecessary CopyColor pass
                // Does not apply to before rendering transparents, due to how depth and color are being handled until
                // that injection point.
                modifiedRequirements ^= ScriptableRenderPassInput.Color;
            }
            fullScreenPass.ConfigureInput(modifiedRequirements);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderingData.cameraData.cameraType != CameraType.Game) return;
            fullScreenPass.Setup(passMaterial, passIndex, requiresColor, injectedBeforeTransparents, "FullScreenPassRendererFeature", renderingData);

            renderer.EnqueuePass(fullScreenPass);
        }
        
        protected override void Dispose(bool disposing)
        {
            fullScreenPass.Dispose();
        }
    }
}