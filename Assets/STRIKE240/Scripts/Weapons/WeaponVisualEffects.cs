using UnityEngine;
using UnityEngine.Rendering;

namespace Strike240
{
    /// <summary>Small pooled muzzle and impact effects for the prototype rifle.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RifleWeapon))]
    public sealed class WeaponVisualEffects : MonoBehaviour
    {
        [SerializeField] private RifleWeapon weapon;
        [SerializeField] private Transform muzzleTransform;
        [SerializeField, Range(2, 12)] private int impactPoolSize = 8;
        [SerializeField, Min(0.01f)] private float muzzleLightDuration = 0.045f;

        private ParticleSystem muzzleParticles;
        private ParticleSystem[] impactParticles;
        private Light muzzleLight;
        private Material particleMaterial;
        private int nextImpactIndex;
        private float muzzleLightTime;

        private void Awake()
        {
            if (weapon == null) weapon = GetComponent<RifleWeapon>();
            if (muzzleTransform == null && transform.parent != null)
            {
                Transform model = transform.parent.Find("AK Placeholder View Model");
                if (model != null) muzzleTransform = model.Find("Muzzle Point");
            }

            CreateParticleMaterial();
            muzzleParticles = CreateParticleSystem("Muzzle flash particles", new Color(1f, 0.62f, 0.18f, 0.96f), 0.065f, 2.8f, 0.075f, ParticleSystemShapeType.Cone);
            muzzleLight = new GameObject("Muzzle flash light").AddComponent<Light>();
            muzzleLight.transform.SetParent(transform, false);
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(1f, 0.56f, 0.2f);
            muzzleLight.range = 2.6f;
            muzzleLight.intensity = 2.4f;
            muzzleLight.shadows = LightShadows.None;
            muzzleLight.enabled = false;

            impactParticles = new ParticleSystem[Mathf.Clamp(impactPoolSize, 2, 12)];
            for (int i = 0; i < impactParticles.Length; i++)
                impactParticles[i] = CreateParticleSystem("Impact sparks " + (i + 1), new Color(0.75f, 0.62f, 0.43f, 0.9f), 0.035f, 1.15f, 0.18f, ParticleSystemShapeType.Sphere);
        }

        private void OnEnable()
        {
            if (weapon == null) weapon = GetComponent<RifleWeapon>();
            if (weapon == null) return;
            weapon.WeaponFired += HandleWeaponFired;
            weapon.SurfaceHit += HandleSurfaceHit;
        }

        private void OnDisable()
        {
            if (weapon == null) return;
            weapon.WeaponFired -= HandleWeaponFired;
            weapon.SurfaceHit -= HandleSurfaceHit;
        }

        private void Update()
        {
            if (!muzzleLight.enabled) return;
            muzzleLightTime -= Time.deltaTime;
            if (muzzleLightTime <= 0f) muzzleLight.enabled = false;
        }

        private void HandleWeaponFired()
        {
            Vector3 position = muzzleTransform != null
                ? muzzleTransform.position
                : transform.position + transform.forward * 0.7f;
            Quaternion rotation = muzzleTransform != null ? muzzleTransform.rotation : transform.rotation;

            muzzleParticles.transform.SetPositionAndRotation(position, rotation);
            muzzleParticles.Emit(4);
            muzzleParticles.Play(false);

            muzzleLight.transform.position = position;
            muzzleLightTime = muzzleLightDuration;
            muzzleLight.enabled = true;
        }

        private void HandleSurfaceHit(RaycastHit hit)
        {
            ParticleSystem impact = impactParticles[nextImpactIndex];
            nextImpactIndex = (nextImpactIndex + 1) % impactParticles.Length;
            impact.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            impact.transform.SetPositionAndRotation(hit.point + hit.normal * 0.012f, Quaternion.LookRotation(hit.normal));
            impact.Emit(5);
            impact.Play(false);
        }

        private ParticleSystem CreateParticleSystem(string objectName, Color startColor, float size, float speed, float lifetime, ParticleSystemShapeType shapeType)
        {
            var effectObject = new GameObject(objectName);
            effectObject.transform.SetParent(transform, false);
            ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.65f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size);
            main.startColor = startColor;
            main.gravityModifier = shapeType == ParticleSystemShapeType.Sphere ? 0.2f : 0f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.radius = 0.012f;
            if (shapeType == ParticleSystemShapeType.Cone)
            {
                shape.angle = 18f;
                shape.length = 0.08f;
            }

            ParticleSystemRenderer particleRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.material = particleMaterial;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            return particles;
        }

        private void CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return;

            particleMaterial = new Material(shader) { name = "BREACH Runtime FX Material", hideFlags = HideFlags.DontSave };
            if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", Color.white);
            if (particleMaterial.HasProperty("_Color")) particleMaterial.SetColor("_Color", Color.white);
        }

        private void OnDestroy()
        {
            if (particleMaterial != null) Destroy(particleMaterial);
        }
    }
}
