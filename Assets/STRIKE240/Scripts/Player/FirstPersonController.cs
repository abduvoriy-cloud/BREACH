using UnityEngine;
using UnityEngine.InputSystem;

namespace Strike240
{
    /// <summary>Simple CharacterController-based first-person movement.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraPivot;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 8f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField, Min(0f)] private float gravity = 20f;

        [Header("Mouse Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(1f, 89f)] private float lookLimit = 80f;
        [SerializeField, Min(0f)] private float recoilRecoverySpeed = 10f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float cameraPitch;
        private float recoilPitch;
        private float recoilYaw;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraPivot == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) cameraPivot = childCamera.transform;
            }
        }

        private void Start() => LockCursor();

        private void Update()
        {
            HandleCursor();
            RecoverRecoil();
            HandleLook();
            HandleMovement();
        }

        /// <summary>Applies an upward camera kick and sideways offset from a weapon.</summary>
        public void ApplyRecoil(float pitchUp, float yaw)
        {
            recoilPitch = Mathf.Clamp(recoilPitch + pitchUp, 0f, 18f);
            recoilYaw = Mathf.Clamp(recoilYaw + yaw, -6f, 6f);
        }

        public void ResetForRound(Transform spawn)
        {
            if (spawn == null) return;
            characterController.enabled = false;
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            characterController.enabled = true;
            verticalVelocity = 0f;
            cameraPitch = 0f;
            recoilPitch = 0f;
            recoilYaw = 0f;
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.identity;
        }

        private void RecoverRecoil()
        {
            float recovery = recoilRecoverySpeed * Time.deltaTime;
            recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, recovery);
            recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, recovery);
        }

        private void HandleCursor()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }
        }

        private void HandleLook()
        {
            Mouse mouse = Mouse.current;
            if (cameraPivot == null || mouse == null || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up * delta.x);
            cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -lookLimit, lookLimit);
            cameraPivot.localRotation = Quaternion.Euler(cameraPitch - recoilPitch, recoilYaw, 0f);
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 input = keyboard == null ? Vector2.zero :
                new Vector2((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            input = Vector2.ClampMagnitude(input, 1f);

            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (grounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

            verticalVelocity -= gravity * Time.deltaTime;
            bool sprinting = keyboard != null && keyboard.leftShiftKey.isPressed;
            float speed = sprinting ? sprintSpeed : walkSpeed;
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            characterController.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
