using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Strike240
{
    public enum WeaponFireMode
    {
        SemiAutomatic,
        Automatic
    }

    /// <summary>Input, firing, ammo, reload, raycast, and recoil for the prototype rifle.</summary>
    public sealed class RifleWeapon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private WeaponAudio weaponAudio;
        [SerializeField] private TeamMember teamMember;

        [Header("Firing")]
        [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Automatic;
        [SerializeField, Min(1f)] private float roundsPerMinute = 600f;
        [SerializeField, Min(1f)] private float range = 150f;
        [SerializeField, Min(0f)] private float damage = 34f;
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;

        [Header("Ammunition")]
        [SerializeField, Min(1)] private int magazineCapacity = 30;
        [SerializeField, Min(0)] private int startingMagazineAmmo = 30;
        [SerializeField, Min(0)] private int startingReserveAmmo = 90;
        [SerializeField, Min(0.1f)] private float reloadDuration = 1.8f;

        [Header("Recoil")]
        [SerializeField, Min(0f)] private float recoilPitch = 1.15f;
        [SerializeField, Min(0f)] private float recoilYaw = 0.3f;
        [SerializeField, Min(0f)] private float crosshairRecoverySpeed = 2.5f;

        private int magazineAmmo;
        private int reserveAmmo;
        private float nextShotTime;
        private float crosshairKick;
        private float lastTargetHitTime = -10f;
        private bool isReloading;

        public int MagazineAmmo => magazineAmmo;
        public int ReserveAmmo => reserveAmmo;
        public int MagazineCapacity => magazineCapacity;
        public bool IsReloading => isReloading;
        public WeaponFireMode FireMode => fireMode;
        public float LastTargetHitTime => lastTargetHitTime;
        public float Damage => damage;
        public float CrosshairGap => 7f + crosshairKick * 7f + MovementSpread;
        public event Action WeaponFired;
        public event Action<RaycastHit> SurfaceHit;

        private float MovementSpread => characterController == null
            ? 0f
            : Mathf.Clamp01(characterController.velocity.magnitude / 8f) * 5f;

        private void Awake()
        {
            magazineAmmo = Mathf.Clamp(startingMagazineAmmo, 0, magazineCapacity);
            reserveAmmo = startingReserveAmmo;
            if (aimCamera == null) aimCamera = GetComponentInParent<Camera>();
            if (playerController == null) playerController = GetComponentInParent<FirstPersonController>();
            if (characterController == null) characterController = GetComponentInParent<CharacterController>();
            if (weaponAudio == null) weaponAudio = GetComponent<WeaponAudio>();
            if (teamMember == null) teamMember = GetComponentInParent<TeamMember>();
        }

        public void ResetForRound()
        {
            StopAllCoroutines();
            magazineAmmo = Mathf.Clamp(startingMagazineAmmo, 0, magazineCapacity);
            reserveAmmo = startingReserveAmmo;
            isReloading = false;
            nextShotTime = Time.time;
            crosshairKick = 0f;
            lastTargetHitTime = -10f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            crosshairKick = Mathf.MoveTowards(crosshairKick, 0f, crosshairRecoverySpeed * Time.deltaTime);

            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
                fireMode = fireMode == WeaponFireMode.Automatic
                    ? WeaponFireMode.SemiAutomatic
                    : WeaponFireMode.Automatic;

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                TryReload();

            if (isReloading || mouse == null || Cursor.lockState != CursorLockMode.Locked) return;

            bool triggerPulled = fireMode == WeaponFireMode.Automatic
                ? mouse.leftButton.isPressed
                : mouse.leftButton.wasPressedThisFrame;

            if (triggerPulled && Time.time >= nextShotTime)
                Fire();
        }

        private void Fire()
        {
            if (magazineAmmo <= 0)
            {
                TryReload();
                return;
            }

            magazineAmmo--;
            nextShotTime = Time.time + 60f / roundsPerMinute;
            crosshairKick = Mathf.Min(crosshairKick + 0.8f, 3f);
            weaponAudio?.PlayShot();
            WeaponFired?.Invoke();

            float yawKick = UnityEngine.Random.Range(-recoilYaw, recoilYaw);
            playerController?.ApplyRecoil(recoilPitch, yawKick);

            if (aimCamera == null) return;

            Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(aimRay, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)) return;
            SurfaceHit?.Invoke(hit);

            DamageableEntity target = hit.collider.GetComponentInParent<DamageableEntity>();
            if (target == null) return;

            target.ApplyDamage(damage, hit.point, teamMember);
            lastTargetHitTime = Time.time;
        }

        private void TryReload()
        {
            if (isReloading || magazineAmmo >= magazineCapacity || reserveAmmo <= 0) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            weaponAudio?.PlayReload();
            yield return new WaitForSeconds(reloadDuration);

            int roundsNeeded = magazineCapacity - magazineAmmo;
            int roundsLoaded = Mathf.Min(roundsNeeded, reserveAmmo);
            magazineAmmo += roundsLoaded;
            reserveAmmo -= roundsLoaded;
            isReloading = false;
        }
    }
}
