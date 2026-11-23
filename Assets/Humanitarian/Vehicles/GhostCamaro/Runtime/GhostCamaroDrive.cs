using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Humanitarian.Vehicles
{
    /// <summary>
    /// Playable FIRST driving pass: Unity WheelColliders + Rigidbody, actual road collision,
    /// wheel transforms, steering wheel, lights and driver input. No fake transform movement.
    /// Input is applied only while a demo driver occupies the vehicle.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class GhostCamaroDrive : MonoBehaviour
    {
        [Header("Imported model")]
        public Transform visualRoot;
        public Transform steeringWheel;
        public Transform wheelFL, wheelFR, wheelRL, wheelRR;

        [Header("Physics")]
        public WheelCollider colliderFL, colliderFR, colliderRL, colliderRR;
        [Min(0)] public float accelerationTorque = 1450f;
        [Min(0)] public float reverseTorque = 700f;
        [Min(0)] public float brakingTorque = 4200f;
        [Min(0)] public float handbrakeTorque = 6200f;
        [Range(10, 45)] public float maximumSteerAngle = 30f;
        [Min(1)] public float maxForwardSpeedKmh = 145f;
        [Min(1)] public float maxReverseSpeedKmh = 30f;
        public bool engineRunning = true;
        public bool driverSeated;

        [Header("Lights / demo")]
        public Light headlampLeft, headlampRight;
        public Light brakeLightLeft, brakeLightRight;
        public Light indicatorFL, indicatorFR, indicatorRL, indicatorRR;
        public bool headlightsOn;

        private Rigidbody body;
        private float gasInput;
        private float steeringInput;
        private float brakeInput;
        private bool parkingBrake;
        private Quaternion originalSteeringRotation;
        private bool hasSteeringWheel;
        private bool leftIndicator, rightIndicator, hazardLights;
        private float signalClock;
        private bool signalPulse;

        public float SpeedKmh => body != null ? body.linearVelocity.magnitude * 3.6f : 0f;
        public float ForwardSpeedKmh => body != null ? Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f : 0f;
        public Rigidbody PhysicsBody => body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 1690f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (steeringWheel != null)
            {
                originalSteeringRotation = steeringWheel.localRotation;
                hasSteeringWheel = true;
            }
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null || !driverSeated)
            {
                gasInput = brakeInput = steeringInput = 0f;
                parkingBrake = true;
            }
            else
            {
                if (keys.rKey.wasPressedThisFrame) engineRunning = !engineRunning;
                if (keys.lKey.wasPressedThisFrame) headlightsOn = !headlightsOn;
                if (keys.zKey.wasPressedThisFrame) { leftIndicator = !leftIndicator; rightIndicator = false; hazardLights = false; }
                if (keys.xKey.wasPressedThisFrame) { rightIndicator = !rightIndicator; leftIndicator = false; hazardLights = false; }
                if (keys.cKey.wasPressedThisFrame) { hazardLights = !hazardLights; leftIndicator = rightIndicator = false; }
                steeringInput = (keys.dKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed ? 1f : 0f);
                bool accelerate = keys.wKey.isPressed;
                bool slow = keys.sKey.isPressed;
                gasInput = accelerate ? 1f : 0f;
                brakeInput = slow && ForwardSpeedKmh > 1.25f ? 1f : 0f;
                if (slow && ForwardSpeedKmh <= 1.25f) gasInput = -0.65f;
                parkingBrake = keys.spaceKey.isPressed;
            }
            if (!driverSeated) parkingBrake = true;
            signalClock += Time.deltaTime;
            if (signalClock >= 0.52f) { signalClock = 0; signalPulse = !signalPulse; }
            ApplyLights();
        }

        private void FixedUpdate()
        {
            if (colliderFL == null || colliderFR == null || colliderRL == null || colliderRR == null) return;
            float steerFactor = Mathf.Lerp(1f, 0.40f, Mathf.Clamp01(SpeedKmh / 110f));
            float steerAngle = steeringInput * maximumSteerAngle * steerFactor;
            colliderFL.steerAngle = steerAngle;
            colliderFR.steerAngle = steerAngle;

            float torque = 0f;
            if (engineRunning && driverSeated)
            {
                if (gasInput > 0f && ForwardSpeedKmh < maxForwardSpeedKmh)
                    torque = accelerationTorque * gasInput;
                else if (gasInput < 0f && ForwardSpeedKmh > -maxReverseSpeedKmh)
                    torque = reverseTorque * gasInput;
            }
            colliderRL.motorTorque = torque * 0.5f;
            colliderRR.motorTorque = torque * 0.5f;
            colliderFL.motorTorque = colliderFR.motorTorque = 0f;

            float brake = brakeInput * brakingTorque;
            colliderFL.brakeTorque = colliderFR.brakeTorque = brake;
            colliderRL.brakeTorque = colliderRR.brakeTorque = Mathf.Max(brake, parkingBrake ? handbrakeTorque : 0f);
        }

        private void LateUpdate()
        {
            ApplyWheelPose(colliderFL, wheelFL);
            ApplyWheelPose(colliderFR, wheelFR);
            ApplyWheelPose(colliderRL, wheelRL);
            ApplyWheelPose(colliderRR, wheelRR);
            if (hasSteeringWheel && steeringWheel != null)
                steeringWheel.localRotation = originalSteeringRotation * Quaternion.Euler(0f, 0f, -steeringInput * 70f);
        }

        private static void ApplyWheelPose(WheelCollider source, Transform mesh)
        {
            if (source == null || mesh == null) return;
            source.GetWorldPose(out Vector3 position, out Quaternion rotation);
            mesh.SetPositionAndRotation(position, rotation);
        }

        private void ApplyLights()
        {
            SetLight(headlampLeft, headlightsOn);
            SetLight(headlampRight, headlightsOn);
            bool braking = brakeInput > 0.02f || parkingBrake;
            SetLight(brakeLightLeft, braking);
            SetLight(brakeLightRight, braking);
            bool left = signalPulse && (hazardLights || leftIndicator);
            bool right = signalPulse && (hazardLights || rightIndicator);
            SetLight(indicatorFL, left);
            SetLight(indicatorRL, left);
            SetLight(indicatorFR, right);
            SetLight(indicatorRR, right);
        }

        private static void SetLight(Light lightComponent, bool enabled)
        {
            if (lightComponent != null) lightComponent.enabled = enabled;
        }
    }
}