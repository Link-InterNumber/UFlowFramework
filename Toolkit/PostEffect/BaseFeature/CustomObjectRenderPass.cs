using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
#if Unity_6_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    public class CustomObjectRenderPass : ScriptableRenderPass, IDisposable
    {
#if Unity_6_OR_NEWER
        RenderQueueType renderQueueType;
#endif
        FilteringSettings m_FilteringSettings;
        // RenderObjects.CustomCameraSettings m_CameraSettings;
        // private RTHandle m_OutlineMaskRT;

        /// <summary>
        /// The override material to use.
        /// </summary>
        public Material overrideMaterial { get; set; }

        /// <summary>
        /// The pass index to use with the override material.
        /// </summary>
        public int overrideMaterialPassIndex { get; set; }

        /// <summary>
        /// The override shader to use.
        /// </summary>
        public Shader overrideShader { get; set; }

        /// <summary>
        /// The pass index to use with the override shader.
        /// </summary>
        public int overrideShaderPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        private PassData m_PassData;

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock[0].mask |= RenderStateMask.Depth;
            m_RenderStateBlock[0].depthState = new DepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets up the stencil settings for the pass.
        /// </summary>
        /// <param name="reference">The stencil reference value.</param>
        /// <param name="compareFunction">The comparison function to use.</param>
        /// <param name="passOp">The stencil operation to use when the stencil test passes.</param>
        /// <param name="failOp">The stencil operation to use when the stencil test fails.</param>
        /// <param name="zFailOp">The stencil operation to use when the stencil test fails because of depth.</param>
        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp,
            StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock[0].mask |= RenderStateMask.Stencil;
            m_RenderStateBlock[0].stencilReference = reference;
            m_RenderStateBlock[0].stencilState = stencilState;
        }

        RenderStateBlock[] m_RenderStateBlock;
        ShaderTagId[] m_ShaderTag = new []{ShaderTagId.none};

        /// <summary>
        /// The constructor for render objects pass.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="renderPassEvent">Controls when the render pass executes.</param>
        /// <param name="shaderTags">List of shader tags to render with.</param>
        /// <param name="renderQueueType">The queue type for the objects to render.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        public CustomObjectRenderPass(CustomObjectFeature.CustomRenderObjectsSettings settings)
        {
            profilingSampler = new ProfilingSampler(settings.passTag);
#if Unity_6_OR_NEWER
            Init(settings.Event, renderQueueType, settings);
#else
            Init(settings.Event, settings);
#endif
        }

#if Unity_6_OR_NEWER
        private void Init(RenderPassEvent injectPoint, RenderQueueType renderQueueType,
            CustomObjectFeature.CustomRenderObjectsSettings settings)
#else
        private void Init(RenderPassEvent injectPoint,
            CustomObjectFeature.CustomRenderObjectsSettings settings)
#endif
        {
            m_PassData = new PassData();

            this.renderPassEvent = injectPoint;
#if Unity_6_OR_NEWER
            this.renderQueueType = renderQueueType;
#endif
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            this.overrideShader = null;
            this.overrideShaderPassIndex = 0;
            RenderQueueRange renderQueueRange = RenderQueueRange.all;
            switch (settings.filterSettings.renderQueueMode)
            {
                case CustomObjectFeature.RenderQueueMode.Opaque:
                    renderQueueRange = RenderQueueRange.opaque;
                    break;
                case CustomObjectFeature.RenderQueueMode.Transparent:
                    renderQueueRange = RenderQueueRange.transparent;
                    break;
                case CustomObjectFeature.RenderQueueMode.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (settings.filterSettings.maskMode == CustomObjectFeature.MaskMode.GameObjectLayer)
            {
                m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask : settings.filterSettings.LayerMask);
            }
            else
            {
                m_FilteringSettings = new FilteringSettings(renderQueueRange, renderingLayerMask : settings.filterSettings.renderingLayerMask);
            }

            if (settings.filterSettings.PassNames != null && settings.filterSettings.PassNames.Length > 0)
            {
                foreach (var tag in settings.filterSettings.PassNames)
                    m_ShaderTagIdList.Add(new ShaderTagId(tag));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                m_ShaderTagIdList.Add(new ShaderTagId("Universal2D"));
            }

            m_RenderStateBlock = new[] {new RenderStateBlock(RenderStateMask.Nothing)};
            // m_CameraSettings = cameraSettings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            // RenderingUtils.ReAllocateIfNeeded(ref m_OutlineMaskRT, desc, name: "_OutlineMaskRT");
        }
        
        public void Dispose()
        {
            // m_OutlineMaskRT?.Release();
            // m_OutlineMaskRT = null;
        }

        /// <inheritdoc/>
        [Obsolete(
            "This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.",
            false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // UniversalCameraData cameraData = renderingData.cameraData;
            var cmd = CommandBufferPool.Get("CustomObject Command");

            using (new ProfilingScope(cmd, profilingSampler))
            {
                // InitPassData(cameraData, ref m_PassData);
                // cmd.SetRenderTarget(m_OutlineMaskRT);
                // cmd.ClearRenderTarget(true, true, Color.clear);
                InitRendererLists(context, ref renderingData, ref m_PassData);
                cmd.DrawRendererList(m_PassData.rendererList);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }


        private class PassData
        {
#if Unity_6_OR_NEWER
            public TextureHandle color;

            public RendererListHandle rendererListHdl;

            public UniversalCameraData cameraData;
#endif
            public RendererList rendererList;
        }

#if Unity_6_OR_NEWER
        private static void ExecutePass(RasterCommandBuffer cmd, RendererList rendererList)
        {
            cmd.DrawRendererList(rendererList);
        }
        private void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.cameraData = cameraData;
        }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
            ref PassData passData, RenderGraph renderGraph)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : passData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData,
                passData.cameraData, lightData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            drawingSettings.overrideShader = overrideShader;
            drawingSettings.overrideShaderPassIndex = overrideShaderPassIndex;

            var renderListParam =
                new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings)
                {
                    stateBlocks = new NativeArray<RenderStateBlock>(m_RenderStateBlock, Allocator.Temp),
                    tagValues = new NativeArray<ShaderTagId>(m_ShaderTag, Allocator.Temp)
                };
            passData.rendererListHdl = renderGraph.CreateRendererList(in renderListParam);
        }
#endif

        private void InitRendererLists(ScriptableRenderContext context, ref RenderingData renderingData,
            ref PassData mPassData)
        {
            SortingCriteria sortingCriteria = renderPassEvent >= RenderPassEvent.BeforeRenderingTransparents
                ? SortingCriteria.CommonTransparent
                : SortingCriteria.None;
            var drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            drawingSettings.overrideShader = overrideShader;
            drawingSettings.overrideShaderPassIndex = overrideShaderPassIndex;
            
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings)
            {
                stateBlocks = new NativeArray<RenderStateBlock>(m_RenderStateBlock, Allocator.Temp),
                tagValues = new NativeArray<ShaderTagId>(m_ShaderTag, Allocator.Temp)
            };
            mPassData.rendererList = context.CreateRendererList(ref rendererListParams);
        }

#if Unity_6_OR_NEWER
        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                InitPassData(cameraData, ref passData);

                passData.color = resourceData.activeColorTexture;
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;
                TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);

                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                TextureHandle[] dBufferHandles = resourceData.dBuffer;
                for (int i = 0; i < dBufferHandles.Length; ++i)
                {
                    TextureHandle dBuffer = dBufferHandles[i];
                    if (dBuffer.IsValid())
                        builder.UseTexture(dBuffer, AccessFlags.Read);
                }

                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);

                InitRendererLists(renderingData, lightData, ref passData, renderGraph);
                builder.UseRendererList(passData.rendererListHdl);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                if (cameraData.xr.enabled)
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(rgContext.cmd, data.rendererListHdl);
                });
            }
        }
#endif
    }
}