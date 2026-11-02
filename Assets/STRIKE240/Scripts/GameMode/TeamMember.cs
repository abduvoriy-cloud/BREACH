using UnityEngine;

namespace Strike240
{
    /// <summary>Team identity and match statistics for one player or offline bot.</summary>
    [DisallowMultipleComponent]
    public sealed class TeamMember : MonoBehaviour
    {
        [SerializeField] private CombatTeam team = CombatTeam.Attackers;

        private int kills;
        private int deaths;
        private bool deathRecordedThisRound;

        public CombatTeam Team => team;
        public int Kills => kills;
        public int Deaths => deaths;

        public bool IsAlive
        {
            get
            {
                PlayerHealth player = GetComponent<PlayerHealth>();
                if (player != null) return player.IsAlive;

                BotAgent bot = GetComponent<BotAgent>();
                return bot == null || bot.IsAlive;
            }
        }

        public void SetTeam(CombatTeam newTeam)
        {
            team = newTeam;
        }

        public void ResetMatchStats()
        {
            kills = 0;
            deaths = 0;
            deathRecordedThisRound = false;
        }

        public void ResetRoundStatus()
        {
            deathRecordedThisRound = false;
        }

        public void RecordKill()
        {
            kills++;
        }

        public bool RecordDeath()
        {
            if (deathRecordedThisRound) return false;
            deathRecordedThisRound = true;
            deaths++;
            return true;
        }
    }
}
