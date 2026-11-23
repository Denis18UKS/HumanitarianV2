using UnityEngine;
using UnityEngine.InputSystem;

namespace Humanitarian.Vehicles
{
    /// <summary>
    /// Tiny demo character and cockpit/chase camera to make the car playable immediately.
    /// This is not a final FPS controller or character model.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class GhostCamaroDemoDriver : MonoBehaviour
    {
        public GhostCamaroDrive vehicle;
        public Camera playerCamera;
        public bool startInCar = true;
        [Min(0.5f)] public float onFootSpeed = 4.2f;
        [Range(0.05f, 3)] public float mouseSensitivity = 0.17f;
        [Range(0.02f, 1)] public float cameraSmoothing = 0.18f;
        private CharacterController character;
        private bool inCar;
        private int cameraMode;
        private float yaw, pitch;
        private float verticalSpeed;
        private float nextInteractionTime;

        private void Start()
        {
            character = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (vehicle == null) vehicle = FindFirstObjectByType<GhostCamaroDrive>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (startInCar && vehicle != null) EnterCar();
        }

        private void Update()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
            if (Cursor.lockState == CursorLockMode.Locked && m != null)
            {
                Vector2 delta = m.delta.ReadValue() * mouseSensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, -48f, 48f);
            }
            if (inCar)
            {
                if (k.f5Key.wasPressedThisFrame) { cameraMode = (cameraMode + 1) % 3; yaw = 0f; pitch = 0f; }
                if (k.eKey.wasPressedThisFrame && Time.time > nextInteractionTime) ExitCar();
            }
            else
            {
                if (k.eKey.wasPressedThisFrame && vehicle != null &&
                    Vector3.Distance(transform.position, vehicle.transform.position) < 4.5f &&
                    Time.time > nextInteractionTime) EnterCar();
                Walk(k);
            }
        }

        private void Walk(Keyboard k)
        {
            if (!character.enabled) return;
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * new Vector3(
                (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f), 0f,
                (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f));
            if (character.isGrounded && verticalSpeed < 0f) verticalSpeed = -1.0f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            Vector3 displacement = direction.normalized * onFootSpeed + Vector3.up * verticalSpeed;
            character.Move(displacement * Time.deltaTime);
        }

        private void EnterCar()
        {
            if (vehicle == null || vehicle.driverSeated) return;
            inCar = true;
            vehicle.driverSeated = true;
            vehicle.engineRunning = true;
            character.enabled = false;
            cameraMode = 1; // Chase camera first; F5 cycles through cockpit and frontal camera.
            yaw = pitch = 0f;
            nextInteractionTime = Time.time + 0.25f;
            Debug.Log("Humanitarian: Ghost Camaro driver entered. Use WASD, Space, F5, L, Z, X, C, R, E.");
        }

        private void ExitCar()
        {
            if (vehicle == null) return;
            inCar = false;
            vehicle.driverSeated = false;
            transform.position = vehicle.transform.TransformPoint(new Vector3(-2.1f, 0.8f, -0.6f));
            character.enabled = true;
            verticalSpeed = 0f;
            nextInteractionTime = Time.time + 0.25f;
        }

        private void LateUpdate()
        {
            if (playerCamera == null) return;
            if (inCar && vehicle != null)
            {
                Transform car = vehicle.transform;
                Vector3 targetPosition;
                Quaternion targetRotation;
                if (cameraMode == 0) // Driver cockpit: view wheel / dashboard / windshield.
                {
                    targetPosition = car.TransformPoint(new Vector3(-0.38f, 1.08f, -0.36f));
                    targetRotation = car.rotation * Quaternion.Euler(pitch, yaw, 0f);
                }
                else
                {
                    bool front = cameraMode == 2;
                    Vector3 orbit = Quaternion.Euler(pitch * 0.6f, yaw + (front ? 180f : 0f), 0f) *
                        new Vector3(0f, 1.9f, -6.1f);
                    Vector3 aim = car.TransformPoint(new Vector3(0f, 1.05f, 0.4f));
                    Vector3 desired = aim + car.rotation * orbit;
                    Vector3 dir = desired - aim;
                    float distance = dir.magnitude;
                    if (distance > 0.001f && Physics.SphereCast(aim, 0.2f, dir.normalized,
                        out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore) &&
                        hit.collider.gameObject != vehicle.gameObject &&
                        !hit.collider.transform.IsChildOf(vehicle.transform))
                        desired = aim + dir.normalized * Mathf.Max(0.6f, hit.distance - 0.15f);
                    targetPosition = desired;
                    targetRotation = Quaternion.LookRotation((aim - desired).normalized, Vector3.up);
                }
                float smooth = 1f - Mathf.Pow(0.001f, Time.unscaledDeltaTime / Mathf.Max(0.02f, cameraSmoothing));
                playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPosition, smooth);
                playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRotation, smooth);
                playerCamera.fieldOfView = cameraMode == 0 ? 74f : Mathf.Lerp(65f, 78f,
                    Mathf.Clamp01(vehicle.SpeedKmh / 110f));
            }
            else
            {
                playerCamera.transform.position = transform.position + Vector3.up * 1.58f;
                playerCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                playerCamera.fieldOfView = 72f;
            }
        }

        private void OnGUI()
        {
            if (vehicle == null) return;
            GUI.Box(new Rect(12, 12, 360, inCar ? 119 : 70), "Humanitarian | Ghost Camaro — Driving Demo");
            if (inCar)
            {
                GUI.Label(new Rect(22, 36, 336, 24),
                    $"Speed: {vehicle.SpeedKmh:0} km/h | Engine: {(vehicle.engineRunning ? "ON" : "OFF")}");
                GUI.Label(new Rect(22, 60, 336, 23), "WASD drive  Space brake  F5 camera  E exit");
                GUI.Label(new Rect(22, 84, 336, 23), "R engine  L lights  Z/X signals  C hazards");
            }
            else
            {
                GUI.Label(new Rect(22, 36, 336, 24), "WASD walk | E enter car nearby | Esc mouse");
            }
        }
    }
}