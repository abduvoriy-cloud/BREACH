using UnityEngine;

namespace Strike240
{
    /// <summary>Shared damage entry point for practice targets, players, and offline bots.</summary>
    public abstract class DamageableEntity : MonoBehaviour
    {
        public virtual bool IsAlive => true;

        public event System.Action<DamageableEntity, TeamMember> Eliminated;

        public abstract void TakeDamage(float damage, Vector3 hitPoint);

        /// <summary>Applies damage with optional team attribution for the offline match.</summary>
        public void ApplyDamage(float damage, Vector3 hitPoint, TeamMember attacker = null)
        {
            if (damage <= 0f || !IsAlive) return;

            TeamMember target = GetComponent<TeamMember>();
            if (attacker != null && target != null && attacker.Team == target.Team) return;

            TakeDamage(damage, hitPoint);
            if (IsAlive) return;

            if (attacker != null && target != null && attacker.Team != target.Team)
                attacker.RecordKill();

            Eliminated?.Invoke(this, attacker);
        }
    }
}
