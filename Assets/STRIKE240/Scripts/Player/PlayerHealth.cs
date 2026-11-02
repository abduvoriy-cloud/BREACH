using System;
using UnityEngine;

namespace Strike240
{
    /// <summary>Small player health component used by the offline bot combat prototype.</summary>
    public sealed class PlayerHealth : DamageableEntity
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private Transform aimTarget = null;

        [SerializeField, Min(0f)] private float currentHealth;

        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public override bool IsAlive => currentHealth > 0f;
        public Vector3 AimPoint => aimTarget != null ? aimTarget.position : transform.position + Vector3.up * 1.25f;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            currentHealth = maximumHealth;
        }

        public override void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (!IsAlive || damage <= 0f) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            HealthChanged?.Invoke(currentHealth, maximumHealth);
            if (currentHealth <= 0f) Died?.Invoke();
        }

        public void RestoreHealth(float amount)
        {
            if (amount <= 0f || currentHealth >= maximumHealth) return;
            currentHealth = Mathf.Min(maximumHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }

        public void ResetForRound()
        {
            currentHealth = maximumHealth;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
        }
    }
}
