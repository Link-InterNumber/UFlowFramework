using System;
using UnityEngine;
using UnityEngine.Rendering;
#if Unity_6_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    public class GaussianBlurFeature : ScriptableRendererFeature
    {
        class GaussianPass : ScriptableRenderPass
        {
            private GaussBlur _gaussBlur;
            private Material _material;
            private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();
            private const string k_VerticalPassName = "GAUSSIAN_BLUR_VERTICAL";
            private const string k_HorizontalPassName = "GAUSSIAN_BLUR_HORIZONTAL";
            private RenderTextureDescriptor _targetTextureDesc;
            private class MainPassData
            {
                public Material material;
                public GaussBlur setting;
#if Unity_6_OR_NEWER
                public TextureHandle inputTexture;
#endif
            }


            // static readonly string k_RenderTag = "Render Gauss Blur Effects";
            // static readonly int k_RenderTagID = Shader.PropertyToID(k_RenderTag);
            static readonly string PassTag = "GaussianBlur";
            private const string k_BlurTextureName = "_BlurTexture";
            static readonly int Buffer0 = Shader.PropertyToID("Gauss_Buffer0");
            static readonly int Buffer1 = Shader.PropertyToID("Gauss_Buffer1");
            static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");

            public GaussianPass(RenderPassEvent evt)
            {
                renderPassEvent = evt;
                _targetTextureDesc = new RenderTextureDescriptor(Screen.width, Screen.height,
                    RenderTextureFormat.Default, 0);
            }
#if Unity_6_OR_NEWER
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (!cameraData.postProcessEnabled) return;
                var stack = VolumeManager.instance.stack;
                _gaussBlur = stack.GetComponent<GaussBlur>();
                if (_gaussBlur == null || !_gaussBlur.IsActive()) return;
                _material = _gaussBlur.gaussBlurMaterial.value;
                if (!_material)
                {
                    return;
                }
                TextureHandle source, destination;

                Debug.Assert(resourcesData.cameraColor.IsValid());
                
                source = resourcesData.activeColorTexture;
                
                int rtW = cameraData.camera.scaledPixelWidth / _gaussBlur.downSample.value;
                int rtH = cameraData.camera.scaledPixelHeight / _gaussBlur.downSample.value;
                _targetTextureDesc.width = rtW;
                _targetTextureDesc.height = rtH;
                _targetTextureDesc.depthBufferBits = 0;
                destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                    _targetTextureDesc, k_BlurTextureName, false);
                if (!source.IsValid() || !destination.IsValid())
                {
                    Debug.LogError("No Texture For Gaussian Blur");
                    return;
                }
                for (int i = 0; i < _gaussBlur.iterations.value; i++)
                {
                    _material.SetFloat(BlurSizeId, 1.0f + i * _gaussBlur.blurSpread.value);
                    // The AddBlitPass method adds a vertical blur render graph pass that blits from the source texture (camera color in this case) to the destination texture using the first shader pass (the shader pass is defined in the last parameter).
                    RenderGraphUtils.BlitMaterialParameters paraHorizontal = new(source, destination, _material, 0);
                    renderGraph.AddBlitPass(paraHorizontal, k_HorizontalPassName);
                    // The AddBlitPass method adds a horizontal blur render graph pass that blits from the texture written by the vertical blur pass to the camera color texture. The method uses the second shader pass.
                    RenderGraphUtils.BlitMaterialParameters paraVertical = new(destination, source, _material, 1);
                    renderGraph.AddBlitPass(paraVertical, k_VerticalPassName);
                }

                resourcesData.cameraColor = destination;
            }
#endif

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(
                    renderingData.cameraData.camera.scaledPixelWidth,
                    renderingData.cameraData.camera.scaledPixelHeight,
                    RenderTextureFormat.Default, 0);
                // cmd.GetTemporaryRT(k_RenderTagID, descriptor);
                ConfigureTarget(k_CameraTarget);
                ConfigureClear(ClearFlag.None, Color.black);
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!renderingData.cameraData.postProcessEnabled) return;
                var stack = VolumeManager.instance.stack;
                _gaussBlur = stack.GetComponent<GaussBlur>();
                if (_gaussBlur == null || !_gaussBlur.IsActive()) return;
                _material = _gaussBlur.gaussBlurMaterial.value;
                if (!_material)
                {
                    Debug.LogError("No Material");
                    return;
                }
                var cmd = CommandBufferPool.Get(PassTag);
                Render(cmd, ref renderingData); 
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                cmd.Release();
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // cmd.ReleaseTemporaryRT(k_RenderTagID);
            }

            void Render(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ref var cameraData = ref renderingData.cameraData;
                var source = cameraData.renderer.cameraColorTargetHandle;
                // int destination = k_RenderTagID;

                // cmd.SetGlobalTexture(MainTexId, source);
                if (!_material)
                {
                    // Debug.LogError("No Material");
                    // cmd.Blit(source, destination);
                    return;
                }

                int rtW = cameraData.camera.scaledPixelWidth / _gaussBlur.downSample.value;
                int rtH = cameraData.camera.scaledPixelHeight / _gaussBlur.downSample.value;
                cmd.GetTemporaryRT(Buffer1, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                cmd.GetTemporaryRT(Buffer0, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                cmd.Blit(source, Buffer0);
                for (int i = 0; i < _gaussBlur.iterations.value; i++)
                {
                    _material.SetFloat(BlurSizeId, 1.0f + i * _gaussBlur.blurSpread.value);
                    cmd.Blit(Buffer0, Buffer1, _material, 0);
                    cmd.Blit(Buffer1, Buffer0, _material, 1);
                }
                cmd.Blit(Buffer1, source);
                cmd.ReleaseTemporaryRT(Buffer0);
                cmd.ReleaseTemporaryRT(Buffer1);
            }
        }

        GaussianPass m_ScriptablePass;

        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new GaussianPass(RenderPassEvent.AfterRenderingPostProcessing);
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(m_ScriptablePass);
            }
        }
    }
}