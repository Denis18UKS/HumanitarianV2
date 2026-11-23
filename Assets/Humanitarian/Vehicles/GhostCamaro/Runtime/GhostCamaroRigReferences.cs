using System;
using UnityEngine;

namespace Humanitarian.Vehicles
{
    /// <summary>Named attachment references to the imported source rig. No physics/drive code yet.</summary>
    [DisallowMultipleComponent]
    public class GhostCamaroRigReferences : MonoBehaviour
    {
        public Transform visualRoot;
        public Transform wheelFL, wheelFR, wheelRL, wheelRR;
        public Transform steeringWheel;
        public Transform doorLeft, doorRight, hood, trunk;
        public Transform leftMirror, rightMirror;
        public Transform driverSeat, passengerSeat;
        public Transform armoredPlough;
        public Animator animator;

        public void AutoBind()
        {
            if (visualRoot == null) visualRoot = transform.Find("Visual");
            if (visualRoot == null) visualRoot = transform;
            wheelFL = FindDeep(visualRoot, "Wheel_FL");
            wheelFR = FindDeep(visualRoot, "Wheel_FR");
            wheelRL = FindDeep(visualRoot, "Wheel_RL");
            wheelRR = FindDeep(visualRoot, "Wheel_RR");
            steeringWheel = FindDeep(visualRoot, "Steering_Wheel");
            doorLeft = FindDeep(visualRoot, "Door_L_hinge") ?? FindDeep(visualRoot, "Door_L");
            doorRight = FindDeep(visualRoot, "Door_R_hinge") ?? FindDeep(visualRoot, "Door_R");
            hood = FindDeep(visualRoot, "Hood");
            trunk = FindDeep(visualRoot, "Trunk");
            leftMirror = FindDeep(visualRoot, "Mirror_Hinge_L");
            rightMirror = FindDeep(visualRoot, "Mirror_Hinge_R");
            driverSeat = FindDeep(visualRoot, "Driver_Seat");
            passengerSeat = FindDeep(visualRoot, "Passenger_Seat");
            armoredPlough = FindDeep(visualRoot, "Armored_Plough");
            animator = visualRoot.GetComponentInChildren<Animator>(true);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
