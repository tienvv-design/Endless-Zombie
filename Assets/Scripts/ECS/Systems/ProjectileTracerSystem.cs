using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Managed presentation for ECS projectiles. The projectile mesh remains the gameplay visual,
/// while this short emissive streak keeps fast bullets readable from the top-down camera.
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class ProjectileTracerSystem : SystemBase
{
    private sealed class Tracer
    {
        public GameObject Root;
        public LineRenderer Line;
        public int SeenFrame;
        public float FadeRemaining;
        public float TailLifetime;
        public float BaseWidth;
        public float VisualScale;
        public Color Color;
        public ParticleSystem Particles;
    }

    private readonly Dictionary<Entity, Tracer> tracers = new();
    private readonly List<Entity> removalBuffer = new();
    private Material tracerMaterial;

    protected override void OnCreate()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        tracerMaterial = new Material(shader)
        {
            name = "Runtime Projectile Tracer",
            hideFlags = HideFlags.HideAndDontSave,
        };
        tracerMaterial.renderQueue = 3100;
    }

    protected override void OnUpdate()
    {
        int frame = UnityEngine.Time.frameCount;
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach ((RefRO<LocalTransform> transformRef, RefRO<PlayerProjectile> projectileRef, Entity entity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerProjectile>>().WithEntityAccess())
        {
            LocalTransform transform = transformRef.ValueRO;
            PlayerProjectile projectile = projectileRef.ValueRO;
            // LDoE renders the flamethrower as one continuous muzzle stream, not as
            // separate bullets or tracers. The ECS projectile remains gameplay-only.
            if (projectile.Archetype == GunArchetype.FlameRifle) continue;
            if (!tracers.TryGetValue(entity, out Tracer tracer))
            {
                tracer = CreateTracer(projectile.Archetype);
                tracers.Add(entity, tracer);
            }

            float3 direction = math.normalizesafe(projectile.Direction, new float3(0f, 0f, 1f));
            float length = math.clamp(projectile.Speed * 0.065f, 0.75f, 1.55f) * tracer.VisualScale;
            Vector3 head = transform.Position;
            Vector3 tail = transform.Position - direction * length;
            tracer.Root.transform.SetPositionAndRotation(
                head, Quaternion.LookRotation((Vector3)(-direction), Vector3.up));
            tracer.Line.SetPosition(0, tail);
            tracer.Line.SetPosition(1, head);
            tracer.Line.startWidth = tracer.BaseWidth * 0.22f;
            tracer.Line.endWidth = tracer.BaseWidth;
            tracer.Line.startColor = new Color(tracer.Color.r, tracer.Color.g, tracer.Color.b, 0.12f);
            tracer.Line.endColor = tracer.Color;
            tracer.SeenFrame = frame;
            tracer.FadeRemaining = tracer.TailLifetime;
            if (!tracer.Root.activeSelf) tracer.Root.SetActive(true);
        }

        removalBuffer.Clear();
        foreach (KeyValuePair<Entity, Tracer> pair in tracers)
        {
            Tracer tracer = pair.Value;
            if (tracer.SeenFrame == frame) continue;

            if (tracer.Particles != null)
            {
                ParticleSystem.EmissionModule emission = tracer.Particles.emission;
                emission.enabled = false;
            }
            tracer.FadeRemaining -= deltaTime;
            if (tracer.FadeRemaining <= 0f)
            {
                Object.Destroy(tracer.Root);
                removalBuffer.Add(pair.Key);
                continue;
            }

            float alpha = tracer.FadeRemaining / tracer.TailLifetime;
            tracer.Line.startColor = new Color(tracer.Color.r, tracer.Color.g, tracer.Color.b, 0f);
            tracer.Line.endColor = new Color(tracer.Color.r, tracer.Color.g, tracer.Color.b, alpha);
            tracer.Line.startWidth = tracer.BaseWidth * 0.15f * alpha;
            tracer.Line.endWidth = tracer.BaseWidth * alpha;
        }
        foreach (Entity entity in removalBuffer) tracers.Remove(entity);
    }

    protected override void OnDestroy()
    {
        foreach (Tracer tracer in tracers.Values)
            if (tracer.Root != null) Object.Destroy(tracer.Root);
        tracers.Clear();
        if (tracerMaterial != null) Object.Destroy(tracerMaterial);
    }

    private Tracer CreateTracer(GunArchetype archetype)
    {
        (Color color, float width) = GetStyle(archetype);
        GunConfig config = WeaponVfxRuntime.CurrentConfig;
        float visualScale = config != null && config.Archetype == archetype
            ? Mathf.Clamp(config.ProjectileTracerScale, 0.1f, 2f)
            : 1f;
        GameObject root = new($"Projectile Tracer ({archetype})")
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 2;
        line.numCornerVertices = 1;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        line.material = tracerMaterial;
        ParticleSystem particles = CreateSpecialParticles(root, archetype);
        return new Tracer
        {
            Root = root,
            Line = line,
            BaseWidth = width * visualScale,
            VisualScale = visualScale,
            Color = color,
            TailLifetime = archetype switch
            {
                GunArchetype.FlameRifle => 0.26f,
                GunArchetype.CryoGun => 0.34f,
                _ => 0.09f,
            },
            Particles = particles,
        };
    }

    private ParticleSystem CreateSpecialParticles(GameObject root, GunArchetype archetype)
    {
        if (archetype != GunArchetype.CryoGun)
            return null;

        GameObject particleRoot = new("Cryo Projectile VFX");
        particleRoot.transform.SetParent(root.transform, false);
        ParticleSystem particles = particleRoot.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 52;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.82f, 1f, 1f, 1f), new Color(0.1f, 0.55f, 1f, 0.9f));

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.085f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.25f, 0.65f, 1f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.65f, 0.55f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = fade;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0.55f));

        ParticleSystemRenderer renderer = particleRoot.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sharedMaterial = tracerMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 2;
        particles.Play();
        return particles;
    }

    private static (Color color, float width) GetStyle(GunArchetype archetype) => archetype switch
    {
        GunArchetype.RocketLauncher => (new Color(1f, 0.26f, 0.035f, 1f), 0.17f),
        GunArchetype.GrenadeLauncher => (new Color(1f, 0.48f, 0.08f, 1f), 0.16f),
        GunArchetype.TeslaGun => (new Color(0.18f, 0.86f, 1f, 1f), 0.13f),
        GunArchetype.CryoGun => (new Color(0.36f, 0.8f, 1f, 1f), 0.18f),
        GunArchetype.FlameRifle => (new Color(1f, 0.23f, 0.025f, 1f), 0.22f),
        GunArchetype.SniperRifle => (new Color(1f, 0.9f, 0.48f, 1f), 0.12f),
        GunArchetype.Shotgun => (new Color(1f, 0.65f, 0.22f, 1f), 0.11f),
        _ => (new Color(1f, 0.82f, 0.32f, 1f), 0.10f),
    };
}
