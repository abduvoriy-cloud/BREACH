using UnityEngine;

namespace Strike240
{
    /// <summary>Low-frequency player sensing with field-of-view and solid-geometry line of sight.</summary>
    [DisallowMultipleComponent]
    public sealed class BotVision : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float visionRange = 24f;
        [SerializeField, Range(30f, 180f)] private float fieldOfView = 110f;
        [SerializeField, Min(0f)] private float detectionDelay = 0.36f;
        [SerializeField, Min(0.05f)] private float sensingInterval = 0.2f;
        [SerializeField, Min(0.2f)] private float eyeHeight = 1.55f;
        [SerializeField] private LayerMask lineOfSightMask = Physics.DefaultRaycastLayers;

        private PlayerHealth player;
        private TeamMember observerTeam;
        private TeamMember targetTeam;
        private float nextSenseTime;
        private float firstVisibleTime;
        private bool wasVisibleLastSense;
        private bool hasSeenPlayer;
        private bool hasDetectedPlayer;

        public PlayerHealth Player => player;
        public bool IsPlayerVisible { get; private set; }
        public bool HasSeenPlayer => hasSeenPlayer;
        public bool HasDetectedPlayer => hasDetectedPlayer;
        public Vector3 LastKnownPosition { get; private set; }
        public float LastSeenTime { get; private set; } = -1000f;
        public float VisionRange => visionRange;
        public float FieldOfView => fieldOfView;

        public void Configure(PlayerHealth target, BotDifficultySettings settings)
        {
            player = target;
            visionRange = settings.visionRange;
            fieldOfView = settings.fieldOfView;
            detectionDelay = settings.detectionDelay;
            nextSenseTime = 0f;
        }

        public void SetPlayer(PlayerHealth target)
        {
            player = target;
            targetTeam = target != null ? target.GetComponent<TeamMember>() : null;
            nextSenseTime = 0f;
        }

        public void SetTeamMembers(TeamMember observer, TeamMember target)
        {
            observerTeam = observer;
            targetTeam = target;
        }

        public void ForgetPlayer()
        {
            IsPlayerVisible = false;
            hasSeenPlayer = false;
            hasDetectedPlayer = false;
            wasVisibleLastSense = false;
            LastSeenTime = -1000f;
        }

        public void TickVision()
        {
            if (Time.time < nextSenseTime) return;
            nextSenseTime = Time.time + sensingInterval;

            if (player == null || !player.IsAlive)
            {
                IsPlayerVisible = false;
                return;
            }

            if (observerTeam != null && targetTeam != null && observerTeam.Team == targetTeam.Team)
            {
                ForgetPlayer();
                return;
            }

            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPoint = player.AimPoint;
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;

            bool inRange = distance <= visionRange && distance > 0.01f;
            Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
            bool inFieldOfView = flatDirection.sqrMagnitude > 0.01f &&
                                 Vector3.Angle(transform.forward, flatDirection) <= fieldOfView * 0.5f;
            bool clearLine = false;

            if (inRange && inFieldOfView)
            {
                Vector3 direction = toTarget / distance;
                clearLine = !Physics.Raycast(origin, direction, out RaycastHit hit, distance,
                    lineOfSightMask, QueryTriggerInteraction.Ignore) ||
                    hit.collider.GetComponentInParent<PlayerHealth>() == player;
            }

            IsPlayerVisible = inRange && inFieldOfView && clearLine;
            if (IsPlayerVisible)
            {
                if (!wasVisibleLastSense) firstVisibleTime = Time.time;
                wasVisibleLastSense = true;
                hasSeenPlayer = true;
                LastKnownPosition = player.transform.position;
                LastSeenTime = Time.time;
                if (Time.time - firstVisibleTime >= detectionDelay)
                    hasDetectedPlayer = true;
            }
            else
            {
                wasVisibleLastSense = false;
            }
        }

        private void OnValidate()
        {
            visionRange = Mathf.Max(1f, visionRange);
            fieldOfView = Mathf.Clamp(fieldOfView, 30f, 180f);
            sensingInterval = Mathf.Max(0.05f, sensingInterval);
        }
    }
}
