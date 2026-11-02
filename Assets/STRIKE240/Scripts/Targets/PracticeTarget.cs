using System.Collections;
using UnityEngine;

namespace Strike240
{
    /// <summary>Damageable range target that visibly confirms raycast hits.</summary>
    public sealed class PracticeTarget : DamageableEntity
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.08f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;

        private Material runtimeMaterial;
        private Color baseColor;
        private Coroutine flashRoutine;
        [SerializeField] private int hitCount;
        [SerializeField] private float currentHealth;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public int HitCount => hitCount;
        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;

        private void Awake()
        {
            currentHealth = maximumHealth;
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                runtimeMaterial = targetRenderer.material;
                baseColor = ReadColor(runtimeMaterial);
            }
        }

        public void RegisterHit(RaycastHit hit)
        {
            RegisterHit(hit, 0f);
        }

        public void RegisterHit(RaycastHit hit, float damage)
        {
            hitCount++;
            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, damage));
            if (runtimeMaterial == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashHit());
        }

        public override void TakeDamage(float damage, Vector3 hitPoint)
        {
            RegisterHit(new RaycastHit { point = hitPoint }, damage);
        }

        private IEnumerator FlashHit()
        {
            WriteColor(runtimeMaterial, hitColor);
            yield return new WaitForSeconds(flashDuration);
            if (runtimeMaterial != null) WriteColor(runtimeMaterial, baseColor);
            flashRoutine = null;
        }

        private static Color ReadColor(Material material)
        {
            return material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : material.color;
        }

        private static void WriteColor(Material material, Color color)
        {
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            else material.color = color;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
