using UnityEngine;

namespace Humanitarian.Vehicles
{
    /// <summary>
    /// Test one imported clip at a time. NOT a driving controller or concurrent-animation system.
    /// Real gameplay should use independent clip layers/Playables after import validation.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostCamaroAnimationPreview : MonoBehaviour
    {
        public Animator animator;
        public string clipName = "Door_L_Open";
        public bool playOnStart;

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (playOnStart) Play(clipName);
        }

        [ContextMenu("Play selected clip")]
        public void PlaySelected() => Play(clipName);

        public bool Play(string name)
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            int stateId = Animator.StringToHash("Base Layer." + name);
            if (!animator.HasState(0, stateId))
            {
                Debug.LogWarning("Ghost Camaro: clip/state not imported: " + name, this);
                return false;
            }
            animator.Play(stateId, 0, 0f);
            return true;
        }
    }
}
