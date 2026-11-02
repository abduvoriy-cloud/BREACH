using UnityEngine;

namespace Strike240
{
    /// <summary>Screen-size-aware prototype crosshair and weapon status display.</summary>
    public sealed class WeaponHUD : MonoBehaviour
    {
        [SerializeField] private RifleWeapon weapon;
        [SerializeField] private Color crosshairColor = Color.white;
        [SerializeField] private Color hitConfirmColor = new Color(0.3f, 1f, 0.35f);

        private GUIStyle ammoStyle;
        private GUIStyle detailStyle;
        private Texture2D pixel;

        private void Awake()
        {
            if (weapon == null) weapon = FindAnyObjectByType<RifleWeapon>();
            pixel = Texture2D.whiteTexture;
        }

        private void OnGUI()
        {
            if (weapon == null) return;

            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 900f, 0.7f, 1.5f);
            DrawCrosshair(scale);
            DrawAmmunition(scale);
        }

        private void DrawCrosshair(float scale)
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float thickness = 2f * scale;
            float length = 9f * scale;
            float gap = weapon.CrosshairGap * scale;
            Color previousColor = GUI.color;
            GUI.color = Time.time - weapon.LastTargetHitTime < 0.12f ? hitConfirmColor : crosshairColor;

            GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - gap - length, thickness, length), pixel);
            GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY + gap, thickness, length), pixel);
            GUI.DrawTexture(new Rect(centerX - gap - length, centerY - thickness * 0.5f, length, thickness), pixel);
            GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness * 0.5f, length, thickness), pixel);
            GUI.color = previousColor;
        }

        private void DrawAmmunition(float scale)
        {
            float margin = 28f * scale;
            float width = 235f * scale;
            float height = 56f * scale;
            float left = Screen.width - width - margin;
            float top = Screen.height - height - margin;

            string ammoText = $"{weapon.MagazineAmmo:00}  /  {weapon.ReserveAmmo:000}";
            string statusText = weapon.IsReloading
                ? "RELOADING"
                : weapon.FireMode == WeaponFireMode.Automatic ? "AUTO  [B]" : "SEMI  [B]";

            ammoStyle.fontSize = Mathf.RoundToInt(30f * scale);
            detailStyle.fontSize = Mathf.RoundToInt(15f * scale);
            GUI.Label(new Rect(left, top, width, 38f * scale), ammoText, ammoStyle);
            GUI.Label(new Rect(left, top + 34f * scale, width, 22f * scale), statusText, detailStyle);
        }

        private void EnsureStyles()
        {
            if (ammoStyle != null) return;
            ammoStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.82f, 0.86f, 0.83f) }
            };
        }
    }
}
