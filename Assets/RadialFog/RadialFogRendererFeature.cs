using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class RadialFogRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    private Material material;
    private RadialFogPass pass;

    public override void Create()
    {
        Shader shader = Shader.Find("Hidden/RPG/RadialFog");
        if (shader == null)
        {
            Debug.LogError("Radial fog shader 'Hidden/RPG/RadialFog' was not found.");
            return;
        }

        CoreUtils.Destroy(material);
        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new RadialFogPass(material) { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null ||
            renderingData.cameraData.cameraType is CameraType.Preview or CameraType.Reflection)
            return;

        RadialFogVolume volume = VolumeManager.instance.stack.GetComponent<RadialFogVolume>();
        if (volume == null || !volume.IsActive()) return;

        pass.renderPassEvent = renderPassEvent;
        pass.SetVolume(volume);
        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (pass != null) pass.SetTarget(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        CoreUtils.Destroy(material);
        material = null;
    }

    private sealed class RadialFogPass : ScriptableRenderPass
    {
        private static readonly int FogColorId = Shader.PropertyToID("_RadialFogColor");
        private static readonly int FogCenterId = Shader.PropertyToID("_RadialFogCenter");
        private static readonly int ClearRadiusId = Shader.PropertyToID("_RadialFogClearRadius");
        private static readonly int DensityId = Shader.PropertyToID("_RadialFogDensity");
        private static readonly int MaxOpacityId = Shader.PropertyToID("_RadialFogMaxOpacity");

        private readonly Material material;
        private RadialFogVolume volume;
        private RTHandle cameraColorTarget;
        private RTHandle temporaryColor;

        public RadialFogPass(Material material)
        {
            this.material = material;
            ConfigureInput(ScriptableRenderPassInput.Depth);
            requiresIntermediateTexture = true;
        }

        public void SetVolume(RadialFogVolume activeVolume) => volume = activeVolume;
        public void SetTarget(RTHandle target) => cameraColorTarget = target;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref temporaryColor, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_RadialFogTemporaryColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (cameraColorTarget == null || volume == null || !volume.IsActive()) return;

            material.SetColor(FogColorId, volume.color.value);
            material.SetVector(FogCenterId, volume.center.value);
            material.SetFloat(ClearRadiusId, volume.clearRadius.value);
            material.SetFloat(DensityId, volume.density.value);
            material.SetFloat(MaxOpacityId, volume.maxOpacity.value);

            CommandBuffer cmd = CommandBufferPool.Get("Radial Fog");
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, temporaryColor, material, 0);
            Blitter.BlitCameraTexture(cmd, temporaryColor, cameraColorTarget);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null || volume == null || !volume.IsActive()) return;

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer)
            {
                Debug.LogWarning("Radial Fog skipped because the active camera color is the back buffer.");
                return;
            }

            TextureHandle source = resources.activeColorTexture;
            TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
            destinationDescriptor.name = "_RadialFogRenderGraphColor";
            destinationDescriptor.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>(
                       "Radial Fog", out PassData passData, profilingSampler))
            {
                passData.source = source;
                passData.material = material;
                passData.fogColor = volume.color.value;
                passData.fogCenter = volume.center.value;
                passData.clearRadius = volume.clearRadius.value;
                passData.density = volume.density.value;
                passData.maxOpacity = volume.maxOpacity.value;

                builder.UseTexture(source, AccessFlags.Read);
                if (resources.cameraDepthTexture.IsValid())
                    builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    data.material.SetColor(FogColorId, data.fogColor);
                    data.material.SetVector(FogCenterId, data.fogCenter);
                    data.material.SetFloat(ClearRadiusId, data.clearRadius);
                    data.material.SetFloat(DensityId, data.density);
                    data.material.SetFloat(MaxOpacityId, data.maxOpacity);
                    Blitter.BlitTexture(
                        context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, 0);
                });
            }

            renderGraph.AddBlitPass(
                destination, source, Vector2.one, Vector2.zero, passName: "Radial Fog Copy Back");
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cameraColorTarget = null;
            volume = null;
        }

        public void Dispose()
        {
            temporaryColor?.Release();
            temporaryColor = null;
        }

        private sealed class PassData
        {
            public TextureHandle source;
            public Material material;
            public Color fogColor;
            public Vector3 fogCenter;
            public float clearRadius;
            public float density;
            public float maxOpacity;
        }
    }
}
