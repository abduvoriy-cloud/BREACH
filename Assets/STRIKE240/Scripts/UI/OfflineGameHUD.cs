using UnityEngine;
using UnityEngine.InputSystem;

namespace Strike240
{
    /// <summary>Readable 1080p tactical HUD for round state, score, and player statistics.</summary>
    public sealed class OfflineGameHUD : MonoBehaviour
    {
        [SerializeField] private OfflineRoundManager roundManager;
        [SerializeField] private Color attackersColor = new Color(0.83f, 0.49f, 0.21f, 1f);
        [SerializeField] private Color defendersColor = new Color(0.32f, 0.59f, 0.76f, 1f);

        private GUIStyle headerStyle;
        private GUIStyle scoreStyle;
        private GUIStyle detailStyle;
        private GUIStyle announcementStyle;
        private GUIStyle subtitleStyle;

        public void Bind(OfflineRoundManager manager)
        {
            roundManager = manager;
        }

        private void Awake()
        {
            if (roundManager == null) roundManager = FindAnyObjectByType<OfflineRoundManager>();
        }

        private void OnGUI()
        {
            if (roundManager == null || roundManager.State == OfflineRoundState.SetupError) return;
            EnsureStyles();

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.72f, 1.2f);
            headerStyle.fontSize = Mathf.RoundToInt(14f * scale);
            scoreStyle.fontSize = Mathf.RoundToInt(26f * scale);
            detailStyle.fontSize = Mathf.RoundToInt(16f * scale);
            announcementStyle.fontSize = Mathf.RoundToInt(30f * scale);
            subtitleStyle.fontSize = Mathf.RoundToInt(18f * scale);
            DrawScoreStrip(scale);
            DrawPlayerStats(scale);
            DrawAnnouncement(scale);
            if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
                DrawScoreboard(scale);
        }

        private void DrawScoreStrip(float scale)
        {
            float width = 650f * scale;
            float height = 62f * scale;
            Rect rect = new Rect((Screen.width - width) * 0.5f, 20f * scale, width, height);
            DrawPanel(rect, new Color(0.035f, 0.045f, 0.05f, 0.82f));

            float sideWidth = width * 0.31f;
            GUI.color = attackersColor;
            GUI.Label(new Rect(rect.x + 12f * scale, rect.y + 7f * scale, sideWidth, 22f * scale), "ATTACKERS", headerStyle);
            GUI.Label(new Rect(rect.x + 12f * scale, rect.y + 27f * scale, sideWidth, 28f * scale),
                roundManager.GetTeamScore(CombatTeam.Attackers).ToString("00"), scoreStyle);

            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + sideWidth, rect.y + 5f * scale, width - sideWidth * 2f, 24f * scale),
                $"ROUND {roundManager.CurrentRound:00}  /  {roundManager.MaximumRounds:00}", headerStyle);
            GUI.Label(new Rect(rect.x + sideWidth, rect.y + 29f * scale, width - sideWidth * 2f, 24f * scale),
                FormatTime(roundManager.RoundTimeRemaining), scoreStyle);

            GUI.color = defendersColor;
            GUI.Label(new Rect(rect.x + width - sideWidth - 12f * scale, rect.y + 7f * scale, sideWidth, 22f * scale), "DEFENDERS", headerStyle);
            GUI.Label(new Rect(rect.x + width - sideWidth - 12f * scale, rect.y + 27f * scale, sideWidth, 28f * scale),
                roundManager.GetTeamScore(CombatTeam.Defenders).ToString("00"), scoreStyle);
            GUI.color = Color.white;
        }

        private void DrawPlayerStats(float scale)
        {
            TeamMember player = roundManager.PlayerMember;
            if (player == null) return;

            float width = 176f * scale;
            float height = 54f * scale;
            Rect rect = new Rect(24f * scale, Screen.height - height - 24f * scale, width, height);
            DrawPanel(rect, new Color(0.035f, 0.045f, 0.05f, 0.72f));
            GUI.Label(new Rect(rect.x + 10f * scale, rect.y + 5f * scale, width - 20f * scale, 18f * scale),
                "PLAYER  K / D", headerStyle);
            GUI.Label(new Rect(rect.x + 10f * scale, rect.y + 23f * scale, width - 20f * scale, 24f * scale),
                $"{player.Kills:00}  /  {player.Deaths:00}", scoreStyle);
        }

        private void DrawAnnouncement(float scale)
        {
            if (string.IsNullOrEmpty(roundManager.Announcement)) return;
            if (roundManager.State == OfflineRoundState.Active && Time.unscaledTime > roundManager.AnnouncementUntil)
                return;

            float width = 640f * scale;
            float height = 114f * scale;
            Rect rect = new Rect((Screen.width - width) * 0.5f,
                Screen.height * 0.34f - height * 0.5f, width, height);
            DrawPanel(rect, new Color(0.025f, 0.035f, 0.04f, 0.78f));
            GUI.Label(new Rect(rect.x + 16f * scale, rect.y + 20f * scale, width - 32f * scale, 42f * scale),
                roundManager.Announcement, announcementStyle);
            GUI.Label(new Rect(rect.x + 16f * scale, rect.y + 65f * scale, width - 32f * scale, 24f * scale),
                roundManager.AnnouncementDetail, subtitleStyle);
        }

        private void DrawScoreboard(float scale)
        {
            float width = 440f * scale;
            float rowHeight = 30f * scale;
            float height = 48f * scale + roundManager.Participants.Count * rowHeight;
            Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawPanel(rect, new Color(0.025f, 0.035f, 0.04f, 0.9f));
            GUI.Label(new Rect(rect.x + 16f * scale, rect.y + 10f * scale, width - 32f * scale, 28f * scale),
                "BREACH  |  PLAYER / TEAM / K / D", headerStyle);

            float y = rect.y + 42f * scale;
            foreach (TeamMember member in roundManager.Participants)
            {
                if (member == null) continue;
                GUI.color = member.Team == CombatTeam.Attackers ? attackersColor : defendersColor;
                string label = member.GetComponent<PlayerHealth>() != null ? "PLAYER" : member.name;
                GUI.Label(new Rect(rect.x + 16f * scale, y, width - 32f * scale, rowHeight),
                    $"{label}  |  {member.Team.ToString().ToUpperInvariant()}  |  {member.Kills:00}  /  {member.Deaths:00}", detailStyle);
                y += rowHeight;
            }
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (headerStyle != null) return;
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.92f, 0.91f) }
            };
            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            announcementStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.83f, 0.86f, 0.85f) }
            };
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none);
            GUI.color = previous;
        }

        private static string FormatTime(float seconds)
        {
            int wholeSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
        }
    }
}
