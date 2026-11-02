using UnityEngine;
using UnityEngine.AI;

namespace Strike240
{
    /// <summary>Registers the baked NavMesh asset for this scene while it is loaded.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class BotNavigationSurface : MonoBehaviour
    {
        [SerializeField] private NavMeshData navMeshData;
        [SerializeField, Min(0)] private int agentTypeId;

        private NavMeshDataInstance activeInstance;

        public NavMeshData NavMeshData => navMeshData;
        public int AgentTypeId => agentTypeId;
        public bool IsRegistered => activeInstance.valid;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (navMeshData == null)
            {
                Debug.LogError("[BREACH NavMesh] No NavMesh asset is assigned to this scene surface. Bake DUST SECTOR from the BREACH offline bot tools.", this);
                return;
            }

            if (!RegisterNavMesh())
                Debug.LogError($"[BREACH NavMesh] Failed to register the NavMesh asset for Agent Type ID {agentTypeId}. Re-bake DUST SECTOR.", this);
        }

        private void OnDisable()
        {
            UnregisterNavMesh();
        }

        private void OnDestroy()
        {
            UnregisterNavMesh();
        }

        public void AssignNavMeshData(NavMeshData data)
        {
            AssignNavMeshData(data, agentTypeId);
        }

        public void AssignNavMeshData(NavMeshData data, int bakedAgentTypeId)
        {
            UnregisterNavMesh();
            navMeshData = data;
            agentTypeId = Mathf.Max(0, bakedAgentTypeId);
            if (Application.isPlaying) RegisterNavMesh();
        }

        /// <summary>Registers the assigned data in Play Mode or for an Editor-side validation query.</summary>
        public bool EnsureRegistered()
        {
            if (activeInstance.valid) return true;
            if (navMeshData == null) return false;

            activeInstance = NavMesh.AddNavMeshData(navMeshData, transform.position, transform.rotation);
            return activeInstance.valid;
        }

        public NavMeshQueryFilter CreateQueryFilter(int areaMask = NavMesh.AllAreas)
        {
            return new NavMeshQueryFilter
            {
                agentTypeID = agentTypeId,
                areaMask = areaMask
            };
        }

        public void UnregisterNavMesh()
        {
            NavMeshDataInstance instanceToRemove = activeInstance;
            activeInstance = default;
            if (instanceToRemove.valid)
                NavMesh.RemoveNavMeshData(instanceToRemove);
        }

        private bool RegisterNavMesh()
        {
            return EnsureRegistered();
        }
    }
}
