using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Strike240
{
    public enum OfflineRoundState
    {
        SetupError,
        RoundStarting,
        Active,
        RoundEnding,
        MatchEnded
    }

    /// <summary>Runs an offline player-versus-bots tactical match in the active scene.</summary>
    [DefaultExecutionOrder(-5000)]
    public sealed class OfflineRoundManager : MonoBehaviour
    {
        private const string MapRootName = "DUST SECTOR | Industrial Basin";

        [Header("Teams")]
        [SerializeField] private CombatTeam playerTeam = CombatTeam.Attackers;
        [SerializeField] private CombatTeam botTeam = CombatTeam.Defenders;
        [SerializeField] private CombatTeam timeoutWinnerTeam = CombatTeam.Defenders;

        [Header("Round Rules")]
        [SerializeField, Min(10f)] private float roundDuration = 120f;
        [SerializeField, Min(0f)] private float roundStartDelay = 3f;
        [SerializeField, Min(0f)] private float intermissionDuration = 4f;
        [SerializeField, Min(1)] private int scoreLimit = 7;
        [SerializeField, Min(1)] private int maximumRounds = 12;

        [Header("Scene References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private RifleWeapon playerWeapon;
        [SerializeField] private Transform attackersSpawn;
        [SerializeField] private Transform defendersSpawn;
        [SerializeField] private BotNavigationSurface navigationSurface;
        [SerializeField] private BotAgent[] bots = new BotAgent[0];

        private readonly List<TeamMember> participants = new List<TeamMember>(8);
        private TeamMember playerMember;
        private int attackersScore;
        private int defendersScore;
        private float roundTimeRemaining;
        private bool matchWillEndAfterRound;
        private Coroutine matchRoutine;

        public OfflineRoundState State { get; private set; } = OfflineRoundState.SetupError;
        public int CurrentRound { get; private set; }
        public float RoundTimeRemaining => roundTimeRemaining;
        public string Announcement { get; private set; } = "";
        public string AnnouncementDetail { get; private set; } = "";
        public float AnnouncementUntil { get; private set; }
        public CombatTeam PlayerTeam => playerTeam;
        public CombatTeam BotTeam => botTeam;
        public TeamMember PlayerMember => playerMember;
        public IReadOnlyList<TeamMember> Participants => participants;
        public int ScoreLimit => scoreLimit;
        public int MaximumRounds => maximumRounds;

        private void Awake()
        {
            ResolveSceneReferences();
            if (!BuildRoster())
            {
                State = OfflineRoundState.SetupError;
                DisableCombat();
                return;
            }

            foreach (TeamMember member in participants)
            {
                member.ResetMatchStats();
                member.ResetRoundStatus();
                DamageableEntity damageable = member.GetComponent<DamageableEntity>();
                if (damageable != null) damageable.Eliminated += HandleEliminated;
            }

            DisableCombat();
            State = OfflineRoundState.RoundStarting;
            SetAnnouncement("BREACH OFFLINE MATCH", "Prepare for the opening round", float.PositiveInfinity);
        }

        private void Start()
        {
            if (State == OfflineRoundState.SetupError) return;
            matchRoutine = StartCoroutine(RunMatch());
        }

        private void Update()
        {
            if (State != OfflineRoundState.Active) return;

            roundTimeRemaining = Mathf.Max(0f, roundTimeRemaining - Time.deltaTime);
            if (roundTimeRemaining <= 0f)
            {
                EndRound(timeoutWinnerTeam, "TIME EXPIRED");
                return;
            }

            EvaluateEliminationWin();
        }

        private void OnDestroy()
        {
            foreach (TeamMember member in participants)
            {
                if (member == null) continue;
                DamageableEntity damageable = member.GetComponent<DamageableEntity>();
                if (damageable != null) damageable.Eliminated -= HandleEliminated;
            }

            if (matchRoutine != null) StopCoroutine(matchRoutine);
        }

        /// <summary>Called by the DUST SECTOR editor setup command to persist scene wiring.</summary>
        public void BindSceneReferences(PlayerHealth player, FirstPersonController controller,
            RifleWeapon weapon, Transform attackersStart, Transform defendersStart,
            BotNavigationSurface surface, BotAgent[] sceneBots)
        {
            playerHealth = player;
            playerController = controller;
            playerWeapon = weapon;
            attackersSpawn = attackersStart;
            defendersSpawn = defendersStart;
            navigationSurface = surface;
            bots = sceneBots ?? new BotAgent[0];
        }

        public int GetTeamScore(CombatTeam team)
        {
            return team == CombatTeam.Attackers ? attackersScore : defendersScore;
        }

        public int GetAliveCount(CombatTeam team)
        {
            int count = 0;
            foreach (TeamMember member in participants)
            {
                if (member != null && member.Team == team && member.gameObject.activeInHierarchy && member.IsAlive)
                    count++;
            }
            return count;
        }

        private Transform GetSpawnForTeam(CombatTeam team)
        {
            return team == CombatTeam.Attackers ? attackersSpawn : defendersSpawn;
        }

        private IEnumerator RunMatch()
        {
            // Let all scene components finish Awake/Start before the first round reset.
            yield return null;

            while (State != OfflineRoundState.MatchEnded && State != OfflineRoundState.SetupError)
            {
                CurrentRound++;
                State = OfflineRoundState.RoundStarting;
                ResetRoundActors();
                float countdown = roundStartDelay;
                while (countdown > 0f)
                {
                    Announcement = $"ROUND {CurrentRound} STARTS IN {Mathf.CeilToInt(countdown)}";
                    AnnouncementDetail = "Attackers versus Defenders";
                    AnnouncementUntil = float.PositiveInfinity;
                    countdown -= Time.unscaledDeltaTime;
                    yield return null;
                }

                roundTimeRemaining = roundDuration;
                State = OfflineRoundState.Active;
                EnableCombat();
                SetAnnouncement("ROUND LIVE", "Eliminate the opposing team", 2f);

                while (State == OfflineRoundState.Active)
                    yield return null;

                if (State == OfflineRoundState.MatchEnded) yield break;

                float intermission = intermissionDuration;
                while (intermission > 0f)
                {
                    intermission -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (matchWillEndAfterRound)
                {
                    State = OfflineRoundState.MatchEnded;
                    bool tied = attackersScore == defendersScore;
                    Announcement = tied ? "MATCH DRAW" : GetLeadingTeamName() + " WIN THE MATCH";
                    AnnouncementDetail = $"Final score  {attackersScore} - {defendersScore}";
                    AnnouncementUntil = float.PositiveInfinity;
                    yield break;
                }
            }
        }

        private void ResetRoundActors()
        {
            foreach (TeamMember member in participants)
                if (member != null) member.ResetRoundStatus();

            Transform playerSpawn = GetSpawnForTeam(playerTeam);
            if (playerHealth != null)
            {
                playerHealth.ResetForRound();
                if (playerSpawn != null) MovePlayerToSpawn(playerSpawn);
            }

            if (playerWeapon != null) playerWeapon.ResetForRound();

            Transform botSpawn = GetSpawnForTeam(botTeam);
            NavMeshQueryFilter filter = navigationSurface != null
                ? navigationSurface.CreateQueryFilter(NavMesh.AllAreas)
                : default;
            List<Vector3> reserved = new List<Vector3>(bots.Length);
            for (int i = 0; i < bots.Length; i++)
            {
                BotAgent bot = bots[i];
                if (bot == null) continue;

                Vector3 desiredPosition = botSpawn != null
                    ? GetBotSpawnPosition(botSpawn, i)
                    : bot.transform.position;
                if (botSpawn != null && navigationSurface != null &&
                    NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 4f, filter))
                    desiredPosition = hit.position;

                if (!bot.ResetForRound(desiredPosition,
                        botSpawn != null ? botSpawn.rotation : bot.transform.rotation,
                        playerHealth, navigationSurface))
                {
                    Debug.LogError($"[BREACH Match] Could not reset {bot.name} onto the assigned NavMesh.", bot);
                    continue;
                }
                bool overlapped = false;
                foreach (Vector3 prior in reserved)
                {
                    if ((bot.transform.position - prior).sqrMagnitude < 1f)
                    {
                        overlapped = true;
                        break;
                    }
                }
                if (overlapped && botSpawn != null && navigationSurface != null)
                {
                    Vector3 fallback = bot.transform.position + botSpawn.right * (1.25f + i);
                    if (NavMesh.SamplePosition(fallback, out NavMeshHit spread, 4f, filter))
                        bot.ResetForRound(spread.position, botSpawn.rotation, playerHealth, navigationSurface);
                }
                reserved.Add(bot.transform.position);
                bot.SetCombatEnabled(false);
            }
        }

        private void MovePlayerToSpawn(Transform spawn)
        {
            if (playerController != null)
                playerController.ResetForRound(spawn);
            else
                playerHealth.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }

        private static Vector3 GetBotSpawnPosition(Transform spawn, int index)
        {
            const float spacing = 2.25f;
            int column = index % 3;
            int row = index / 3;
            float side = (column - 1) * spacing;
            return spawn.position + spawn.right * side + spawn.forward * (row * spacing);
        }

        private void EnableCombat()
        {
            if (playerController != null) playerController.enabled = true;
            if (playerWeapon != null) playerWeapon.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            foreach (BotAgent bot in bots)
                if (bot != null && bot.IsAlive) bot.SetCombatEnabled(true);
        }

        private void DisableCombat()
        {
            if (playerWeapon != null) playerWeapon.enabled = false;
            if (playerController != null) playerController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            foreach (BotAgent bot in bots)
                if (bot != null) bot.SetCombatEnabled(false);
        }

        private void EvaluateEliminationWin()
        {
            int aliveAttackers = GetAliveCount(CombatTeam.Attackers);
            int aliveDefenders = GetAliveCount(CombatTeam.Defenders);
            if (aliveAttackers == 0 && aliveDefenders == 0)
                EndRound(null, "BOTH TEAMS ELIMINATED");
            else if (aliveAttackers == 0)
                EndRound(CombatTeam.Defenders, "ATTACKERS ELIMINATED");
            else if (aliveDefenders == 0)
                EndRound(CombatTeam.Attackers, "DEFENDERS ELIMINATED");
        }

        private void HandleEliminated(DamageableEntity victim, TeamMember attacker)
        {
            TeamMember victimMember = victim != null ? victim.GetComponent<TeamMember>() : null;
            if (victimMember == null || !victimMember.RecordDeath()) return;
            if (State == OfflineRoundState.Active) EvaluateEliminationWin();
        }

        private void EndRound(CombatTeam? winner, string reason)
        {
            if (State != OfflineRoundState.Active) return;

            State = OfflineRoundState.RoundEnding;
            DisableCombat();
            matchWillEndAfterRound = false;
            if (winner == CombatTeam.Attackers) attackersScore++;
            else if (winner == CombatTeam.Defenders) defendersScore++;

            Announcement = winner.HasValue
                ? winner.Value.ToString().ToUpperInvariant() + " WIN ROUND " + CurrentRound
                : "ROUND DRAW";
            AnnouncementDetail = reason;
            AnnouncementUntil = float.PositiveInfinity;

            bool scoreLimitReached = attackersScore >= scoreLimit || defendersScore >= scoreLimit;
            bool maximumRoundsReached = CurrentRound >= maximumRounds;
            matchWillEndAfterRound = scoreLimitReached || maximumRoundsReached;
        }

        private string GetLeadingTeamName()
        {
            if (attackersScore == defendersScore) return "MATCH";
            return attackersScore > defendersScore ? "ATTACKERS" : "DEFENDERS";
        }

        private bool BuildRoster()
        {
            if (playerHealth == null || playerController == null || playerWeapon == null)
            {
                Debug.LogError("[BREACH Match] DUST SECTOR needs a PlayerHealth, FirstPersonController, and RifleWeapon.", this);
                return false;
            }

            if (playerTeam == botTeam)
            {
                Debug.LogError("[BREACH Match] Player Team and Bot Team must be different for a player-versus-bots match.", this);
                return false;
            }

            if (attackersSpawn == null || defendersSpawn == null)
            {
                Debug.LogError("[BREACH Match] DUST SECTOR needs both Team A Spawn and Team B Spawn transforms.", this);
                return false;
            }

            if (bots == null || bots.Length == 0)
            {
                Debug.LogError("[BREACH Match] No BotAgent is assigned. Add offline bots with BREACH > Offline Bots > Test Bot Spawner.", this);
                return false;
            }

            if (navigationSurface == null || navigationSurface.NavMeshData == null)
            {
                Debug.LogError("[BREACH Match] DUST SECTOR needs a baked NavMesh assigned to its BotNavigationSurface.", this);
                return false;
            }

            playerMember = EnsureTeamMember(playerHealth.gameObject, playerTeam);
            participants.Add(playerMember);
            for (int i = 0; i < bots.Length; i++)
            {
                BotAgent bot = bots[i];
                if (bot == null) continue;
                TeamMember member = EnsureTeamMember(bot.gameObject, botTeam);
                participants.Add(member);
                bot.SetTeamContext(playerHealth, navigationSurface);
            }

            return participants.Count > 1;
        }

        private void SetAnnouncement(string title, string detail, float visibleSeconds)
        {
            Announcement = title;
            AnnouncementDetail = detail;
            AnnouncementUntil = float.IsPositiveInfinity(visibleSeconds)
                ? float.PositiveInfinity
                : Time.unscaledTime + Mathf.Max(0f, visibleSeconds);
        }

        private void ResolveSceneReferences()
        {
            Scene activeScene = gameObject.scene;
            if (playerHealth == null)
                playerHealth = FindInScene<PlayerHealth>(activeScene);
            if (playerController == null)
                playerController = FindInScene<FirstPersonController>(activeScene);
            if (playerWeapon == null)
                playerWeapon = FindInScene<RifleWeapon>(activeScene);

            GameObject mapRoot = GameObject.Find(MapRootName);
            if (mapRoot != null && mapRoot.scene == activeScene && navigationSurface == null)
                navigationSurface = mapRoot.GetComponent<BotNavigationSurface>();

            if (attackersSpawn == null) attackersSpawn = FindNamedTransform(activeScene, "Team A Spawn");
            if (defendersSpawn == null) defendersSpawn = FindNamedTransform(activeScene, "Team B Spawn");
            if (bots == null || bots.Length == 0)
                bots = FindAllInScene<BotAgent>(activeScene);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (T component in FindObjectsByType<T>(FindObjectsInactive.Include))
                if (component.gameObject.scene == scene) return component;
            return null;
        }

        private static T[] FindAllInScene<T>(Scene scene) where T : Component
        {
            List<T> found = new List<T>();
            foreach (T component in FindObjectsByType<T>(FindObjectsInactive.Include))
                if (component.gameObject.scene == scene) found.Add(component);
            return found.ToArray();
        }

        private static Transform FindNamedTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == objectName) return child;
            }
            return null;
        }

        private static TeamMember EnsureTeamMember(GameObject actor, CombatTeam team)
        {
            TeamMember member = actor.GetComponent<TeamMember>();
            if (member == null) member = actor.AddComponent<TeamMember>();
            member.SetTeam(team);
            return member;
        }

        private void OnValidate()
        {
            roundDuration = Mathf.Max(10f, roundDuration);
            roundStartDelay = Mathf.Max(0f, roundStartDelay);
            intermissionDuration = Mathf.Max(0f, intermissionDuration);
            scoreLimit = Mathf.Max(1, scoreLimit);
            maximumRounds = Mathf.Max(1, maximumRounds);
        }
    }
}
