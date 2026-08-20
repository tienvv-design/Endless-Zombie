using UnityEngine;
using UnityEngine.Rendering;
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
    }
}
