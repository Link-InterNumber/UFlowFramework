using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PowerCellStudio
{
    public class CustomObjectFeature : ScriptableRendererFeature
    {
        private bool IsMaterialValid(Material mat)
        {
            return mat && mat.shader && mat.shader.isSupported;
        }

        /// <summary>
        /// Settings class used for the render objects renderer feature.
        /// </summary>
        [System.Serializable]
        public class CustomRenderObjectsSettings
        {
            /// <summary>
            /// The profiler tag used with the pass.
            /// </summary>
            public string passTag = "RenderObjectsFeature";

            /// <summary>
            /// Controls when the render pass executes.
            /// </summary>
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            /// <summary>
            /// The filter settings for the pass.
            /// </summary>
            public FilterSettings filterSettings = new FilterSettings();

            /// <summary>
            /// The override material to use.
            /// </summary>
            public Material overrideMaterial = null;

            /// <summary>
            /// The pass index to use with the override material.
            /// </summary>
            public int overrideMaterialPassIndex = 0;

            /// <summary>
            /// The override shader to use.
            /// </summary>
            public Shader overrideShader = null;

            /// <summary>
            /// The pass index to use with the override shader.
            /// </summary>
            public int overrideShaderPassIndex = 0;

            /// <summary>
            /// Options to select which type of override mode should be used.
            /// </summary>
            public enum OverrideMaterialMode
            {
                /// <summary>
                /// Use this to not override.
                /// </summary>
                None,

                /// <summary>
                /// Use this to use an override material.
                /// </summary>
                Material,

                /// <summary>
                /// Use this to use an override shader.
                /// </summary>
                Shader
            };

            /// <summary>
            /// The selected override mode.
            /// </summary>
            public OverrideMaterialMode
                overrideMode =
                    OverrideMaterialMode.Material; //default to Material as this was previously the only option

            /// <summary>
            /// Sets whether it should override depth or not.
            /// </summary>
            public bool overrideDepthState = false;

            /// <summary>
            /// The depth comparison function to use.
            /// </summary>
            public CompareFunction depthCompareFunction = CompareFunction.LessEqual;

            /// <summary>
            /// Sets whether it should write to depth or not.
            /// </summary>
            public bool enableWrite = true;

            /// <summary>
            /// The stencil settings to use.
            /// </summary>
            public StencilStateData stencilSettings = new StencilStateData();

            /// <summary>
            /// The camera settings to use.
            /// </summary>
            public CustomCameraSettings cameraSettings = new CustomCameraSettings();
        }
        
        public enum RenderQueueMode
        {
            Opaque,
            Transparent,
            All
        }
        
        public enum MaskMode
        {
            GameObjectLayer,
            RenderingLayer
        }

        /// <summary>
        /// The filter settings used.
        /// </summary>
        [System.Serializable]
        public class FilterSettings
        {
            // TODO: expose opaque, transparent, all ranges as drop down

            /// <summary>
            /// The queue type for the objects to render.
            /// </summary>
            public RenderQueueMode renderQueueMode;

            public MaskMode maskMode;

            /// <summary>
            /// The layer mask to use.
            /// </summary>
            public LayerMask LayerMask;
            
            /// <summary>
            /// The rendering layer mask to use.
            /// </summary>
            public UInt32 renderingLayerMask;

            /// <summary>
            /// The passes to render.
            /// </summary>
            public string[] PassNames;

            /// <summary>
            /// The constructor for the filter settings.
            /// </summary>
            public FilterSettings()
            {
                renderQueueMode = RenderQueueMode.Opaque;
                maskMode = MaskMode.GameObjectLayer;
                LayerMask = 0;
                renderingLayerMask = 0;
            }
        }

        /// <summary>
        /// The settings for custom cameras values.
        /// </summary>
        [System.Serializable]
        public class CustomCameraSettings
        {
            /// <summary>
            /// Used to mark whether camera values should be changed or not.
            /// </summary>
            public bool overrideCamera = false;

            /// <summary>
            /// Should the values be reverted after rendering the objects?
            /// </summary>
            public bool restoreCamera = true;

            /// <summary>
            /// Changes the camera offset.
            /// </summary>
            public Vector4 offset;

            /// <summary>
            /// Changes the camera field of view.
            /// </summary>
            public float cameraFieldOfView = 60.0f;
        }

        /// <summary>
        /// The settings used for the Render Objects renderer feature.
        /// </summary>
        public CustomRenderObjectsSettings settings = new CustomRenderObjectsSettings();

        CustomObjectRenderPass renderObjectsPass;

        /// <inheritdoc/>
        public override void Create()
        {
            FilterSettings filter = settings.filterSettings;

            // Render Objects pass doesn't support events before rendering prepasses.
            // The camera is not setup before this point and all rendering is monoscopic.
            // Events before BeforeRenderingPrepasses should be used for input texture passes (shadow map, LUT, etc) that doesn't depend on the camera.
            // These events are filtering in the UI, but we still should prevent users from changing it from code or
            // by changing the serialized data.
            if (settings.Event < RenderPassEvent.BeforeRenderingPrePasses)
                settings.Event = RenderPassEvent.BeforeRenderingPrePasses;

            renderObjectsPass = new CustomObjectRenderPass(settings);

            switch (settings.overrideMode)
            {
                case CustomRenderObjectsSettings.OverrideMaterialMode.None:
                    renderObjectsPass.overrideMaterial = null;
                    renderObjectsPass.overrideShader = null;
                    break;
                case CustomRenderObjectsSettings.OverrideMaterialMode.Material:
                    renderObjectsPass.overrideMaterial = settings.overrideMaterial;
                    renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;
                    renderObjectsPass.overrideShader = null;
                    break;
                case CustomRenderObjectsSettings.OverrideMaterialMode.Shader:
                    renderObjectsPass.overrideMaterial = null;
                    renderObjectsPass.overrideShader = settings.overrideShader;
                    renderObjectsPass.overrideShaderPassIndex = settings.overrideShaderPassIndex;
                    break;
            }

            if (settings.overrideDepthState)
                renderObjectsPass.SetDepthState(settings.enableWrite, settings.depthCompareFunction);

            if (settings.stencilSettings.overrideStencilState)
                renderObjectsPass.SetStencilState(settings.stencilSettings.stencilReference,
                    settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                    settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || UniversalRenderer.IsOffscreenDepthTexture(in renderingData.cameraData))
                return;
            renderer.EnqueuePass(renderObjectsPass);
        }
        
        protected override void Dispose(bool disposing)
        {
            renderObjectsPass?.Dispose();
        }
    }
}