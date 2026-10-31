using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FireEscape
{
    // Works alone for Phase 1; the emergency manager owns controls during Phase 2.
    [RequireComponent(typeof(CharacterController))]
    public sealed class FEFirstPerson : MonoBehaviour
    {
        public Camera viewCamera;
        public float walkSpeed = 3.5f;
        public float mouseSensitivity = 0.12f;
        [HideInInspector] public bool controlsEnabled = true;
        [HideInInspector] public bool showWalkthroughHUD = true;
        public System.Action restartRequested;
        private CharacterController controller;
        private Vector3 spawn;
        private float pitch;
        private float verticalSpeed;
        private GUIStyle style;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawn = transform.position;
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (!controlsEnabled) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || viewCamera == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ReleaseCursor();
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }
            if (keyboard.rKey.wasPressedThisFrame)
            {
                if (restartRequested != null) restartRequested();
                else ResetToSpawn();
                return;
            }
            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -80, 80);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            Vector3 direction = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(verticalSpeed - 20 * Time.deltaTime, -40);
            controller.Move((direction * walkSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
#endif
        }

        public void ResetToSpawn()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawn, Quaternion.identity);
            controller.enabled = true;
            verticalSpeed = 0;
            pitch = 0;
            if (viewCamera != null) viewCamera.transform.localRotation = Quaternion.identity;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        private void OnDisable() { ReleaseCursor(); }
        private void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (!showWalkthroughHUD) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
                style.normal.textColor = Color.white;
            }
            string instructions = "PHASE 1 | Layout walkthrough\nWASD: move   Mouse: look   R: return to lab   Esc: release mouse\nAt the junction: straight = Exit 1; left = Exit 2; right = Exit 3.\nAll paths are open for this layout test. No emergency events yet.";
#if !ENABLE_INPUT_SYSTEM
            instructions = "Enable Input System Package (New) in Edit > Project Settings > Player > Other Settings > Active Input Handling, then restart the Editor when prompted.";
#endif
            if (Cursor.lockState != CursorLockMode.Locked) instructions += "\nClick inside the Game view to control the player.";
            if (transform.position.x > 26 && transform.position.z > 28)
                instructions += "\nASSEMBLY AREA REACHED - press R to inspect another route.";
            float width = Mathf.Min(Screen.width - 24, 720);
            GUI.Box(new Rect(12, 12, width, 180), GUIContent.none);
            GUI.Label(new Rect(24, 20, width - 24, 165), instructions, style);
            if (Cursor.lockState == CursorLockMode.Locked)
                GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 25), "+", style);
        }
    }
}
