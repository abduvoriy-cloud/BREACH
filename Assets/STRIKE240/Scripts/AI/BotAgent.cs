using System;
using UnityEngine;
using UnityEngine.AI;

namespace Strike240
{
    /// <summary>Navigation, health, and fictional hitscan combat for one offline bot.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider), typeof(BotVision))]
    public sealed class BotAgent : DamageableEntity
    {
        [Header("Setup")]
        [SerializeField] private BotDifficulty difficulty = BotDifficulty.Medium;
        [SerializeField, HideInInspector] private BotDifficulty lastAppliedDifficulty = BotDifficulty.Medium;
        [SerializeField] private PlayerHealth playerTarget;
        [SerializeField] private BotNavigationSurface navigationSurface;
        [SerializeField] private Vector3 patrolCenter;
        [SerializeField, Min(1f)] private float patrolRadius = 8f;
        [SerializeField] private LayerMask shotMask = Physics.DefaultRaycastLayers;

        [Header("Difficulty Tuning")]
        [SerializeField, Min(1f)] private float visionRange = 24f;
        [SerializeField, Range(30f, 180f)] private float fieldOfView = 110f;
        [SerializeField, Min(0f)] private float detectionDelay = 0.36f;
        [SerializeField, Min(0f)] private float reactionTime = 0.44f;
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.48f;
        [SerializeField, Min(0.1f)] private float fireInterval = 0.62f;
        [SerializeField, Min(0f)] private float damagePerShot = 12f;
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0.1f)] private float movementSpeed = 3.2f;

        [Header("Behavior")]
        [SerializeField, Min(1f)] private float engageDistance = 22f;
        [SerializeField, Min(0.1f)] private float chaseUpdateInterval = 0.55f;
        [SerializeField, Min(0.5f)] private float investigationTimeout = 7f;
        [SerializeField, Min(0.5f)] private float investigationMemorySeconds = 2.5f;
        [SerializeField, Min(1f)] private float searchDuration = 5f;

        [Header("Optional Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shotSound = null;

        private NavMeshAgent navMeshAgent;
        private BotVision botVision;
        private BotStateMachine stateMachine;
        [SerializeField, Min(0f)] private float currentHealth;
        private float nextShotTime;
        private float searchDeadline;
        private bool isAlive = true;
        private bool isEngaging;
        private bool roundCombatEnabled = true;
        private CapsuleCollider hitCollider;
        private string standingObjectName;

        private static Material bodyMaterial;
        private static Material darkMaterial;

        public NavMeshAgent Agent => navMeshAgent;
        public BotVision Vision => botVision;
        public bool AgentReady => navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh;
        public override bool IsAlive => isAlive;
        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public BotDifficulty Difficulty => difficulty;
        public BotStateId CurrentState => stateMachine == null ? BotStateId.Idle : stateMachine.CurrentState;
        public float EngageDistance => engageDistance;
        public float ChaseUpdateInterval => chaseUpdateInterval;
        public float InvestigationTimeout => investigationTimeout;
        public float InvestigationMemorySeconds => investigationMemorySeconds;
        public float SearchDeadline => searchDeadline;
        public bool HasUnreachablePath => AgentReady && !navMeshAgent.pathPending &&
                                         navMeshAgent.hasPath &&
                                         navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete;

        public event Action<BotAgent> Died;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            hitCollider = GetComponent<CapsuleCollider>();
            botVision = GetComponent<BotVision>();
            standingObjectName = gameObject.name;
            currentHealth = maximumHealth;

            // Agents are enabled only after the scene NavMesh has been registered and a valid
            // position has been sampled. This avoids native agent creation at an off-mesh point.
            navMeshAgent.enabled = false;
            navMeshAgent.radius = 0.4f;
            navMeshAgent.height = 1.8f;
            navMeshAgent.speed = movementSpeed;
            navMeshAgent.acceleration = 8f;
            navMeshAgent.angularSpeed = 0f;
            navMeshAgent.stoppingDistance = 1.4f;
            navMeshAgent.updateRotation = false;

            hitCollider.height = 1.8f;
            hitCollider.radius = 0.38f;
            hitCollider.center = Vector3.up * 0.9f;

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            BuildPlaceholderVisual();
        }

        private void Start()
        {
            if (playerTarget == null) playerTarget = FindAnyObjectByType<PlayerHealth>();
            if (navigationSurface == null) navigationSurface = FindAnyObjectByType<BotNavigationSurface>();
            if (navigationSurface == null || !navigationSurface.EnsureRegistered())
            {
                Debug.LogError($"{name} cannot start: no valid BREACH NavMesh is registered. Bake DUST SECTOR from BREACH > Offline Bots > Test Bot Spawner.", this);
                return;
            }

            navMeshAgent.agentTypeID = navigationSurface.AgentTypeId;
            if (!TrySnapToNavMesh())
            {
                Debug.LogError($"{name} could not find a nearby point on the Agent Type {navigationSurface.AgentTypeId} NavMesh. Re-bake DUST SECTOR and respawn the bot.", this);
                return;
            }

            botVision.Configure(playerTarget, CurrentSettings());
            botVision.SetTeamMembers(GetComponent<TeamMember>(),
                playerTarget != null ? playerTarget.GetComponent<TeamMember>() : null);
            stateMachine = new BotStateMachine(this);
            stateMachine.Start();
        }

        private void Update()
        {
            if (!isAlive || !roundCombatEnabled || stateMachine == null) return;
            botVision.TickVision();
            stateMachine.Tick();
            UpdateFacing();
        }

        public void Configure(PlayerHealth target, BotDifficulty selectedDifficulty, Vector3 center,
            float radius = 8f, BotNavigationSurface surface = null)
        {
            playerTarget = target;
            if (surface != null) navigationSurface = surface;
            difficulty = selectedDifficulty;
            lastAppliedDifficulty = selectedDifficulty;
            patrolCenter = center;
            patrolRadius = Mathf.Max(1f, radius);
            ApplyDifficultySettings();
            if (navMeshAgent != null) navMeshAgent.speed = movementSpeed;
            if (botVision != null) botVision.Configure(playerTarget, CurrentSettings());
            if (botVision != null)
                botVision.SetTeamMembers(GetComponent<TeamMember>(),
                    playerTarget != null ? playerTarget.GetComponent<TeamMember>() : null);
        }

        public void SetCombatContext(PlayerHealth target, BotNavigationSurface surface)
        {
            playerTarget = target;
            if (surface != null) navigationSurface = surface;
            if (botVision != null)
            {
                botVision.Configure(playerTarget, CurrentSettings());
                botVision.SetTeamMembers(GetComponent<TeamMember>(),
                    playerTarget != null ? playerTarget.GetComponent<TeamMember>() : null);
            }
        }

        public void SetTeamContext(PlayerHealth target, BotNavigationSurface surface)
        {
            SetCombatContext(target, surface);
        }

        public void SetCombatEnabled(bool enabled)
        {
            roundCombatEnabled = enabled;
            if (!enabled)
            {
                EndEngagement();
                botVision?.ForgetPlayer();
                if (AgentReady)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.ResetPath();
                }
            }
            else if (AgentReady)
            {
                navMeshAgent.isStopped = false;
            }
        }

        public bool ResetForRound(Vector3 spawnPosition, Quaternion spawnRotation, PlayerHealth target,
            BotNavigationSurface surface)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
            if (hitCollider == null) hitCollider = GetComponent<CapsuleCollider>();
            if (botVision == null) botVision = GetComponent<BotVision>();

            SetCombatContext(target, surface);
            roundCombatEnabled = false;
            isAlive = true;
            currentHealth = maximumHealth;
            isEngaging = false;
            nextShotTime = 0f;
            gameObject.name = standingObjectName;
            if (hitCollider != null) hitCollider.enabled = true;
            if (navMeshAgent == null || navigationSurface == null || !navigationSurface.EnsureRegistered())
            {
                isAlive = false;
                if (hitCollider != null) hitCollider.enabled = false;
                return false;
            }

            navMeshAgent.enabled = false;
            navMeshAgent.agentTypeID = navigationSurface.AgentTypeId;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            if (!TrySnapToNavMesh())
            {
                isAlive = false;
                if (hitCollider != null) hitCollider.enabled = false;
                return false;
            }

            navMeshAgent.ResetPath();
            navMeshAgent.isStopped = true;
            botVision.Configure(playerTarget, CurrentSettings());
            botVision.SetTeamMembers(GetComponent<TeamMember>(),
                playerTarget != null ? playerTarget.GetComponent<TeamMember>() : null);
            botVision.ForgetPlayer();
            stateMachine = new BotStateMachine(this);
            stateMachine.Start();
            return true;
        }

        public void SetDifficulty(BotDifficulty selectedDifficulty)
        {
            difficulty = selectedDifficulty;
            lastAppliedDifficulty = selectedDifficulty;
            ApplyDifficultySettings();
            currentHealth = maximumHealth;
            isAlive = true;
        }

        public override void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (!isAlive || damage <= 0f) return;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (currentHealth > 0f) return;

            isAlive = false;
            if (AgentReady)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
            if (hitCollider != null) hitCollider.enabled = false;
            gameObject.name = name + " (down)";
            Died?.Invoke(this);
        }

        public bool TrySetDestination(Vector3 destination)
        {
            if (!AgentReady) return false;
            NavMeshQueryFilter filter = GetNavigationFilter();
            if (!NavMesh.SamplePosition(destination, out NavMeshHit sample, 3f, filter))
                return false;
            navMeshAgent.isStopped = false;
            return navMeshAgent.SetDestination(sample.position);
        }

        public bool HasReachedDestination()
        {
            if (!AgentReady || navMeshAgent.pathPending) return false;
            if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid) return true;
            return navMeshAgent.hasPath &&
                   navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.25f;
        }

        public bool TryFindPatrolPoint(out Vector3 point)
        {
            return TryFindPoint(patrolCenter, patrolRadius, out point);
        }

        public bool TryFindSearchPoint(out Vector3 point)
        {
            Vector3 center = botVision != null && botVision.HasSeenPlayer
                ? botVision.LastKnownPosition
                : patrolCenter;
            return TryFindPoint(center, 6f, out point);
        }

        public void BeginEngagement()
        {
            isEngaging = true;
            nextShotTime = Time.time + reactionTime;
        }

        public void EndEngagement()
        {
            isEngaging = false;
        }

        public void BeginSearch()
        {
            searchDeadline = Time.time + searchDuration;
        }

        public void TryFireAtPlayer()
        {
            if (!isEngaging || Time.time < nextShotTime || !AgentReady ||
                botVision == null || !botVision.IsPlayerVisible || !botVision.HasDetectedPlayer)
                return;

            PlayerHealth target = botVision.Player;
            if (target == null || !target.IsAlive) return;
            TeamMember shooterTeam = GetComponent<TeamMember>();
            TeamMember targetTeam = target.GetComponent<TeamMember>();
            if (shooterTeam != null && targetTeam != null && shooterTeam.Team == targetTeam.Team) return;
            nextShotTime = Time.time + fireInterval;

            Vector3 origin = transform.position + Vector3.up * 1.35f + transform.forward * 0.35f;
            Vector3 aimDirection = (target.AimPoint - origin).normalized;
            float spread = Mathf.Lerp(0.14f, 0.018f, accuracy);
            Vector2 miss = UnityEngine.Random.insideUnitCircle * spread;
            Vector3 shotDirection = (aimDirection + transform.right * miss.x + transform.up * miss.y).normalized;

            if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);
            if (Physics.Raycast(origin, shotDirection, out RaycastHit hit, visionRange,
                    shotMask, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<PlayerHealth>() == target)
            {
                target.ApplyDamage(damagePerShot, hit.point, shooterTeam);
            }
        }

        public void FacePosition(Vector3 position)
        {
            Vector3 direction = Vector3.ProjectOnPlane(position - transform.position, Vector3.up);
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, 360f * Time.deltaTime);
        }

        private bool TryFindPoint(Vector3 center, float radius, out Vector3 point)
        {
            point = center;
            if (!AgentReady) return false;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
            NavMeshQueryFilter filter = GetNavigationFilter();
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit sample, radius + 2f, filter))
                return false;
            point = sample.position;
            return true;
        }

        private bool TrySnapToNavMesh()
        {
            if (navigationSurface == null || navMeshAgent == null) return false;

            NavMeshQueryFilter filter = GetNavigationFilter();
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit sample, 4f, filter))
                return false;

            transform.position = sample.position;
            navMeshAgent.enabled = true;
            if (!navMeshAgent.isOnNavMesh && !navMeshAgent.Warp(sample.position))
            {
                navMeshAgent.enabled = false;
                return false;
            }

            return navMeshAgent.isOnNavMesh;
        }

        private NavMeshQueryFilter GetNavigationFilter()
        {
            return navigationSurface != null
                ? navigationSurface.CreateQueryFilter(navMeshAgent.areaMask)
                : new NavMeshQueryFilter { agentTypeID = navMeshAgent.agentTypeID, areaMask = navMeshAgent.areaMask };
        }

        private void UpdateFacing()
        {
            if (!AgentReady) return;
            if (navMeshAgent.velocity.sqrMagnitude > 0.08f)
            {
                FacePosition(transform.position + navMeshAgent.velocity);
            }
            else if (isEngaging && botVision.Player != null)
            {
                FacePosition(botVision.Player.AimPoint);
            }
        }

        private void ApplyDifficultySettings()
        {
            BotDifficultySettings settings = BotDifficultySettings.For(difficulty);
            visionRange = settings.visionRange;
            fieldOfView = settings.fieldOfView;
            detectionDelay = settings.detectionDelay;
            reactionTime = settings.reactionTime;
            accuracy = settings.accuracy;
            fireInterval = settings.fireInterval;
            damagePerShot = settings.damagePerShot;
            maximumHealth = settings.health;
            movementSpeed = settings.movementSpeed;
            if (navMeshAgent != null) navMeshAgent.speed = movementSpeed;
            if (botVision != null) botVision.Configure(playerTarget, settings);
        }

        private void OnValidate()
        {
            if (difficulty != lastAppliedDifficulty)
            {
                ApplyDifficultySettings();
                lastAppliedDifficulty = difficulty;
            }

            patrolRadius = Mathf.Max(1f, patrolRadius);
            engageDistance = Mathf.Max(1f, engageDistance);
            chaseUpdateInterval = Mathf.Max(0.1f, chaseUpdateInterval);
            investigationTimeout = Mathf.Max(0.5f, investigationTimeout);
            investigationMemorySeconds = Mathf.Max(0.5f, investigationMemorySeconds);
            searchDuration = Mathf.Max(1f, searchDuration);
        }

        private BotDifficultySettings CurrentSettings()
        {
            return new BotDifficultySettings
            {
                visionRange = visionRange,
                fieldOfView = fieldOfView,
                detectionDelay = detectionDelay,
                reactionTime = reactionTime,
                accuracy = accuracy,
                fireInterval = fireInterval,
                damagePerShot = damagePerShot,
                health = maximumHealth,
                movementSpeed = movementSpeed
            };
        }

        private void BuildPlaceholderVisual()
        {
            if (transform.Find("BREACH Bot Visual") != null) return;

            CreateVisualPart("BREACH Bot Visual", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f),
                new Vector3(0.66f, 0.83f, 0.44f), BodyMat);
            CreateVisualPart("Helmet", PrimitiveType.Sphere, new Vector3(0f, 1.82f, 0f),
                new Vector3(0.46f, 0.3f, 0.42f), DarkMat);
            CreateVisualPart("Chest plate", PrimitiveType.Cube, new Vector3(0f, 1.13f, 0.23f),
                new Vector3(0.42f, 0.38f, 0.09f), DarkMat);
            CreateVisualPart("Back pack", PrimitiveType.Cube, new Vector3(0f, 1.08f, -0.24f),
                new Vector3(0.38f, 0.5f, 0.18f), BodyMat);
        }

        private void CreateVisualPart(string partName, PrimitiveType primitive, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                partCollider.enabled = false;
                Destroy(partCollider);
            }
            Renderer partRenderer = part.GetComponent<Renderer>();
            if (partRenderer != null) partRenderer.sharedMaterial = material;
        }

        private static Material BodyMat => GetOrCreateMaterial(ref bodyMaterial, "BREACH Bot Olive", new Color(0.19f, 0.23f, 0.19f));
        private static Material DarkMat => GetOrCreateMaterial(ref darkMaterial, "BREACH Bot Dark", new Color(0.075f, 0.085f, 0.08f));

        private static Material GetOrCreateMaterial(ref Material material, string materialName, Color color)
        {
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = materialName, hideFlags = HideFlags.DontSave };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
    }
}
